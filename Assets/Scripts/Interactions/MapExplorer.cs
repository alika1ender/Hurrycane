using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>First-person player: walk, look, take/carry items, and tap or hold E to use whatever is under the crosshair.</summary>
[RequireComponent(typeof(CharacterController))]
public sealed class MapExplorer : MonoBehaviour
{
    /// <summary>Raised after any interactable is used by the player. The task engine listens to this.</summary>
    public static event Action<MapExplorer, IInteractable> Used;
    /// <summary>Letting go of E keeps hold progress this long (Docs/Design/03).</summary>
    const float HoldMemory = 5;
    /// <summary>A pickup this far behind the first thing hit still counts (items inside open cabinets and drawers).</summary>
    const float SeeThroughDepth = .45f;

    public Camera viewCamera;
    public Vector3 spawnPoint;
    public float reach = 2.3f;
    /// <summary>Scales every hold time; the day sets it for storm phases (x1.5).</summary>
    public float holdTimeMultiplier = 1;
    /// <summary>Extra hold-time factor from a racing pulse (Docs/Design/05: +15% / +30% on Day 3).</summary>
    public float pulseHoldFactor = 1;
    /// <summary>Set while breathing or fainted: look around only, no walking or using.</summary>
    public bool movementLocked;
    /// <summary>True on frames where the player actually sprints (pulse rises on Day 3).</summary>
    public bool Sprinting { get; private set; }
    CharacterController controller;
    IInteractable focus, holdTarget;
    float pitch, verticalSpeed, holdProgress, holdReleasedAt = float.NegativeInfinity, clock;
    bool holding;
    static readonly RaycastHit[] hits = new RaycastHit[16];

    PlayerInventory inventory;
    public PlayerInventory Inventory
    {
        get
        {
            if (!inventory) inventory = GetComponent<PlayerInventory>();
            if (!inventory) inventory = gameObject.AddComponent<PlayerInventory>();
            return inventory;
        }
    }
    /// <summary>Off once GameUI exists; the old IMGUI prompt and controls box are a fallback for scenes without it.</summary>
    public static bool DrawDebugHud = true;
    /// <summary>What the crosshair was on this frame (for the prompt UI).</summary>
    public IInteractable Focus => focus;
    /// <summary>What E would use right now (fresh raycast), without using it.</summary>
    public IInteractable Target => FindTarget();
    /// <summary>0..1 progress of the current hold, for the HUD.</summary>
    public float HoldProgress => holding ? holdProgress : 0;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        // The camera sits inside the capsule; keep the player's own collider out of the interaction ray.
        gameObject.layer = 2; // Ignore Raycast
    }
    void Start() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    void Update()
    {
        var keyboard = Keyboard.current; var mouse = Mouse.current;
        if (keyboard == null) return;
        if (keyboard.escapeKey.wasPressedThisFrame)
        { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked && !PauseMenu.Active)
        { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        if (Cursor.lockState != CursorLockMode.Locked) { focus = null; ReleaseHold(); return; }
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue() * .085f;
            transform.Rotate(0, delta.x, 0);
            pitch = Mathf.Clamp(pitch - delta.y, -82, 82);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        Vector2 axis = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                                  (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
        axis = movementLocked ? Vector2.zero : Vector2.ClampMagnitude(axis, 1);
        if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
        verticalSpeed += Physics.gravity.y * Time.deltaTime;
        Sprinting = keyboard.leftShiftKey.isPressed && !Inventory.Encumbered && axis.sqrMagnitude > .01f;
        float speed = Sprinting ? 4.2f : 2.6f;
        controller.Move(((transform.right * axis.x + transform.forward * axis.y) * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
        if (transform.position.y < -8) Respawn();
        if (movementLocked) { focus = null; ReleaseHold(); return; }
        if (keyboard.gKey.wasPressedThisFrame) Inventory.DropArmful(transform);
        StepUse(keyboard.eKey.isPressed, keyboard.eKey.wasPressedThisFrame, Time.deltaTime);
    }

    /// <summary>
    /// One frame of E handling: tap targets fire on press; hold targets fill while E stays down on them.
    /// Public so tests can drive it without real input.
    /// </summary>
    public void StepUse(bool down, bool pressedThisFrame, float deltaTime)
    {
        clock += deltaTime;
        focus = FindTarget();
        bool usable = focus != null && focus.Unavailable(this) == null;
        if (!down || !usable) { ReleaseHold(); return; }
        if (focus.HoldSeconds <= 0)
        {
            if (pressedThisFrame) Use(focus);
            return;
        }
        if (!holding)
        {
            // Resume the same target within the grace window, otherwise start over.
            if (focus != holdTarget || clock - holdReleasedAt > HoldMemory) holdProgress = 0;
            holdTarget = focus; holding = true;
        }
        holdProgress += deltaTime / Mathf.Max(.01f, focus.HoldSeconds * holdTimeMultiplier * pulseHoldFactor);
        if (holdProgress >= 1) { Use(focus); holding = false; holdTarget = null; holdProgress = 0; }
    }
    void ReleaseHold()
    {
        if (holding) holdReleasedAt = clock;
        holding = false;
    }
    void Use(IInteractable target)
    {
        target.Use(this);
        Used?.Invoke(this, target);
    }

    /// <summary>
    /// What E would use. Normally the first thing hit; a pickup slightly behind it wins (items inside open cabinets),
    /// unless a closed door or drawer is in the way.
    /// </summary>
    IInteractable FindTarget()
    {
        var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
        int count = Physics.RaycastNonAlloc(ray, hits, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (count == 0) return null;
        Array.Sort(hits, 0, count, System.Collections.Generic.Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));
        var first = hits[0].collider.GetComponentInParent<IInteractable>();
        if (first != null && first.Prompt == null) first = null; // e.g. windows not in play yet
        for (int i = 0; i < count && hits[i].distance <= hits[0].distance + SeeThroughDepth; i++)
        {
            var target = hits[i].collider.GetComponentInParent<IInteractable>();
            if (target is Pickup) return target;
            if (target is CabinetOpenable cabinet && !cabinet.IsOpen) break;
        }
        return first;
    }
    /// <summary>Uses whatever is under the crosshair at once, skipping any hold. Used by tests. Returns the target, used or not.</summary>
    public IInteractable Interact()
    {
        var target = FindTarget();
        if (target != null && target.Unavailable(this) == null) Use(target);
        return target;
    }
    void Respawn()
    { controller.enabled = false; transform.position = spawnPoint; controller.enabled = true; verticalSpeed = 0; }
    void OnGUI()
    {
        if (!DrawDebugHud) return;
        GUI.Box(new Rect(16, 16, 560, 82), "HOUSE MAP — WALKTHROUGH");
        GUI.Label(new Rect(28, 41, 540, 26), "WASD move   Shift run   Mouse look   E use (hold for some)   G drop armful   Esc cursor");
        GUI.Label(new Rect(28, 64, 540, 26), Inventory.Summary());
        if (Cursor.lockState != CursorLockMode.Locked) return;
        var c = new Vector2(Screen.width * .5f, Screen.height * .5f);
        var old = GUI.color;
        GUI.color = focus != null ? new Color(1, .85f, .35f, .95f) : new Color(1, 1, 1, .55f);
        float size = focus != null ? 6 : 4;
        GUI.DrawTexture(new Rect(c.x - size * .5f, c.y - size * .5f, size, size), Texture2D.whiteTexture);
        GUI.color = old;
        if (focus == null) return;
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
        string missing = focus.Unavailable(this);
        string key = focus.HoldSeconds > 0 ? "Hold E · " : "[E] ";
        GUI.Label(new Rect(c.x - 200, c.y + 18, 400, 26), key + focus.Prompt, style);
        if (missing != null)
        {
            GUI.color = new Color(.75f, .75f, .75f, .9f);
            GUI.Label(new Rect(c.x - 200, c.y + 42, 400, 24), missing, new GUIStyle(style) { fontSize = 14, fontStyle = FontStyle.Normal });
        }
        else if (HoldProgress > 0)
        {
            GUI.color = new Color(0, 0, 0, .5f);
            GUI.DrawTexture(new Rect(c.x - 60, c.y + 46, 120, 6), Texture2D.whiteTexture);
            GUI.color = new Color(1, .85f, .35f, .95f);
            GUI.DrawTexture(new Rect(c.x - 60, c.y + 46, 120 * HoldProgress, 6), Texture2D.whiteTexture);
        }
        GUI.color = old;
    }
}
