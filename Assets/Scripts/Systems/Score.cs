using System.Linq;
using UnityEngine;

/// <summary>The end-of-game breakdown (Docs/Design/08). Each part is already in points.</summary>
public sealed class ScoreReport
{
    public int health, openings, reinforcements, supplies, tasks, repairs, total;
    public string rank, line, fact;
    // Raw numbers behind the points, for the score screen.
    public int intact, openingCount, reinforcedCount, supplyCount, tasksDone, tasksRequired, damagedNow, hits;
}

public static class Score
{
    /// <summary>"Supplies left" gives full points at this many water + food + first aid uses (08).</summary>
    const int SuppliesForFullPoints = 16;

    // Shown one at a time on the score screen. Fact-check against ready.gov / NOAA before shipping (08).
    static readonly string[] Facts =
    {
        "Taping windows does not stop them breaking. Use boards or storm shutters.",
        "Store at least one gallon (about 4 litres) of water per person per day, for several days.",
        "Never run a generator indoors or in a garage. Carbon monoxide kills.",
        "Stay away from windows during the storm. Shelter in an interior room.",
        "Storm surge and flooding, not wind, cause most hurricane deaths.",
    };

    public static ScoreReport Compute(GameSession session, MapExplorer player)
    {
        var report = new ScoreReport();
        var openings = Object.FindObjectsByType<BreakableOpening>(FindObjectsInactive.Include);
        var stash = Object.FindAnyObjectByType<ShelterStash>();
        var inventory = player.Inventory;

        report.openingCount = openings.Length;
        report.intact = openings.Count(o => o.Intact);
        report.reinforcedCount = Mathf.Min(15, session.reinforcedIds.Count);
        report.supplyCount = inventory.Count(ItemKind.Water) + inventory.Count(ItemKind.Food) + inventory.Uses(ItemKind.FirstAid)
            + (stash ? stash.Count(ItemKind.Water) + stash.Count(ItemKind.Food) : 0);
        report.tasksDone = session.tasksDone;
        report.tasksRequired = session.tasksRequired;
        report.damagedNow = openings.Count(o => o.State == OpeningState.Cracked || o.State == OpeningState.Broken);
        report.hits = session.hits;

        report.health = Mathf.RoundToInt(300 * Mathf.Clamp01(session.health / 100f));
        report.openings = Mathf.RoundToInt(250f * report.intact / Mathf.Max(1, report.openingCount));
        report.reinforcements = Mathf.RoundToInt(150f * report.reinforcedCount / 15f);
        report.supplies = Mathf.RoundToInt(100 * Mathf.Clamp01(report.supplyCount / (float)SuppliesForFullPoints));
        report.tasks = Mathf.RoundToInt(100 * Mathf.Clamp01(report.tasksDone / (float)Mathf.Max(1, report.tasksRequired)));
        report.repairs = report.hits == 0 ? 100 : Mathf.RoundToInt(100 * Mathf.Clamp01(1 - report.damagedNow / (float)report.hits));
        report.total = report.health + report.openings + report.reinforcements + report.supplies + report.tasks + report.repairs;

        if (report.total >= 900) { report.rank = "PREPARED"; report.line = "The house held. So did you."; }
        else if (report.total >= 700) { report.rank = "SURVIVOR"; report.line = "Battered, but you made it through."; }
        else if (report.total >= 450) { report.rank = "SHAKEN"; report.line = "You survived. Next time, prepare earlier."; }
        else { report.rank = "UNPREPARED"; report.line = "The storm found every gap."; }
        report.fact = Facts[Random.Range(0, Facts.Length)];
        return report;
    }
}
