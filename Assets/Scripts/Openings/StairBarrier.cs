using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// The top of the basement stairs (Docs/Design/01, 04): hold E with towels in your arms to roll them across the top step.
/// Blocked before Day 3's flood, no water comes down; blocked during it, the rise stops. The player walks through it freely.
/// </summary>
[DisallowMultipleComponent]
public sealed class StairBarrier : MonoBehaviour, IInteractable
{
    public static event Action<bool> BlockedChanged;

    FloodSettings settings;
    MapExplorer player;
    GameObject rolls;

    public bool Blocked { get; private set; }
    /// <summary>Set by FloodSystem while water is coming down, so the prompt shows even without towels in hand.</summary>
    public bool Urgent { get; set; }

    /// <summary>Builds the barrier at the north (top) end of the basement stair ramp.</summary>
    public static StairBarrier Create(MapExplorer explorer, FloodSettings floodSettings)
    {
        var ramp = FindObjectsByType<Collider>(FindObjectsInactive.Exclude).FirstOrDefault(c => c.name == "COL_Basement_Stair_Ramp");
        if (!ramp) { Debug.LogWarning("StairBarrier: COL_Basement_Stair_Ramp not found"); return null; }
        var b = ramp.bounds;
        var go = new GameObject("BasementStairBarrier");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, explorer.gameObject.scene);
        go.transform.position = new Vector3(b.center.x, b.max.y + .2f, b.min.z + .2f);
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(b.size.x - .1f, .45f, .3f);
        Physics.IgnoreCollision(box, explorer.GetComponent<CharacterController>());
        var barrier = go.AddComponent<StairBarrier>();
        barrier.settings = floodSettings; barrier.player = explorer;
        barrier.BuildRolls(b.size.x);
        return barrier;
    }

    void BuildRolls(float width)
    {
        rolls = new GameObject("TowelRolls");
        rolls.transform.SetParent(transform, false);
        var material = GeneratedProps.TowelMaterial;
        for (int i = 0; i < 3; i++)
        {
            var roll = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyImmediate(roll.GetComponent<Collider>());
            roll.transform.SetParent(rolls.transform, false);
            roll.transform.localPosition = new Vector3((i - 1) * width * .3f, -.11f, 0);
            roll.transform.localRotation = Quaternion.Euler(0, 0, 90);
            roll.transform.localScale = new Vector3(.17f, width * .16f, .17f);
            roll.GetComponent<Renderer>().sharedMaterial = material;
        }
        rolls.SetActive(false);
    }

    public void SetBlocked(bool blocked)
    {
        Blocked = blocked;
        if (rolls) rolls.SetActive(blocked);
        BlockedChanged?.Invoke(blocked);
    }

    int TowelsInArms => player ? player.Inventory.Armful.Count(p => p.Kind == ItemKind.Towel) : 0;

    public string Prompt => Blocked ? "Stairs blocked with towels" : (TowelsInArms > 0 || Urgent) ? "Block the basement stairs with towels" : null;
    public float HoldSeconds => Blocked ? 0 : settings.blockHoldSeconds;
    public string Unavailable(MapExplorer user)
    {
        if (Blocked) return "Already blocked";
        return TowelsInArms >= settings.towelsToBlock ? null : "Need: " + settings.towelsToBlock + " towels in your arms (bathroom vanity, basement)";
    }
    public void Use(MapExplorer user)
    {
        if (Blocked) return;
        for (int i = 0; i < settings.towelsToBlock; i++) user.Inventory.ConsumeArmful(ItemKind.Towel);
        SetBlocked(true);
    }
}
