using System;
using System.Collections;
using UnityEngine;

public enum OpeningState { OutOfPlay, Unreinforced, Reinforced, Cracked, Broken, Repaired }

/// <summary>
/// One window or the front door (Docs/Design/03). Lives on the invisible COL_* proxy, which is what the E ray hits.
/// Reinforce/upgrade = toolkit + plank set; repair = toolkit + repair tape. Storm hits go through Hit()/Strike().
/// Visuals are placeholders (cube boards, crack lines, tape strips) until art delivers real states.
/// </summary>
[DisallowMultipleComponent]
public sealed class BreakableOpening : MonoBehaviour, IInteractable
{
    /// <summary>Raised on every state change (old, new). Health, pulse, score and the handbook map listen to this.</summary>
    public static event Action<BreakableOpening, OpeningState, OpeningState> StateChanged;
    /// <summary>Raised when the creak/shake warning starts, the given seconds before a hit lands.</summary>
    public static event Action<BreakableOpening, float> WarningStarted;

    [SerializeField] string id, room;
    [SerializeField] int firstDay;
    [SerializeField] bool isDoor;
    [SerializeField] OpeningState state;
    OpeningsConfig config;
    OpeningVisuals visuals;

    public string Id => id;
    public string Room => room;
    public int FirstDay => firstDay;
    public bool IsDoor => isDoor;
    public OpeningState State => state;
    public bool InPlay => state != OpeningState.OutOfPlay;
    /// <summary>Counts as intact for the score (cracked still keeps the weather out).</summary>
    public bool Intact => state != OpeningState.Broken;
    public bool Striking { get; private set; }
    /// <summary>World centre of the opening. Use this, not transform.position (COL_* proxies are pivoted elsewhere).</summary>
    public Vector3 Center => GetComponent<Collider>().bounds.center;
    /// <summary>
    /// Distance from a point to the opening's box. (Collider.ClosestPoint can't be used: the proxies are non-convex
    /// mesh colliders, for which it just returns the input point.)
    /// </summary>
    public float DistanceTo(Vector3 point) => Vector3.Distance(GetComponent<Collider>().bounds.ClosestPoint(point), point);

    public void Configure(OpeningDefinition definition, OpeningsConfig settings)
    {
        id = definition.id; room = definition.room; firstDay = definition.firstDay; isDoor = definition.isDoor;
        config = settings;
        visuals = new OpeningVisuals(GetComponent<Collider>(), isDoor);
        SetState(OpeningState.OutOfPlay);
    }

    /// <summary>Brings the opening into play on its first day (or takes it out for a restart).</summary>
    public void SetInPlay(bool inPlay)
    {
        if (inPlay && state == OpeningState.OutOfPlay) SetState(OpeningState.Unreinforced);
        else if (!inPlay) SetState(OpeningState.OutOfPlay);
    }

    /// <summary>Sets the state directly: snapshot restore, scripted events, tests.</summary>
    public void SetState(OpeningState next)
    {
        var previous = state;
        state = next;
        visuals?.Show(next);
        if (previous != next) StateChanged?.Invoke(this, previous, next);
    }

    /// <summary>Raised when a storm hit lands (not for restores or tests calling SetState). Health and pulse react to this.</summary>
    public static event Action<BreakableOpening, OpeningState> HitLanded;

    /// <summary>A storm hit lands now (no warning). Reinforced only cracks; everything else breaks.</summary>
    public OpeningState Hit() => Hit(null);
    /// <summary>A hit with a forced result, e.g. the Day 1 scripted crack.</summary>
    public OpeningState Hit(OpeningState? forced)
    {
        if (!InPlay) return state;
        if (state == OpeningState.Broken && forced == null) return state; // already open to the storm: no second injury
        SetState(forced ?? (state == OpeningState.Reinforced ? OpeningState.Cracked : OpeningState.Broken));
        HitLanded?.Invoke(this, state);
        return state;
    }

    /// <summary>Creak, shake and dust for the warning time, then Hit(). Used by the storm scheduler.</summary>
    public void Strike(OpeningState? forced = null)
    {
        if (InPlay && !Striking) StartCoroutine(StrikeRoutine(forced));
    }
    IEnumerator StrikeRoutine(OpeningState? forced)
    {
        Striking = true;
        float warning = config != null ? config.warningSeconds : 3;
        WarningStarted?.Invoke(this, warning);
        for (float t = 0; t < warning; t += Time.deltaTime)
        {
            visuals.Shake(Mathf.Lerp(.004f, .018f, t / warning));
            yield return null;
        }
        visuals.Shake(0);
        Striking = false;
        Hit(forced);
    }
    /// <summary>Ending cutscene: make intact glass see-through so the street outside shows (normal glass reads as daylight).</summary>
    public void SetGlassClear(bool clear) => visuals?.SetClearGlass(clear, state);

    /// <summary>Stops a strike that is still in its warning (the day ended or the game is over).</summary>
    public void CancelStrike()
    {
        if (!Striking) return;
        StopAllCoroutines();
        visuals.Shake(0);
        Striking = false;
    }
    [ContextMenu("Debug: Strike now")] void DebugStrike() => Strike();

    // ---- IInteractable ----
    enum Job { None, Reinforce, Repair }
    Job Next
    {
        get
        {
            switch (state)
            {
                case OpeningState.Unreinforced: case OpeningState.Repaired: return Job.Reinforce;
                case OpeningState.Cracked: case OpeningState.Broken: return Job.Repair;
                default: return Job.None;
            }
        }
    }
    string Noun => isDoor ? "front door" : "window";
    int TapeNeeded => isDoor ? config.doorRepairTapeUses : 1;

    /// <summary>Null hides the prompt entirely (out-of-play windows look like scenery).</summary>
    public string Prompt
    {
        get
        {
            switch (Next)
            {
                case Job.Reinforce: return isDoor ? "Brace front door" : "Board up window";
                case Job.Repair: return "Repair " + Noun + (state == OpeningState.Cracked ? " (cracked)" : " (broken)");
                default: return InPlay ? (isDoor ? "Front door braced" : "Window boarded up") : null;
            }
        }
    }
    public float HoldSeconds
    {
        get
        {
            switch (Next)
            {
                case Job.Reinforce: return isDoor ? config.braceDoorSeconds : state == OpeningState.Repaired ? config.upgradeWindowSeconds : config.reinforceWindowSeconds;
                case Job.Repair: return isDoor ? config.repairDoorSeconds : config.repairWindowSeconds;
                default: return 0;
            }
        }
    }
    public string Unavailable(MapExplorer user)
    {
        var inventory = user.Inventory;
        if (Next == Job.None) return InPlay ? "Already secure" : "";
        bool toolkit = inventory.Has(ItemKind.Toolkit);
        if (Next == Job.Reinforce)
        {
            bool planks = inventory.Has(ItemKind.PlankSet);
            return toolkit && planks ? null : "Need: " + (toolkit ? "" : "toolkit") + (toolkit || planks ? "" : ", ") + (planks ? "" : "planks");
        }
        bool tape = inventory.Uses(ItemKind.RepairTape) >= TapeNeeded;
        if (toolkit && tape) return null;
        string tapeText = TapeNeeded > 1 ? "repair tape (" + TapeNeeded + " uses)" : "repair tape";
        return "Need: " + (toolkit ? "" : "toolkit") + (toolkit || tape ? "" : ", ") + (tape ? "" : tapeText);
    }
    public void Use(MapExplorer user)
    {
        switch (Next)
        {
            case Job.Reinforce:
                if (user.Inventory.ConsumeArmful(ItemKind.PlankSet)) SetState(OpeningState.Reinforced);
                break;
            case Job.Repair:
                if (user.Inventory.ConsumeUses(ItemKind.RepairTape, TapeNeeded)) SetState(OpeningState.Repaired);
                break;
        }
    }
}
