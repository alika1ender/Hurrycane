using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class TaskDefinition
{
    public string id, text, hint, phases = "PDA", condition = "", appearsWhen = "";
    public int day;
    /// <summary>Workload estimate in Preparation seconds (Docs/Design/04). Informational.</summary>
    public float seconds;
    public bool required, disabled;
}

public enum TaskStatus { Hidden, Open, Done, Locked }

public sealed class TaskState
{
    public TaskDefinition definition;
    public TaskStatus status;
    public bool appeared;
    public bool completedAtDayStart;
}

/// <summary>
/// Today's task list from Resources/HurrycaneData/tasks.json (Docs/Design/04). Each task has a condition checked against
/// the live house every frame; once true the task is done for good. A task past its last allowed phase is locked ("Too late").
/// Condition words: alert, radio, carry:Kind[:n], stash:Kind[:n], supplies:Kind[:n], secured:IDs, fixed:ID, damaged:ID,
/// anyDamaged, noneDamaged, stairsBlocked, flooding, floodStopped, anyPuddles, noPuddles, doorGapBlocked, unplugged,
/// utilitiesOff, looseMoved:day, joined with &amp;.
/// </summary>
[RequireComponent(typeof(DayDirector))]
public sealed class TaskEngine : MonoBehaviour
{
    public static event Action<TaskState> TaskCompleted;

    [Serializable] sealed class TaskFile { public TaskDefinition[] tasks = new TaskDefinition[0]; }
    readonly List<TaskState> today = new List<TaskState>();
    TaskDefinition[] all;
    DayDirector director;
    MapExplorer player;
    Dictionary<string, BreakableOpening> openings = new Dictionary<string, BreakableOpening>();
    ShelterStash stash;
    RadioSwitch radio;
    StairBarrier barrier;
    FloodSystem flood;
    PuddleSystem puddles;
    DoorGap doorGap;

    public IReadOnlyList<TaskState> Today => today;
    bool loadingDay;
    // Intro acknowledgement and work already satisfied at dawn are not work done today.
    public int DailyWorkDone => today.Count(t => t.definition.condition != "alert" && !t.completedAtDayStart && t.status == TaskStatus.Done);
    public int DailyWorkAvailable => today.Count(t => t.definition.condition != "alert" && !t.completedAtDayStart && t.appeared);
    public void RefreshProgress() => Evaluate();

    // ---- one task at a time (every day) ----
    TaskState assigned;
    float assignedAt, targetsAt;
    readonly List<Component> targets = new List<Component>();
    static readonly TaskState StoreSupplies = new TaskState
    {
        status = TaskStatus.Open, appeared = true,
        definition = new TaskDefinition { id = "store", text = "Put the food and water in the supply box", hint = "Supply box in the master bedroom", condition = "" },
    };

    /// <summary>
    /// The one task the player is given right now. Urgent tasks (ones that appear mid-day: repairs, the flood, puddles)
    /// interrupt; otherwise list order. While carrying food or water the task still needs more of, it stays and the
    /// supply box glows along with the rest to collect; once the arms hold enough (or food/water the task doesn't need),
    /// "put it in the supply box" takes over.
    /// </summary>
    public TaskState Current => CarryingSupplies && !gathering ? StoreSupplies : assigned;
    /// <summary>Game time when the current task was given (resets when it changes).</summary>
    public float AssignedAt => assignedAt;
    /// <summary>Day 1 shows where at once; later days only after a delay (days.json locationDelaySeconds).</summary>
    public float RevealIn => Current == StoreSupplies ? 0 : Mathf.Max(0, director.Today.locationDelaySeconds - (Time.time - assignedAt));
    public bool LocationRevealed => RevealIn <= 0;
    /// <summary>What the current task points at (for the see-through-walls glow); empty until the location is revealed.</summary>
    public IReadOnlyList<Component> Targets => targets;

    bool CarryingSupplies => player && stash && player.Inventory.Armful.Any(p => p.Kind == ItemKind.Water || p.Kind == ItemKind.Food);
    /// <summary>The assigned task still wants more food/water than is carried + stored, and some is left to pick up.</summary>
    bool gathering;

    bool StillGathering(TaskState task)
    {
        if (task == null || !player || !stash) return false;
        foreach (var atom in task.definition.condition.Split('&'))
        {
            var parts = atom.Trim().Split(':');
            if (parts[0] != "supplies" && parts[0] != "stash") continue;
            if (!Kind(parts, out var kind) || (kind != ItemKind.Water && kind != ItemKind.Food)) continue;
            int amount = parts.Length > 2 && int.TryParse(parts[2], out var n) ? n : 1;
            if (player.Inventory.Count(kind) + stash.Count(kind) < amount && WorldPickups(kind).Any()) return true;
        }
        return false;
    }

    void Assign()
    {
        bool Urgent(TaskState t) => !string.IsNullOrEmpty(t.definition.appearsWhen);
        var urgent = today.FirstOrDefault(t => t.status == TaskStatus.Open && Urgent(t));
        TaskState next = assigned;
        if (next == null || next.status != TaskStatus.Open) next = urgent ?? today.FirstOrDefault(t => t.status == TaskStatus.Open);
        else if (urgent != null && !Urgent(next)) next = urgent;
        if (next != assigned) { assigned = next; assignedAt = Time.time; }
    }

    void Awake()
    {
        director = GetComponent<DayDirector>();
        var file = Resources.Load<TextAsset>("HurrycaneData/tasks");
        all = file ? JsonUtility.FromJson<TaskFile>(file.text).tasks : new TaskDefinition[0];
        if (!file) Debug.LogError("tasks.json missing from Resources/HurrycaneData");
    }
    void OnEnable() => GameSession.DayStarted += LoadDay;
    void OnDisable() => GameSession.DayStarted -= LoadDay;
    public void Bind(MapExplorer explorer) => player = explorer;

    void LoadDay(int day)
    {
        today.Clear();
        assigned = null; targets.Clear();
        foreach (var definition in all.Where(t => t.day == day && !t.disabled)) today.Add(new TaskState { definition = definition });
        openings = FindObjectsByType<BreakableOpening>(FindObjectsInactive.Include).ToDictionary(o => o.Id);
        stash = FindAnyObjectByType<ShelterStash>();
        radio = FindAnyObjectByType<RadioSwitch>();
        barrier = FindAnyObjectByType<StairBarrier>();
        flood = GetComponent<FloodSystem>();
        puddles = GetComponent<PuddleSystem>();
        doorGap = FindAnyObjectByType<DoorGap>();
        loadingDay = true;
        Evaluate();
        loadingDay = false;
    }

    void Update()
    {
        if (director.Phase > DayPhase.AfterDisaster) { targets.Clear(); return; }
        Evaluate();
        Assign();
        if (Time.time >= targetsAt) { targetsAt = Time.time + .25f; gathering = StillGathering(assigned); RefreshTargets(); }
    }

    // ---- what the current task points at ----

    void RefreshTargets()
    {
        targets.Clear();
        // Live flood water just pushed Cane back: point at the main breaker, whatever the task.
        var flood = director.GetComponent<FloodSystem>();
        if (flood && flood.BlockedRecently && flood.Breaker) targets.Add(flood.Breaker);
        var task = Current;
        if (task == null || !LocationRevealed || !player) return;
        if (task == StoreSupplies) { targets.Add(stash); return; }
        foreach (var atom in task.definition.condition.Split('&')) AddTargets(atom.Trim());
    }

    void AddTargets(string atom)
    {
        var parts = atom.Split(':');
        int amount = parts.Length > 2 && int.TryParse(parts[2], out var n) ? n : 1;
        var inventory = player.Inventory;
        switch (parts[0])
        {
            case "carry":
                if (!Kind(parts, out var k1) || inventory.Count(k1) >= amount) return;
                AddNearest(WorldPickups(k1), 3);
                if (k1 == ItemKind.PlankSet) { var plankStack = FindAnyObjectByType<PlankStack>(); if (plankStack && plankStack.Remaining > 0) targets.Add(plankStack); }
                return;
            case "supplies":
                if (!Kind(parts, out var k2) || inventory.Count(k2) + (stash ? stash.Count(k2) : 0) >= amount) return;
                AddNearest(WorldPickups(k2), 3);
                if (inventory.Has(k2) && stash && !targets.Contains(stash)) targets.Add(stash); // where they go
                return;
            case "stash":
                if (!Kind(parts, out var k3) || !stash || stash.Count(k3) >= amount) return;
                // Holding some: the box always shows; anything still missing shows too.
                if (inventory.Has(k3) && !targets.Contains(stash)) targets.Add(stash);
                if (inventory.Count(k3) + stash.Count(k3) < amount) AddNearest(WorldPickups(k3), 3);
                return;
            case "radio": if (radio && !radio.IsOn) targets.Add(radio); return;
            case "secured":
                var toSecure = Ids(parts).Where(o => o.State != OpeningState.Reinforced && o.State != OpeningState.Cracked).ToList();
                targets.AddRange(toSecure);
                if (toSecure.Count > 0) { NeedItem(ItemKind.Toolkit); NeedItem(ItemKind.PlankSet); } // what the job needs glows too
                return;
            case "fixed": case "anyDamaged": case "noneDamaged":
                var toFix = (parts[0] == "fixed" ? Ids(parts) : openings.Values.Where(o => o.InPlay)).Where(Damaged).ToList();
                targets.AddRange(toFix);
                if (toFix.Count > 0) { NeedItem(ItemKind.Toolkit); NeedItem(ItemKind.RepairTape); }
                return;
            case "stairsBlocked": case "floodStopped":
                if (!barrier || barrier.Blocked) return;
                targets.Add(barrier); if (!inventory.Has(ItemKind.Towel)) AddNearest(WorldPickups(ItemKind.Towel), 2);
                return;
            case "doorGapBlocked":
                if (!doorGap || doorGap.Blocked) return;
                targets.Add(doorGap); if (!inventory.Has(ItemKind.Towel)) AddNearest(WorldPickups(ItemKind.Towel), 2);
                return;
            case "unplugged": targets.AddRange(Switchable.All.Where(s => s.Group == Switchable.Appliances && !s.Off)); return;
            case "utilitiesOff": targets.AddRange(Switchable.All.Where(s => s.Group == Switchable.Utilities && !s.Off)); return;
            case "looseMoved": if (parts.Length > 1 && int.TryParse(parts[1], out var day)) targets.AddRange(LooseItem.All.Where(i => i.Day == day && !i.Moved)); return;
            case "noPuddles": case "anyPuddles":
                if (!puddles) return;
                int before = targets.Count;
                targets.AddRange(puddles.Puddles.Where(p => p));
                if (targets.Count > before) NeedItem(ItemKind.Towel);
                return;
        }
    }

    /// <summary>An item the current job needs and Cane isn't holding: the nearest ones glow wherever they are.</summary>
    void NeedItem(ItemKind kind)
    {
        if (player.Inventory.Has(kind)) return;
        AddNearest(WorldPickups(kind), 2);
        if (kind == ItemKind.PlankSet) { var plankStack = FindAnyObjectByType<PlankStack>(); if (plankStack && plankStack.Remaining > 0 && !targets.Contains(plankStack)) targets.Add(plankStack); }
    }

    IEnumerable<Pickup> WorldPickups(ItemKind kind) =>
        FindObjectsByType<Pickup>(FindObjectsInactive.Exclude).Where(p => p.Kind == kind && !p.Stash);

    void AddNearest(IEnumerable<Pickup> items, int count)
    {
        var from = player.transform.position;
        targets.AddRange(items.OrderBy(p => (p.transform.position - from).sqrMagnitude).Take(count));
    }

    void Evaluate()
    {
        if (!player) return;
        int phase = director.Phase == DayPhase.Disaster ? 1 : director.Phase == DayPhase.AfterDisaster ? 2 : 0;
        foreach (var task in today)
        {
            if (task.status == TaskStatus.Done || task.status == TaskStatus.Locked) continue;
            string phases = task.definition.phases ?? "PDA";
            int first = phases.Contains('P') ? 0 : phases.Contains('D') ? 1 : 2;
            int last = phases.Contains('A') ? 2 : phases.Contains('D') ? 1 : 0;
            if (phase > last) { task.status = TaskStatus.Locked; continue; }
            if (!task.appeared) task.appeared = string.IsNullOrEmpty(task.definition.appearsWhen) || Check(task.definition.appearsWhen);
            if (!task.appeared || phase < first) { task.status = TaskStatus.Hidden; continue; }
            if (Check(task.definition.condition))
            {
                task.status = TaskStatus.Done;
                task.completedAtDayStart = loadingDay;
                TaskCompleted?.Invoke(task);
            }
            else task.status = TaskStatus.Open;
        }
    }

    bool Check(string expression)
    {
        if (string.IsNullOrEmpty(expression)) return false;
        return expression.Split('&').All(part => Atom(part.Trim()));
    }

    bool Atom(string atom)
    {
        var parts = atom.Split(':');
        string word = parts[0];
        int amount = parts.Length > 2 && int.TryParse(parts[2], out var n) ? n : 1;
        switch (word)
        {
            case "alert": return director.AlertRead || director.Phase > DayPhase.Alert && !director.Today.alertFirst;
            case "radio": return radio && radio.IsOn;
            case "carry": return Kind(parts, out var k1) && player.Inventory.Count(k1) >= amount;
            case "stash": return Kind(parts, out var k2) && stash && stash.Count(k2) >= amount;
            case "supplies": return Kind(parts, out var k3) && player.Inventory.Count(k3) + (stash ? stash.Count(k3) : 0) >= amount;
            case "secured": return Ids(parts).All(o => o.State == OpeningState.Reinforced || o.State == OpeningState.Cracked);
            case "fixed": return Ids(parts).All(o => !Damaged(o));
            case "damaged": return Ids(parts).Any(Damaged);
            case "stairsBlocked": case "floodStopped": return barrier && barrier.Blocked;
            case "flooding": return flood && flood.Flooding;
            case "anyPuddles": return puddles && puddles.Any;
            case "noPuddles": return !puddles || !puddles.Any;
            case "doorGapBlocked": return doorGap && doorGap.Blocked;
            case "unplugged": return Switchable.AllOff(Switchable.Appliances);
            case "utilitiesOff": return Switchable.AllOff(Switchable.Utilities);
            case "looseMoved": return parts.Length > 1 && int.TryParse(parts[1], out var looseDay) && LooseItem.AllMoved(looseDay);
            case "anyDamaged": return openings.Values.Any(o => o.InPlay && Damaged(o));
            case "noneDamaged": return !openings.Values.Any(o => o.InPlay && Damaged(o));
            default: Debug.LogWarning("Unknown task condition: " + atom); return false;
        }
    }

    static bool Damaged(BreakableOpening o) => o.State == OpeningState.Cracked || o.State == OpeningState.Broken;
    static bool Kind(string[] parts, out ItemKind kind) { kind = default; return parts.Length > 1 && Enum.TryParse(parts[1], out kind); }
    IEnumerable<BreakableOpening> Ids(string[] parts) =>
        parts.Length > 1 ? parts[1].Split(',').Select(id => openings.TryGetValue(id.Trim(), out var o) ? o : null).Where(o => o) : Enumerable.Empty<BreakableOpening>();
}
