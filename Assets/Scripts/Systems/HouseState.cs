using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Everything needed to restart a day (Docs/Design/06, 09). Saved as JSON at the start of each day.</summary>
[Serializable]
public sealed class HouseSnapshot
{
    public int day;
    public string savedAt;
    public float health, pulse, phoneBattery, flashlightCharge = 100;
    public bool stairsBlocked;
    public PuddleRecord[] puddles = new PuddleRecord[0];
    /// <summary>Names of Switchables that are off (unplugged / shut).</summary>
    public string[] switchedOff = new string[0];
    /// <summary>Names of loose items already moved away from their windows.</summary>
    public string[] movedLoose = new string[0];
    public Vector3 playerPosition;
    public float playerYaw;
    public int plankStackRemaining;
    public bool radioOn;
    public OpeningRecord[] openings;
    public PickupRecord[] pickups;
    // Score totals so far (Docs/Design/08).
    public int tasksDone, tasksRequired, hits;
    public string[] reinforcedIds = new string[0];
    // Card summary for the restart menu.
    public int reinforced, water, food;
}

[Serializable] public sealed class OpeningRecord { public string id; public OpeningState state; }

public enum PickupPlace { World, Pockets, Armful, Stash }

[Serializable]
public sealed class PickupRecord
{
    public string name;
    public bool generated;
    /// <summary>For generated items: which prop to recreate.</summary>
    public ItemKind kind;
    public PickupPlace place;
    /// <summary>Name of the parent transform for World items ("" = scene root), e.g. a drawer.</summary>
    public string parent;
    public Vector3 localPosition;
    public Quaternion localRotation;
    public int usesLeft;
}

/// <summary>Captures the live house into a HouseSnapshot and puts a snapshot back into a freshly loaded scene.</summary>
public static class HouseState
{
    public static HouseSnapshot Capture(GameSession session, MapExplorer player)
    {
        var inventory = player.Inventory;
        var openings = UnityEngine.Object.FindObjectsByType<BreakableOpening>(FindObjectsInactive.Include);
        var pickups = new List<PickupRecord>();
        foreach (var item in UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsInactive.Include))
        {
            PickupPlace place;
            if (inventory.Pockets.Contains(item)) place = PickupPlace.Pockets;
            else if (inventory.Armful.Contains(item)) place = PickupPlace.Armful;
            else if (item.Stash && item.gameObject.activeInHierarchy) place = PickupPlace.Stash;
            else if (item.gameObject.activeInHierarchy) place = PickupPlace.World;
            else continue;
            var t = item.transform;
            pickups.Add(new PickupRecord
            {
                name = item.name, generated = item.Generated, kind = item.Kind, place = place, usesLeft = item.UsesLeft,
                parent = t.parent ? t.parent.name : "", localPosition = t.localPosition, localRotation = t.localRotation,
            });
        }
        var stash = UnityEngine.Object.FindAnyObjectByType<ShelterStash>();
        var stack = UnityEngine.Object.FindAnyObjectByType<PlankStack>();
        var radio = UnityEngine.Object.FindAnyObjectByType<RadioSwitch>();
        return new HouseSnapshot
        {
            day = session.CurrentDay,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            health = session.health, pulse = session.pulse, phoneBattery = session.phoneBattery, flashlightCharge = session.flashlightCharge,
            stairsBlocked = UnityEngine.Object.FindAnyObjectByType<StairBarrier>() is StairBarrier barrier && barrier.Blocked,
            puddles = session.GetComponent<PuddleSystem>() is PuddleSystem puddleSystem ? puddleSystem.Records() : new PuddleRecord[0],
            switchedOff = Switchable.All.Where(s => s.Off).Select(s => s.Key).ToArray(),
            movedLoose = LooseItem.All.Where(i => i.Moved).Select(i => i.Key).ToArray(),
            tasksDone = session.tasksDone, tasksRequired = session.tasksRequired, hits = session.hits,
            reinforcedIds = session.reinforcedIds.ToArray(),
            playerPosition = player.transform.position, playerYaw = player.transform.eulerAngles.y,
            plankStackRemaining = stack ? stack.Remaining : 0,
            radioOn = radio && radio.IsOn,
            openings = openings.Select(o => new OpeningRecord { id = o.Id, state = o.State }).ToArray(),
            pickups = pickups.ToArray(),
            reinforced = openings.Count(o => o.State == OpeningState.Reinforced),
            water = inventory.Count(ItemKind.Water) + (stash ? stash.Count(ItemKind.Water) : 0),
            food = inventory.Count(ItemKind.Food) + (stash ? stash.Count(ItemKind.Food) : 0),
        };
    }

    /// <summary>Applies a snapshot to the scene as it is right after loading (all props in their original places).</summary>
    public static void Apply(HouseSnapshot snapshot, GameSession session, MapExplorer player)
    {
        var inventory = player.Inventory;
        inventory.Clear();
        var stash = UnityEngine.Object.FindAnyObjectByType<ShelterStash>();
        if (stash) stash.ClearStored();

        var scenePickups = new Dictionary<string, Pickup>();
        foreach (var item in UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsInactive.Include))
        {
            if (item.Generated) UnityEngine.Object.Destroy(item.gameObject);
            else scenePickups[item.name] = item;
        }
        // Scene items missing from the snapshot were used up (e.g. empty tape rolls).
        var kept = new HashSet<string>(snapshot.pickups.Where(r => !r.generated).Select(r => r.name));
        foreach (var pair in scenePickups) if (!kept.Contains(pair.Key)) UnityEngine.Object.Destroy(pair.Value.gameObject);

        var transforms = new Dictionary<string, Transform>();
        foreach (var root in player.gameObject.scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (!transforms.ContainsKey(t.name)) transforms[t.name] = t;

        foreach (var record in snapshot.pickups)
        {
            Pickup item;
            // Saves from before "kind" was recorded read as Water (0); those were always plank sets.
            if (record.generated) { item = GeneratedProps.Create(record.kind == ItemKind.Water ? ItemKind.PlankSet : record.kind); if (!item) continue; }
            else if (!scenePickups.TryGetValue(record.name, out item)) { Debug.LogWarning("Snapshot pickup not in scene: " + record.name); continue; }
            item.UsesLeft = record.usesLeft;
            item.Stash = null;
            switch (record.place)
            {
                case PickupPlace.Pockets: case PickupPlace.Armful: inventory.Take(item); break;
                case PickupPlace.Stash: if (stash) stash.Store(item); break;
                default:
                    transforms.TryGetValue(record.parent ?? "", out var parent);
                    item.transform.SetParent(string.IsNullOrEmpty(record.parent) ? null : parent, false);
                    item.transform.localPosition = record.localPosition;
                    item.transform.localRotation = record.localRotation;
                    item.gameObject.SetActive(true);
                    break;
            }
        }

        var states = snapshot.openings.ToDictionary(o => o.id, o => o.state);
        foreach (var opening in UnityEngine.Object.FindObjectsByType<BreakableOpening>(FindObjectsInactive.Include))
            if (states.TryGetValue(opening.Id, out var state)) opening.SetState(state);

        var stack = UnityEngine.Object.FindAnyObjectByType<PlankStack>();
        if (stack) stack.SetRemaining(snapshot.plankStackRemaining);
        var radio = UnityEngine.Object.FindAnyObjectByType<RadioSwitch>();
        if (radio) radio.IsOn = snapshot.radioOn;

        session.health = snapshot.health; session.pulse = snapshot.pulse; session.phoneBattery = snapshot.phoneBattery; session.flashlightCharge = snapshot.flashlightCharge;
        var barrierNow = UnityEngine.Object.FindAnyObjectByType<StairBarrier>();
        if (barrierNow) barrierNow.SetBlocked(snapshot.stairsBlocked);
        var puddleSystem = session.GetComponent<PuddleSystem>();
        if (puddleSystem) puddleSystem.Restore(snapshot.puddles);
        var off = new HashSet<string>(snapshot.switchedOff ?? new string[0]);
        foreach (var s in Switchable.All) s.SetOff(off.Contains(s.Key));
        var moved = new HashSet<string>(snapshot.movedLoose ?? new string[0]);
        foreach (var item in LooseItem.All.ToList()) if (moved.Contains(item.Key)) item.MoveAway(); // fresh scene: items start in place
        session.tasksDone = snapshot.tasksDone; session.tasksRequired = snapshot.tasksRequired; session.hits = snapshot.hits;
        session.reinforcedIds = new HashSet<string>(snapshot.reinforcedIds ?? new string[0]);
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.SetPositionAndRotation(snapshot.playerPosition, Quaternion.Euler(0, snapshot.playerYaw, 0));
        controller.enabled = true;
        Physics.SyncTransforms();
    }
}
