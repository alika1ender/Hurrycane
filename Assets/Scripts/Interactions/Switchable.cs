using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Something to switch off once with a short hold (Docs/Design/04): unplugging the TV, fridge and kettle, and the basement
/// main breaker and utility valve. Stays off for the rest of the game; tasks check Switchable.AllOff(group).
/// </summary>
[DisallowMultipleComponent]
public sealed class Switchable : MonoBehaviour, IInteractable
{
    public const string Appliances = "appliances", Utilities = "utilities";
    static readonly List<Switchable> all = new List<Switchable>();

    [SerializeField] string group, verb, doneText;
    [SerializeField] float holdSeconds = 1.5f;

    public bool Off { get; private set; }
    public string Group => group;
    /// <summary>Snapshot key: the object name.</summary>
    public string Key => name;

    public void Configure(string switchGroup, string actionVerb, string offText, float seconds)
    {
        group = switchGroup; verb = actionVerb; doneText = offText; holdSeconds = seconds;
    }

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    public static bool AllOff(string group)
    {
        bool any = false;
        foreach (var s in all) { if (s.group != group) continue; any = true; if (!s.Off) return false; }
        return any;
    }
    public static IEnumerable<Switchable> All => all;

    public void SetOff(bool off)
    {
        Off = off;
        var tv = GetComponent<TVBroadcast>();
        if (tv) tv.RefreshPower();
    }

    public string Prompt => Off ? doneText : verb;
    public float HoldSeconds => Off ? 0 : holdSeconds;
    public string Unavailable(MapExplorer user) => Off ? "Already done" : null;
    public void Use(MapExplorer user) { if (!Off) SetOff(true); }
}
