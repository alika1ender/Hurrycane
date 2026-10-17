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
    /// <summary>The first open task in list order: what the Day 1 phone shows.</summary>
    public TaskState Current => today.FirstOrDefault(t => t.status == TaskStatus.Open);

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
        foreach (var definition in all.Where(t => t.day == day && !t.disabled)) today.Add(new TaskState { definition = definition });
        openings = FindObjectsByType<BreakableOpening>(FindObjectsInactive.Include).ToDictionary(o => o.Id);
        stash = FindAnyObjectByType<ShelterStash>();
        radio = FindAnyObjectByType<RadioSwitch>();
        barrier = FindAnyObjectByType<StairBarrier>();
        flood = GetComponent<FloodSystem>();
        puddles = GetComponent<PuddleSystem>();
        doorGap = FindAnyObjectByType<DoorGap>();
        Evaluate();
    }

    void Update()
    {
        if (director.Phase <= DayPhase.AfterDisaster) Evaluate();
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
