using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public enum DayPhase { Alert, Preparation, Disaster, AfterDisaster, Night, Ended, GameOver }

/// <summary>
/// Runs a day (Docs/Design/01, 03): optional alert, then Preparation → Disaster → After-Disaster on the clock only,
/// storm break rolls with warnings, injuries from nearby breaks, the night with overnight recovery, then the next day.
/// </summary>
[RequireComponent(typeof(GameSession))]
public sealed class DayDirector : MonoBehaviour
{
    public static event Action<DayPhase> PhaseChanged;

    /// <summary>Speeds up the clock for testing. Leave at 1.</summary>
    public float debugTimeScale = 1;

    struct PlannedHit { public float warnAt; public BreakableOpening opening; public OpeningState? forced; }
    readonly List<PlannedHit> schedule = new List<PlannedHit>();
    DaysConfig config;
    GameSession session;
    MapExplorer player;
    float phaseClock;

    public DayPhase Phase { get; private set; } = DayPhase.Ended;
    public float TimeLeft { get; private set; }
    public bool AlertRead { get; private set; }
    /// <summary>Text for the night card / ending card.</summary>
    public string NightText { get; private set; } = "";
    public DayConfig Today => config.For(session.CurrentDay);
    /// <summary>The final score, set when Day 3 ends (Docs/Design/08).</summary>
    public ScoreReport Score { get; private set; }
    public DaysConfig Config => config;
    public int PlannedHits => schedule.Count;

    void Awake() { config = DaysConfig.Load(); session = GetComponent<GameSession>(); }
    void OnEnable()
    {
        GameSession.DayStarting += OnDayStarting;
        GameSession.DayStarted += OnDayStarted;
        BreakableOpening.HitLanded += OnHitLanded;
    }
    void OnDisable()
    {
        GameSession.DayStarting -= OnDayStarting;
        GameSession.DayStarted -= OnDayStarted;
        BreakableOpening.HitLanded -= OnHitLanded;
    }
    public void Bind(MapExplorer explorer) => player = explorer;

    // A fresh Day 1 (not a restore) stages the toolkit and 4 plank sets at the top of the basement stairs (Docs/Design/04).
    // Later mornings: Cane wakes by the master bed (Docs/Design/01), before the autosave.
    void OnDayStarting(int day)
    {
        if (day == 1) StageDay1();
        else PlaceAtBedside();
    }

    void OnDayStarted(int day)
    {
        AlertRead = false;
        if (Today.alertFirst) { SetControl(false); EnterPhase(DayPhase.Alert); }
        else { SetControl(true); EnterPhase(DayPhase.Preparation); }
    }

    /// <summary>"Tapping" the government alert starts the clock.</summary>
    public void DismissAlert()
    {
        if (Phase != DayPhase.Alert) return;
        AlertRead = true;
        SetControl(true);
        EnterPhase(DayPhase.Preparation);
    }

    void Update()
    {
        if (Phase == DayPhase.Alert)
        {
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if ((keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) || (mouse != null && mouse.leftButton.wasPressedThisFrame))
                DismissAlert();
            return;
        }
        if (Phase == DayPhase.Ended || Phase == DayPhase.GameOver) return;
        float dt = Time.deltaTime * debugTimeScale;
        TimeLeft -= dt;
        if (Phase == DayPhase.Night) { if (TimeLeft <= 0) session.StartDay(session.CurrentDay + 1); return; }
        phaseClock += dt;
        for (int i = schedule.Count - 1; i >= 0; i--)
            if (schedule[i].warnAt <= phaseClock) { schedule[i].opening.Strike(schedule[i].forced); schedule.RemoveAt(i); }
        if (TimeLeft <= 0) NextPhase();
    }

    void NextPhase()
    {
        if (Phase == DayPhase.Preparation) EnterPhase(DayPhase.Disaster);
        else if (Phase == DayPhase.Disaster) EnterPhase(DayPhase.AfterDisaster);
        else if (Phase == DayPhase.AfterDisaster) StartNight();
    }

    void EnterPhase(DayPhase phase)
    {
        Phase = phase; phaseClock = 0; schedule.Clear();
        TimeLeft = Today.Length(phase);
        // Unfinished work costs more once the storm is here (Docs/Design/03: x1.5 in Disaster and After).
        if (player) player.holdTimeMultiplier = phase == DayPhase.Disaster || phase == DayPhase.AfterDisaster ? config.stormHoldMultiplier : 1;
        if (phase == DayPhase.Preparation && Today.prepBreaks) PlanPrepBreaks();
        if (phase == DayPhase.Disaster) PlanStorm();
        PhaseChanged?.Invoke(phase);
    }

    /// <summary>Rolls every in-play opening once; hits land between 15% and 85% of Disaster Time, at least 6 s apart.</summary>
    void PlanStorm()
    {
        var today = Today;
        var openings = FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude);
        var scripted = new HashSet<string>(today.scripted.Select(s => s.opening));
        var hits = new List<KeyValuePair<float, PlannedHit>>();
        float length = today.disasterSeconds;
        foreach (var opening in openings)
        {
            // Skip ones mid-warning from a Preparation break, or they would be hit twice.
            if (!opening.InPlay || opening.Striking || scripted.Contains(opening.Id)) continue;
            if (UnityEngine.Random.value < today.ChanceFor(opening))
                hits.Add(Hit(UnityEngine.Random.Range(config.hitWindowStart * length, config.hitWindowEnd * length), opening, null));
        }
        foreach (var s in today.scripted)
        {
            var opening = openings.FirstOrDefault(o => o.Id == s.opening);
            if (opening && Enum.TryParse(s.result, out OpeningState result)) hits.Add(Hit(s.atSeconds, opening, result));
        }
        hits.Sort((a, b) => a.Key.CompareTo(b.Key));
        float previous = float.NegativeInfinity;
        foreach (var hit in hits)
        {
            float at = Mathf.Max(hit.Key, previous + config.minSecondsBetweenHits);
            previous = at;
            var planned = hit.Value; planned.warnAt = Mathf.Max(0, at - config.warningLeadSeconds);
            schedule.Add(planned);
        }
    }

    /// <summary>As the wind rises, unreinforced openings may break in the last 30 s of Preparation (Days 2-3).</summary>
    void PlanPrepBreaks()
    {
        float length = Today.prepSeconds;
        foreach (var opening in FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude))
        {
            if (opening.State != OpeningState.Unreinforced) continue;
            if (UnityEngine.Random.value >= Today.ChanceFor(opening) * config.prepBreakFactor) continue;
            float at = UnityEngine.Random.Range(Mathf.Max(0, length - config.prepBreakWindowSeconds), length);
            schedule.Add(new PlannedHit { warnAt = Mathf.Max(0, at - config.warningLeadSeconds), opening = opening });
        }
    }

    static KeyValuePair<float, PlannedHit> Hit(float at, BreakableOpening opening, OpeningState? forced) =>
        new KeyValuePair<float, PlannedHit>(at, new PlannedHit { opening = opening, forced = forced });

    /// <summary>A break near Cane hurts (Docs/Design/06). Cracks behind boards don't.</summary>
    void OnHitLanded(BreakableOpening opening, OpeningState result)
    {
        if (result == OpeningState.Cracked || result == OpeningState.Broken) session.hits++;
        if (result != OpeningState.Broken || !player) return;
        var closest = opening.GetComponent<Collider>().ClosestPoint(player.viewCamera.transform.position);
        if (Vector3.Distance(closest, player.viewCamera.transform.position) > config.injuryRadius) return;
        session.health = Mathf.Max(0, session.health - (opening.IsDoor ? Today.doorDamage : Today.glassDamage));
    }

    void StartNight()
    {
        CancelStrikes();
        if (Today.faintIsGameOver && session.health <= 0) { GameOver(); return; }
        SetControl(false);
        var tasks = GetComponent<TaskEngine>();
        if (tasks)
        {
            session.tasksRequired += tasks.Today.Count(t => t.definition.required);
            session.tasksDone += tasks.Today.Count(t => t.definition.required && t.status == TaskStatus.Done);
        }
        if (session.CurrentDay >= 3)
        {
            Score = global::Score.Compute(session, player);
            Phase = DayPhase.Ended;
            NightText = "The storm has passed\n\nThe city outside is in ruins. Your house is still standing.";
            PhaseChanged?.Invoke(Phase);
            return;
        }
        NightText = "Day " + session.CurrentDay + " survived\n\n" + OvernightRecovery();
        Phase = DayPhase.Night;
        TimeLeft = config.nightSeconds;
        PhaseChanged?.Invoke(Phase);
    }

    /// <summary>Cane uses 1 water and 1 food, from the shelter first, then from his arms (Docs/Design/06).</summary>
    string OvernightRecovery()
    {
        var stash = FindAnyObjectByType<ShelterStash>();
        bool water = (stash && stash.Consume(ItemKind.Water)) || player.Inventory.ConsumeArmful(ItemKind.Water);
        bool food = (stash && stash.Consume(ItemKind.Food)) || player.Inventory.ConsumeArmful(ItemKind.Food);
        float gain = water && food ? config.recoveryEnough : water || food ? config.recoverySome : config.recoveryNone;
        session.health = Mathf.Min(100, session.health + gain);
        string what = water && food ? "Water and food" : water ? "Only water" : food ? "Only food" : "Nothing to eat or drink";
        return what + ": +" + Mathf.RoundToInt(gain) + " health";
    }

    void CancelStrikes()
    {
        foreach (var opening in FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude)) opening.CancelStrike();
    }

    /// <summary>Day 3 at zero health: the restart menu, no score (Docs/Design/06).</summary>
    public void GameOver()
    {
        if (Phase == DayPhase.GameOver) return;
        schedule.Clear();
        CancelStrikes();
        SetControl(false);
        Phase = DayPhase.GameOver;
        PhaseChanged?.Invoke(Phase);
    }

    /// <summary>Stands Cane on the floor beside the master bed, facing it (morning wake-up and after fainting).</summary>
    public void PlaceAtBedside()
    {
        var bed = FindObjectsByType<Collider>(FindObjectsInactive.Exclude).FirstOrDefault(c => c.name == "COL_Master_DoubleBed");
        if (!bed || !player) return;
        var b = bed.bounds;
        var controller = player.GetComponent<CharacterController>();
        foreach (var side in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
        {
            var spot = new Vector3(b.center.x, b.max.y, b.center.z) + Vector3.Scale(side, b.extents + Vector3.one * .55f);
            if (!Physics.Raycast(spot + Vector3.up * .5f, Vector3.down, out var floor, 3, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            if (Mathf.Abs(floor.point.y - b.min.y) > .3f) continue; // landed on furniture, not the floor
            var bottom = floor.point + Vector3.up * (controller.radius + .05f);
            var top = floor.point + Vector3.up * (controller.height - controller.radius);
            if (Physics.CheckCapsule(bottom, top, controller.radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            controller.enabled = false;
            var facing = b.center - floor.point; facing.y = 0;
            player.transform.SetPositionAndRotation(floor.point + Vector3.up * .05f, Quaternion.LookRotation(facing.normalized));
            controller.enabled = true;
            Physics.SyncTransforms();
            return;
        }
        Debug.LogWarning("No free spot beside the master bed");
    }

    void SetControl(bool on)
    {
        if (!player) return;
        player.enabled = on;
        if (on) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    void StageDay1()
    {
        var all = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        var marker = all.FirstOrDefault(t => t.name == "MARKER_BasementEntry");
        if (!marker) { Debug.LogWarning("Day 1 staging: MARKER_BasementEntry not found"); return; }
        Vector3 spot = FlatFloorNear(marker.position);
        var toolkit = FindObjectsByType<Pickup>(FindObjectsInactive.Include).FirstOrDefault(p => p.name == "PICKUP_Basement_Toolkit_02");
        if (toolkit) toolkit.PlaceAt(spot + Vector3.up, null, true);
        for (int i = 0; i < 4; i++) PlankStack.CreatePlankSet().PlaceAt(spot + new Vector3(.45f, 1 + i * .1f, 0), null, true);
        var stack = FindAnyObjectByType<PlankStack>();
        if (stack) stack.SetRemaining(stack.Remaining - 4);
    }

    /// <summary>A spot near the marker where a 1.2 x 0.6 m patch is level with it (not over the stairwell).</summary>
    static Vector3 FlatFloorNear(Vector3 marker)
    {
        for (float radius = 1; radius <= 3; radius += .5f)
            for (int step = 0; step < 12; step++)
            {
                float angle = step * 30 * Mathf.Deg2Rad;
                var spot = marker + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                bool level = true;
                foreach (var corner in new[] { new Vector3(-.2f, 0, -.3f), new Vector3(.8f, 0, -.3f), new Vector3(-.2f, 0, .3f), new Vector3(.8f, 0, .3f), new Vector3(.3f, 0, 0) })
                {
                    var from = spot + corner + Vector3.up * 1.2f;
                    if (!Physics.Raycast(from, Vector3.down, out var hit, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                        || Mathf.Abs(hit.point.y - marker.y) > .04f || hit.distance < 1.1f) { level = false; break; }
                }
                if (level) return spot;
            }
        return marker;
    }

    [ContextMenu("Debug: Skip to next phase")] void DebugSkip() { if (Phase == DayPhase.Alert) DismissAlert(); else TimeLeft = 0; }
}
