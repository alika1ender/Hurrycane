using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// The gap under the front door (Docs/Design/04, Day 2 task): hold E with a towel in your arms to stuff it.
/// For that day's storm the door's break chance drops by 10 points, and if it breaks anyway no puddles form.
/// Resets each morning (a new storm). The player walks through its collider.
/// </summary>
[DisallowMultipleComponent]
public sealed class DoorGap : MonoBehaviour, IInteractable
{
    public static event Action<bool> BlockedChanged;

    DoorGapSettings settings;
    MapExplorer player;
    GameObject towel;

    public bool Blocked { get; private set; }
    public float ChanceReduction => Blocked ? settings.chanceReduction : 0;

    public static DoorGap Create(MapExplorer explorer, DoorGapSettings gapSettings)
    {
        var door = FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude).FirstOrDefault(o => o.IsDoor);
        if (!door) { Debug.LogWarning("DoorGap: no front door opening"); return null; }
        var b = door.GetComponent<Collider>().bounds;
        var inward = new Vector3(0, 0, Mathf.Sign(-5 - b.center.z)); // the door is in the south wall
        var go = new GameObject("FrontDoorGap");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, explorer.gameObject.scene);
        go.transform.position = new Vector3(b.center.x, b.min.y + .12f, b.center.z) + inward * .25f;
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(b.size.x * .9f, .24f, .25f);
        Physics.IgnoreCollision(box, explorer.GetComponent<CharacterController>());
        var gap = go.AddComponent<DoorGap>();
        gap.settings = gapSettings; gap.player = explorer;
        gap.towel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        DestroyImmediate(gap.towel.GetComponent<Collider>());
        gap.towel.transform.SetParent(go.transform, false);
        gap.towel.transform.localPosition = new Vector3(0, -.04f, 0);
        gap.towel.transform.localRotation = Quaternion.Euler(0, 0, 90);
        gap.towel.transform.localScale = new Vector3(.15f, b.size.x * .42f, .15f);
        gap.towel.GetComponent<Renderer>().sharedMaterial = GeneratedProps.TowelMaterial;
        gap.towel.SetActive(false);
        return gap;
    }

    void OnEnable() => GameSession.DayStarted += OnDayStarted;
    void OnDisable() => GameSession.DayStarted -= OnDayStarted;
    void OnDayStarted(int day) => SetBlocked(false);

    public void SetBlocked(bool blocked)
    {
        Blocked = blocked;
        if (towel) towel.SetActive(blocked);
        BlockedChanged?.Invoke(blocked);
    }

    int TowelsInArms => player ? player.Inventory.Armful.Count(p => p.Kind == ItemKind.Towel) : 0;

    public string Prompt => Blocked ? "Door gap blocked with a towel" : TowelsInArms > 0 ? "Stuff a towel in the door gap" : null;
    public float HoldSeconds => Blocked ? 0 : settings.holdSeconds;
    public string Unavailable(MapExplorer user) => Blocked ? "Already blocked" : TowelsInArms >= settings.towels ? null : "Need: a towel in your arms";
    public void Use(MapExplorer user)
    {
        if (Blocked) return;
        for (int i = 0; i < settings.towels; i++) user.Inventory.ConsumeArmful(ItemKind.Towel);
        SetBlocked(true);
    }
}
