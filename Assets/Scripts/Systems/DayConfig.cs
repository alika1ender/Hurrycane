using System;
using UnityEngine;

/// <summary>Phase lengths, break chances and damage per day, from Resources/HurrycaneData/days.json (Docs/Design/01, 03).</summary>
[Serializable]
public sealed class DaysConfig
{
    public float stormHoldMultiplier = 1.5f, warningLeadSeconds = 3, hitWindowStart = .15f, hitWindowEnd = .85f, minSecondsBetweenHits = 6;
    public float prepBreakWindowSeconds = 30, prepBreakFactor = .25f, injuryRadius = 2, nightSeconds = 6;
    public float recoveryEnough = 50, recoverySome = 30, recoveryNone = 10;
    public DayConfig[] days = new DayConfig[0];

    public DayConfig For(int day) => Array.Find(days, d => d.day == day) ?? (days.Length > 0 ? days[days.Length - 1] : new DayConfig());

    public static DaysConfig Load()
    {
        var file = Resources.Load<TextAsset>("HurrycaneData/days");
        if (!file) { Debug.LogError("days.json missing from Resources/HurrycaneData"); return new DaysConfig(); }
        return JsonUtility.FromJson<DaysConfig>(file.text);
    }
}

[Serializable]
public sealed class DayConfig
{
    public int day = 1;
    public string title = "", taskSource = "phone";
    public float prepSeconds = 90, disasterSeconds = 60, afterSeconds = 30;
    public bool alertFirst, prepBreaks;
    public BreakChances window = new BreakChances(), door = new BreakChances();
    public float glassDamage, doorDamage;
    public ScriptedBreak[] scripted = new ScriptedBreak[0];

    public float Length(DayPhase phase) => phase == DayPhase.Preparation ? prepSeconds : phase == DayPhase.Disaster ? disasterSeconds : phase == DayPhase.AfterDisaster ? afterSeconds : 0;

    /// <summary>Chance this opening gets hit in this day's storm (Docs/Design/03 table). Cracked rolls like unreinforced.</summary>
    public float ChanceFor(BreakableOpening opening)
    {
        var c = opening.IsDoor ? door : window;
        switch (opening.State)
        {
            case OpeningState.Unreinforced: case OpeningState.Cracked: return c.unreinforced;
            case OpeningState.Repaired: return c.repaired;
            case OpeningState.Reinforced: return c.reinforced;
            default: return 0; // out of play, or already broken
        }
    }
}

[Serializable] public sealed class BreakChances { public float unreinforced, repaired, reinforced; }

[Serializable]
public sealed class ScriptedBreak
{
    public string opening, result = "Cracked";
    /// <summary>Seconds into Disaster Time when the hit lands (the warning starts earlier).</summary>
    public float atSeconds;
}
