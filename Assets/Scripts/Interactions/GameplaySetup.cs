using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds gameplay components to the generated house when a scene with a MapExplorer loads. Done at runtime instead of in
/// HouseMapBuilder so "Rebuild House Map" never wipes it and the scene file stays untouched.
/// </summary>
public static class GameplaySetup
{
    // sceneLoaded (not AfterSceneLoad) so it also runs when a restart reloads the house.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Setup(scene);

    static int FirstAidUses => DaysConfig.Load().health.firstAidUsesPerKit;

    // Things to switch off (Docs/Design/04): object, group, verb, done text, hold seconds, collision proxies that forward to it.
    // An object with no proxy gets a fitted box; the fridge uses its shell panels so its door still opens.
    static readonly (string obj, string group, string verb, string done, float hold, string[] proxies)[] Switches =
    {
        ("Living_TV_Animimo", Switchable.Appliances, "Unplug the TV", "TV unplugged", 1.5f, new string[0]),
        ("Kitchen_Fridge", Switchable.Appliances, "Unplug the fridge", "Fridge unplugged", 1.5f, new[] { "COL_FridgeShell_Left", "COL_FridgeShell_Right", "COL_FridgeShell_Top" }),
        ("Kitchen_Kettle", Switchable.Appliances, "Unplug the kettle", "Kettle unplugged", 1f, new string[0]),
        ("Basement_FusePanel", Switchable.Utilities, "Switch off the main breaker", "Main breaker off", 2f, new string[0]),
        ("Basement_UtilityTank", Switchable.Utilities, "Shut the utility valve", "Utility valve shut", 3f, new[] { "COL_Basement_UtilityTank" }),
    };

    static void AddSwitches(System.Collections.Generic.Dictionary<string, Transform> byName)
    {
        foreach (var s in Switches)
        {
            if (!byName.TryGetValue(s.obj, out var target)) { Debug.LogWarning("GameplaySetup: switch target missing " + s.obj); continue; }
            if (target.GetComponent<Switchable>()) continue;
            var switchable = target.gameObject.AddComponent<Switchable>();
            switchable.Configure(s.group, s.verb, s.done, s.hold);
            if (s.proxies.Length == 0) FitCollider(target);
            foreach (var proxyName in s.proxies)
                if (byName.TryGetValue(proxyName, out var proxy) && !proxy.GetComponent<InteractionProxy>())
                    proxy.gameObject.AddComponent<InteractionProxy>().Configure(switchable);
        }
    }

    public static void Setup(Scene scene)
    {
        MapExplorer explorer = null;
        foreach (var root in scene.GetRootGameObjects()) if (!explorer) explorer = root.GetComponentInChildren<MapExplorer>(true);
        if (!explorer) return;
        if (!explorer.GetComponent<PlayerInventory>()) explorer.gameObject.AddComponent<PlayerInventory>();
        var config = OpeningsConfig.Load();
        var openings = new System.Collections.Generic.Dictionary<string, OpeningDefinition>();
        foreach (var definition in config.openings) openings[definition.collider] = definition;
        ShelterStash stash = null; Transform stashProxy = null, workbench = null;
        var byName = new System.Collections.Generic.Dictionary<string, Transform>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!byName.ContainsKey(t.name)) byName[t.name] = t;
                if (t.name == "COL_Basement_ShelterStash") stashProxy = t;
                if (t.name == "Basement_Workbench_Visual") workbench = t;
                if (openings.TryGetValue(t.name, out var opening) && !t.GetComponent<BreakableOpening>())
                {
                    var breakable = t.gameObject.AddComponent<BreakableOpening>();
                    breakable.Configure(opening, config);
                }
                if (t.name == "Basement_ShelterStash") stash = t.GetComponent<ShelterStash>() ? t.GetComponent<ShelterStash>() : t.gameObject.AddComponent<ShelterStash>();
                if (!t.name.StartsWith("PICKUP_") || t.name.EndsWith("_Visual")) continue;
                var kind = Pickup.KindFromName(t.name);
                if (kind == null) { Debug.LogWarning("GameplaySetup: unknown pickup type for " + t.name); continue; }
                if (kind == ItemKind.Radio) { if (!t.GetComponent<RadioSwitch>()) t.gameObject.AddComponent<RadioSwitch>(); }
                else if (!t.GetComponent<Pickup>())
                    t.gameObject.AddComponent<Pickup>().Configure(kind.Value, kind == ItemKind.RepairTape ? config.tapeUsesPerRoll : kind == ItemKind.FirstAid ? FirstAidUses : 0);
                FitCollider(t);
            }
        // The invisible collision proxy sits in front of the stash mesh and is what the E ray hits.
        if (stash && stashProxy && !stashProxy.GetComponent<InteractionProxy>()) stashProxy.gameObject.AddComponent<InteractionProxy>().Configure(stash);
        AddSwitches(byName);
        var stack = Object.FindAnyObjectByType<PlankStack>();
        if (workbench && (!stack || stack.gameObject.scene != scene)) AddPlankStack(workbench, config.plankSetsInStack);
        if (!explorer.GetComponent<PauseMenu>()) explorer.gameObject.AddComponent<PauseMenu>();
        // Screen UI before the session starts, so it hears the first PhaseChanged.
        var existingUi = Object.FindAnyObjectByType<GameUI>();
        if (!existingUi || existingUi.gameObject.scene != scene)
        {
            var uiHost = new GameObject("GameUI");
            SceneManager.MoveGameObjectToScene(uiHost, scene);
            uiHost.AddComponent<GameUI>().Bind(explorer);
        }
        // Last: the session starts the day (or restores a restart) once everything above exists.
        var session = Object.FindAnyObjectByType<GameSession>();
        if (!session || session.gameObject.scene != scene)
        {
            var host = new GameObject("GameSession");
            SceneManager.MoveGameObjectToScene(host, scene);
            var newSession = host.AddComponent<GameSession>();
            host.AddComponent<DayDirector>().Bind(explorer);
            host.AddComponent<TaskEngine>().Bind(explorer);
            host.AddComponent<PulseSystem>().Bind(explorer);
            host.AddComponent<HealthSystem>().Bind(explorer);
            host.AddComponent<PowerSystem>().Bind(explorer);
            var director = host.GetComponent<DayDirector>();
            host.AddComponent<FloodSystem>().Bind(explorer, StairBarrier.Create(explorer, director.Config.flood));
            host.AddComponent<PuddleSystem>().Bind(explorer, DoorGap.Create(explorer, director.Config.doorGap));
            newSession.Begin(explorer, config.startDay);
        }
    }

    /// <summary>Placeholder plank stack on the left end of the basement workbench top.</summary>
    static void AddPlankStack(Transform workbench, int sets)
    {
        var top = workbench.GetComponent<Renderer>().bounds;
        var stack = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SceneManager.MoveGameObjectToScene(stack, workbench.gameObject.scene);
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
