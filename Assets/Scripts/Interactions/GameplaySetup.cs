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

    /// <summary>Loose items from Resources/HurrycaneData/loose.json (Docs/Design/03, 04).</summary>
    static void AddLooseItems(System.Collections.Generic.Dictionary<string, Transform> byName)
    {
        var config = LooseConfig.Load();
        foreach (var definition in config.items)
        {
            if (!byName.TryGetValue(definition.@object, out var target)) { Debug.LogWarning("GameplaySetup: loose item missing " + definition.@object); continue; }
            if (target.GetComponent<LooseItem>()) continue;
            Transform proxy = null;
            if (!string.IsNullOrEmpty(definition.proxy)) byName.TryGetValue(definition.proxy, out proxy);
            var item = target.gameObject.AddComponent<LooseItem>();
            item.Configure(definition, config, proxy);
            if (proxy) { if (!proxy.GetComponent<InteractionProxy>()) proxy.gameObject.AddComponent<InteractionProxy>().Configure(item); }
            else FitCollider(target);
        }
    }

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
                // Batteries were cut (the flashlight no longer runs down): hide the scene's battery props.
                if (kind == ItemKind.Batteries) { t.gameObject.SetActive(false); continue; }
                if (kind == ItemKind.Radio) { if (!t.GetComponent<RadioSwitch>()) t.gameObject.AddComponent<RadioSwitch>(); }
                else if (!t.GetComponent<Pickup>())
                    t.gameObject.AddComponent<Pickup>().Configure(kind.Value, kind == ItemKind.RepairTape ? config.tapeUsesPerRoll : kind == ItemKind.FirstAid ? FirstAidUses : 0);
                FitCollider(t);
            }
        // The invisible collision proxy sits in front of the stash mesh and is what the E ray hits.
        if (stash && stashProxy && !stashProxy.GetComponent<InteractionProxy>()) stashProxy.gameObject.AddComponent<InteractionProxy>().Configure(stash);
        MoveShelterToMasterBedroom(byName);
        AddSwitches(byName);
        if (byName.TryGetValue("Living_TV_Animimo", out var television) && !television.GetComponent<TVBroadcast>())
            television.gameObject.AddComponent<TVBroadcast>();
        AddLooseItems(byName);
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
            host.AddComponent<HouseHealth>();
            host.AddComponent<HandheldSystem>().Bind(explorer);
            host.AddComponent<CutsceneSystem>().Bind(explorer);
            host.AddComponent<WindowWeather>();
            host.AddComponent<PickupHighlight>().Bind(explorer);
            host.AddComponent<TaskGlow>().Bind(explorer);
            newSession.Begin(explorer, config.startDay);
        }
    }

    /// <summary>
    /// The shelter is the master bedroom: the supply box (Basement_ShelterStash, its visual, its E proxy and marker) is
    /// moved from the basement onto free floor beside the master bed. The basement spot is kept for the two towels.
    /// </summary>
    static void MoveShelterToMasterBedroom(System.Collections.Generic.Dictionary<string, Transform> byName)
    {
        byName.TryGetValue("Basement_ShelterStash_Visual", out var visual);
        byName.TryGetValue("COL_Master_DoubleBed", out var bedProxy);
        byName.TryGetValue("20_Master_Bedroom_Visual", out var roomVisual);
        if (!visual || !bedProxy || !visual.GetComponent<Renderer>()) { Debug.LogWarning("Shelter move: stash or master bed not found"); return; }
        var box = visual.GetComponent<Renderer>().bounds;
        GeneratedProps.BasementTowelSpot = new Vector3(box.max.x + .35f, box.min.y, box.center.z);
        var bed = bedProxy.GetComponent<Collider>().bounds;
        var room = roomVisual && roomVisual.GetComponent<Renderer>() ? roomVisual.GetComponent<Renderer>().bounds : new Bounds(bed.center, bed.size * 3);

        Vector3? target = null;
        foreach (var side in new[] { Vector3.back, Vector3.forward, Vector3.right, Vector3.left })
            foreach (float along in new[] { 0f, .6f, -.6f })
            {
                if (target != null) break;
                var across = new Vector3(Mathf.Abs(side.z), 0, Mathf.Abs(side.x));
                var gap = Vector3.Scale(side, bed.extents + box.extents + Vector3.one * .2f);
                var spot = new Vector3(bed.center.x, bed.max.y + 1, bed.center.z) + new Vector3(gap.x, 0, gap.z) + Vector3.Scale(across, bed.extents) * along;
                if (!Physics.Raycast(spot, Vector3.down, out var floor, 3, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                if (floor.normal.y < .9f || Mathf.Abs(floor.point.y - bed.min.y) > .3f) continue;
                if (floor.point.x - box.extents.x < room.min.x + .1f || floor.point.x + box.extents.x > room.max.x - .1f
                    || floor.point.z - box.extents.z < room.min.z + .1f || floor.point.z + box.extents.z > room.max.z - .1f) continue;
                var centre = floor.point + Vector3.up * (box.extents.y + .07f);
                // Room for the box plus a little walking space, free of walls and furniture.
                if (Physics.CheckBox(centre, new Vector3(box.extents.x + .1f, box.extents.y, box.extents.z + .1f), Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                target = floor.point;
            }
        if (target == null) { Debug.LogWarning("Shelter move: no free floor beside the master bed; the supply box stays in the basement"); return; }

        var delta = target.Value - new Vector3(box.center.x, box.min.y, box.center.z);
        var parts = new System.Collections.Generic.List<Transform>();
        foreach (var name in new[] { "Basement_ShelterStash", "Basement_ShelterStash_Visual", "COL_Basement_ShelterStash", "MARKER_ShelterStash" })
            if (byName.TryGetValue(name, out var part)) parts.Add(part);
        foreach (var part in parts)
        {
            bool parentMoves = false;
            for (var p = part.parent; p; p = p.parent) if (parts.Contains(p)) parentMoves = true;
            if (!parentMoves) part.position += delta;
        }
        Physics.SyncTransforms();
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
