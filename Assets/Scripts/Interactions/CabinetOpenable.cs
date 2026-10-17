using UnityEngine;

/// <summary>
/// Moves a door/drawer pivot between the closed and open poses authored in Blender.
/// Poses are stored parent-local, so the motion is exact regardless of FBX axis conversion.
/// </summary>
[DisallowMultipleComponent]
public sealed class CabinetOpenable : MonoBehaviour
{
    [SerializeField] bool isDrawer;
    [SerializeField] bool isRoomDoor;
    [SerializeField, Min(.05f)] float duration = .45f;
    [SerializeField] Vector3 closedPosition, openPosition;
    [SerializeField] Quaternion closedRotation = Quaternion.identity, openRotation = Quaternion.identity;
    [SerializeField, Range(0, 1)] float amount;
    [SerializeField] bool targetOpen;

    static CharacterController player;
    static readonly Collider[] overlaps = new Collider[32];
    Collider[] colliders;

    public bool IsOpen => targetOpen;
    public bool IsMoving => !Mathf.Approximately(amount, targetOpen ? 1f : 0f);
    public bool IsDrawer => isDrawer;
    public bool IsRoomDoor => isRoomDoor;
    /// <summary>True when the last motion step was stopped because the player is pinned in the way.</summary>
    public bool Blocked { get; private set; }

    public void Configure(bool drawer, bool roomDoor, Vector3 closedPos, Quaternion closedRot, Vector3 openPos, Quaternion openRot, bool startOpen)
    {
        isDrawer = drawer; isRoomDoor = roomDoor;
        duration = drawer ? .35f : roomDoor ? .8f : .45f;
        closedPosition = closedPos; closedRotation = closedRot;
        openPosition = openPos; openRotation = openRot;
        targetOpen = startOpen; amount = startOpen ? 1 : 0;
        Apply(amount);
    }

    public string Verb => (targetOpen ? "Close " : "Open ") + (isDrawer ? "drawer" : "door");

    [ContextMenu("Toggle Open / Closed")]
    public void Toggle() => SetOpen(!targetOpen);
    public void SetOpen(bool open) { targetOpen = open; Blocked = false; }

    /// <summary>Jumps straight to a pose (0 closed, 1 open) and stops there; used by tests and tools.</summary>
    public void Snap(float t)
    {
        amount = Mathf.Clamp01(t); targetOpen = amount >= .5f; Blocked = false;
        Apply(amount);
    }

    void Awake() => colliders = GetComponentsInChildren<Collider>(true);

    void Update()
    {
        float goal = targetOpen ? 1f : 0f;
        if (Mathf.Approximately(amount, goal)) return;
        float previous = amount;
        Matrix4x4 before = transform.localToWorldMatrix;
        amount = Mathf.MoveTowards(amount, goal, Time.deltaTime / duration);
        Apply(amount);
        Blocked = HitsPlayer() && !PushPlayer(before);
        if (Blocked) { amount = previous; Apply(amount); }
    }

    /// <summary>Moves the player out of the way of the moving part. False if the player is pinned (e.g. against a wall).</summary>
    bool PushPlayer(Matrix4x4 before)
    {
        // Direction the part's surface moves at the player's position this frame.
        Vector3 center = player.transform.TransformPoint(player.center);
        Vector3 attached = before.MultiplyPoint3x4(transform.worldToLocalMatrix.MultiplyPoint3x4(center));
        Vector3 push = center - attached; push.y = 0;
        if (push.sqrMagnitude < 1e-8f) { push = center - transform.position; push.y = 0; }
        push.Normalize();
        for (int i = 0; i < 12; i++)
        {
            Vector3 start = player.transform.position;
            player.Move(push * .02f);
            if (!HitsPlayer()) return true;
            if ((player.transform.position - start).sqrMagnitude < 1e-6f) return false;
        }
        return false;
    }

    void Apply(float t)
    {
        float eased = t * t * (3f - 2f * t);
        transform.localPosition = Vector3.LerpUnclamped(closedPosition, openPosition, eased);
        transform.localRotation = Quaternion.SlerpUnclamped(closedRotation, openRotation, eased);
    }

    /// <summary>Whether the part currently overlaps the player's capsule.</summary>
    bool HitsPlayer()
    {
        if (!player) player = FindAnyObjectByType<CharacterController>();
        if (!player || !player.enabled || colliders == null || colliders.Length == 0) return false;
        Physics.SyncTransforms();
        var t = player.transform;
        float radius = player.radius * Mathf.Max(t.lossyScale.x, t.lossyScale.z);
        float half = Mathf.Max(player.height * t.lossyScale.y * .5f - radius, 0);
        Vector3 center = t.TransformPoint(player.center);
        int count = Physics.OverlapCapsuleNonAlloc(center + Vector3.up * half, center - Vector3.up * half, radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (overlaps[i] != player && overlaps[i].transform.IsChildOf(transform)) return true;
        return false;
    }
}
