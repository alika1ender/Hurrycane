using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>First-person map inspection: walk, look, and open/close doors and drawers with E.</summary>
[RequireComponent(typeof(CharacterController))]
public sealed class MapExplorer : MonoBehaviour
{
    public Camera viewCamera;
    public Vector3 spawnPoint;
    public float reach = 2.3f;
    CharacterController controller;
    CabinetOpenable focus;
    float pitch, verticalSpeed;

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
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        if (Cursor.lockState != CursorLockMode.Locked) { focus = null; return; }
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue() * .085f;
            transform.Rotate(0, delta.x, 0);
            pitch = Mathf.Clamp(pitch - delta.y, -82, 82);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        Vector2 axis = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                                  (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
        axis = Vector2.ClampMagnitude(axis, 1);
        if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
        verticalSpeed += Physics.gravity.y * Time.deltaTime;
        float speed = keyboard.leftShiftKey.isPressed ? 4.2f : 2.6f;
        controller.Move(((transform.right * axis.x + transform.forward * axis.y) * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
        if (transform.position.y < -8) Respawn();
        focus = FindTarget();
        if (keyboard.eKey.wasPressedThisFrame) Interact();
    }
    CabinetOpenable FindTarget()
    {
        var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
        return Physics.Raycast(ray, out var hit, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.collider.GetComponentInParent<CabinetOpenable>() : null;
    }
    /// <summary>Toggles the door or drawer under the crosshair. Returns the toggled control.</summary>
    public CabinetOpenable Interact()
    {
        var target = FindTarget();
        if (target) target.Toggle();
        return target;
    }
    void Respawn()
    { controller.enabled = false; transform.position = spawnPoint; controller.enabled = true; verticalSpeed = 0; }
    void OnGUI()
    {
        GUI.Box(new Rect(16, 16, 470, 58), "HOUSE MAP — WALKTHROUGH");
        GUI.Label(new Rect(28, 41, 450, 26), "WASD move   Shift run   Mouse look   E open/close   Esc cursor");
        if (Cursor.lockState != CursorLockMode.Locked) return;
        var c = new Vector2(Screen.width * .5f, Screen.height * .5f);
        var old = GUI.color;
        GUI.color = focus ? new Color(1, .85f, .35f, .95f) : new Color(1, 1, 1, .55f);
        float size = focus ? 6 : 4;
        GUI.DrawTexture(new Rect(c.x - size * .5f, c.y - size * .5f, size, size), Texture2D.whiteTexture);
        GUI.color = old;
        if (focus)
        {
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(c.x - 160, c.y + 18, 320, 26), "[E] " + focus.Verb + (focus.Blocked ? " (blocked)" : ""), style);
        }
    }
}
