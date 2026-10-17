using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds gameplay components to the generated house when a scene with a MapExplorer loads. Done at runtime instead of in
/// HouseMapBuilder so "Rebuild House Map" never wipes it and the scene file stays untouched.
/// </summary>
public static class GameplaySetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnSceneLoaded() => Setup(SceneManager.GetActiveScene());

    public static void Setup(Scene scene)
    {
        var explorer = Object.FindAnyObjectByType<MapExplorer>();
        if (!explorer) return;
        if (!explorer.GetComponent<PlayerInventory>()) explorer.gameObject.AddComponent<PlayerInventory>();
        var config = OpeningsConfig.Load();
        var openings = new System.Collections.Generic.Dictionary<string, OpeningDefinition>();
        foreach (var definition in config.openings) openings[definition.collider] = definition;
        ShelterStash stash = null; Transform stashProxy = null, workbench = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "COL_Basement_ShelterStash") stashProxy = t;
                if (t.name == "Basement_Workbench_Visual") workbench = t;
                if (openings.TryGetValue(t.name, out var opening) && !t.GetComponent<BreakableOpening>())
                {
                    var breakable = t.gameObject.AddComponent<BreakableOpening>();
                    breakable.Configure(opening, config);
                    breakable.SetInPlay(opening.firstDay <= config.startDay);
                }
                if (t.name == "Basement_ShelterStash") stash = t.GetComponent<ShelterStash>() ? t.GetComponent<ShelterStash>() : t.gameObject.AddComponent<ShelterStash>();
                if (!t.name.StartsWith("PICKUP_") || t.name.EndsWith("_Visual")) continue;
                var kind = Pickup.KindFromName(t.name);
                if (kind == null) { Debug.LogWarning("GameplaySetup: unknown pickup type for " + t.name); continue; }
                if (kind == ItemKind.Radio) { if (!t.GetComponent<RadioSwitch>()) t.gameObject.AddComponent<RadioSwitch>(); }
                else if (!t.GetComponent<Pickup>()) t.gameObject.AddComponent<Pickup>().Configure(kind.Value, kind == ItemKind.RepairTape ? config.tapeUsesPerRoll : 0);
                FitCollider(t);
            }
        // The invisible collision proxy sits in front of the stash mesh and is what the E ray hits.
        if (stash && stashProxy && !stashProxy.GetComponent<InteractionProxy>()) stashProxy.gameObject.AddComponent<InteractionProxy>().Configure(stash);
        if (workbench && !Object.FindAnyObjectByType<PlankStack>()) AddPlankStack(workbench, config.plankSetsInStack);
    }

    /// <summary>Placeholder plank stack on the left end of the basement workbench top.</summary>
    static void AddPlankStack(Transform workbench, int sets)
    {
        var top = workbench.GetComponent<Renderer>().bounds;
        var stack = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stack.name = "PlankStack";
        stack.GetComponent<Renderer>().sharedMaterial = PlankStack.Material();
        // Scale only on y as planks are taken, so pivot at the bottom by placing the centre half a height up.
        stack.transform.localScale = new Vector3(1.0f, .3f, .3f);
        stack.transform.position = new Vector3(top.min.x + .65f, top.max.y + .15f, top.center.z);
        stack.AddComponent<PlankStack>().Configure(sets);
    }

    /// <summary>Box collider around the item's meshes, so the E raycast can hit it.</summary>
    static void FitCollider(Transform item)
    {
        if (item.GetComponent<Collider>()) return;
        var filters = item.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0) return;
        Bounds local = default; bool any = false;
        foreach (var filter in filters)
        {
            if (!filter.sharedMesh) continue;
            var b = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = item.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (!any) { local = new Bounds(p, Vector3.zero); any = true; } else local.Encapsulate(p);
            }
        }
        if (!any) return;
        var box = item.gameObject.AddComponent<BoxCollider>();
        box.center = local.center;
        // Thin items (tape, batteries) get a minimum size so they are easy to aim at.
        box.size = Vector3.Max(local.size, Vector3.one * .06f);
    }
}
