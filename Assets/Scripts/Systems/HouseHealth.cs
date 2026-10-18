using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// The house's health (playtest request, 2026-10-18): 100% minus what the storm has done right now. Broken and cracked
/// windows and the door, basement flood water and unwiped puddles count against it; repairing, wiping and blocking the
/// flood win it back, and boarded-up windows protect it. On Day 3 (faintIsGameOver) a house at 0 is lost: game over.
/// Cane's own health (injuries, first aid, fainting) stays separate. Tuning: "house" block in days.json.
/// </summary>
[RequireComponent(typeof(DayDirector))]
public sealed class HouseHealth : MonoBehaviour
{
    /// <summary>Raised when the house loses health (the amount lost).</summary>
    public static event Action<float> Damaged;

    DayDirector director;
    HouseSettings settings;
    FloodSystem flood;
    PuddleSystem puddles;
    float last = 100;
    /// <summary>Off while the house bar is hidden: a loss you can't see coming isn't fair.</summary>
    public bool gameOverAtZero;

    public float Value { get; private set; } = 100;

    void Awake()
    {
        director = GetComponent<DayDirector>();
        settings = director.Config.house;
        flood = GetComponent<FloodSystem>();
        puddles = GetComponent<PuddleSystem>();
    }
    void OnEnable() => GameSession.DayStarted += OnDayStarted;
    void OnDisable() => GameSession.DayStarted -= OnDayStarted;
    void OnDayStarted(int day) { Value = Measure(); last = Value; } // no "damage" flash for what was already broken

    void Update()
    {
        Value = Measure();
        if (Value < last - .5f) Damaged?.Invoke(last - Value);
        last = Value;
        // The house bar is hidden now (playtest 2026-10-18), so an empty house no longer ends the game: it only costs score.
        if (gameOverAtZero && Value <= 0 && director.Today.faintIsGameOver
            && director.Phase >= DayPhase.Preparation && director.Phase <= DayPhase.AfterDisaster)
            director.GameOver("The storm tore your house apart.");
    }

    float Measure()
    {
        float loss = 0;
        foreach (var o in FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude))
        {
            if (!o.InPlay) continue;
            if (o.State == OpeningState.Broken) loss += o.IsDoor ? settings.brokenDoor : settings.brokenWindow;
            else if (o.State == OpeningState.Cracked) loss += o.IsDoor ? settings.crackedDoor : settings.crackedWindow;
        }
        if (!flood) flood = GetComponent<FloodSystem>();
        if (!puddles) puddles = GetComponent<PuddleSystem>();
        if (flood && flood.Depth > 0) loss += settings.floodMax * Mathf.Clamp01(flood.Depth / Mathf.Max(.01f, director.Config.flood.maxDepth));
        if (puddles) loss += Mathf.Min(settings.puddleMax, settings.puddle * puddles.Puddles.Count(p => p));
        return Mathf.Clamp(100 - loss, 0, 100);
    }
}
