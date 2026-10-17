using System;
using UnityEngine;

/// <summary>Phase lengths, break chances and damage per day, from Resources/HurrycaneData/days.json (Docs/Design/01, 03).</summary>
[Serializable]
public sealed class DaysConfig
{
    public float stormHoldMultiplier = 1.5f, warningLeadSeconds = 3, hitWindowStart = .15f, hitWindowEnd = .85f, minSecondsBetweenHits = 6;
    public float prepBreakWindowSeconds = 30, prepBreakFactor = .25f, injuryRadius = 2, nightSeconds = 6;
    public float recoveryEnough = 50, recoverySome = 30, recoveryNone = 10;
    public PulseSettings pulse = new PulseSettings();
    public HealthSettings health = new HealthSettings();
    public PowerSettings power = new PowerSettings();
    public FloodSettings flood = new FloodSettings();
    public PuddleSettings puddles = new PuddleSettings();
    public DoorGapSettings doorGap = new DoorGapSettings();
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
    // Pulse (Docs/Design/05) and health (06).
    public float restingPrep = 72, restingDisaster = 80, restingAfter = 72, pulseCeiling = 95, windowBreakSpike = 5, doorBreakSpike = 8;
    public bool pulseVisuals, pulsePenalties, sprintRaisesPulse, faintIsGameOver;
    /// <summary>Blackout (Day 2 on) and basement flooding (Day 3), Docs/Design/01.</summary>
    public bool powerOut, floods;

    public float Resting(DayPhase phase) => phase == DayPhase.Disaster ? restingDisaster : phase == DayPhase.AfterDisaster ? restingAfter : restingPrep;

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

[Serializable]
public sealed class PulseSettings
{
    public float driftPerSecond = 3, nearbyRadius = 4, taskDoneDrop = 6, safeRoomDropPerSecond = 5, safeRoomBelowY = -1.5f;
    public float breathWindupSeconds = 4, breathMaxSeconds = 4, breathDropPerSecond = 8, breathCooldownSeconds = 20;
    public float lastSecondsRisePerSecond = 2, unfinishedTaskRise = 2, unfinishedTaskRiseMax = 12, sprintRisePerSecond = 6;
    public float panicTaskPenalty = .15f, peakTaskPenalty = .30f;
}

[Serializable]
public sealed class PowerSettings
{
    public float ambientFactor = .3f, flashlightDrainPerSecond = .55f, darknessRestingRise = 10, flashlightRestingRise = 6;
}

[Serializable]
public sealed class FloodSettings
{
    public float startSeconds = 40, earlierPerBreachSeconds = 10, riseSeconds = 90, maxDepth = .4f, harmfulDepth = .2f;
    public float damagePerSecond = .5f, speedFactor = .7f, blockHoldSeconds = 4;
    public int towelsToBlock = 2;
}

[Serializable]
public sealed class PuddleSettings { public float radius = 1.5f, speedFactor = .6f, wipeHoldSeconds = 4; }

[Serializable]
public sealed class DoorGapSettings { public int towels = 1; public float holdSeconds = 3, chanceReduction = .1f; }

[Serializable]
public sealed class HealthSettings
{
    public float faintSeconds = 5, faintWakeHealth = 35, firstAidHeal = 30, firstAidHoldSeconds = 1.5f;
    public int firstAidUsesPerKit = 2;
}

[Serializable] public sealed class BreakChances { public float unreinforced, repaired, reinforced; }

[Serializable]
public sealed class ScriptedBreak
{
    public string opening, result = "Cracked";
    /// <summary>Seconds into Disaster Time when the hit lands (the warning starts earlier).</summary>
    public float atSeconds;
}
