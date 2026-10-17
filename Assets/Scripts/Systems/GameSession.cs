using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The running game: current day, autosave at day start, restart from a saved day (Docs/Design/01, 06).
/// Health, pulse and phone battery live here as plain numbers until their systems exist.
/// </summary>
public sealed class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }
    /// <summary>Raised when a day starts or is restored, after the autosave.</summary>
    public static event Action<int> DayStarted;
    /// <summary>Survives the scene reload that a restart does.</summary>
    static HouseSnapshot pendingRestore;

    public float health = 100, pulse = 72, phoneBattery = 40;
    MapExplorer player;

    public int CurrentDay { get; private set; }

    /// <summary>Called by GameplaySetup once the scene is wired: restores a pending restart, or starts a new game.</summary>
    public void Begin(MapExplorer explorer, int firstDay)
    {
        Instance = this; player = explorer;
        if (pendingRestore != null)
        {
            var snapshot = pendingRestore; pendingRestore = null;
            HouseState.Apply(snapshot, this, player);
            CurrentDay = snapshot.day;
            DayStarted?.Invoke(CurrentDay);
            return;
        }
        // No main menu yet: entering the scene is a new game.
        SaveSystem.DeleteAll();
        StartDay(firstDay);
    }

    /// <summary>Begins a day: brings that day's windows into play, then autosaves. The phase timer will call this each morning.</summary>
    public void StartDay(int day)
    {
        CurrentDay = day;
        foreach (var opening in FindObjectsByType<BreakableOpening>(FindObjectsInactive.Include))
            if (opening.FirstDay <= day) opening.SetInPlay(true);
        SaveSystem.Save(HouseState.Capture(this, player));
        DayStarted?.Invoke(day);
    }

    /// <summary>Reloads the house and puts back the snapshot taken at the start of that day. Later days are deleted.</summary>
    public static void RestartFromDay(int day)
    {
        var snapshot = SaveSystem.Load(day);
        if (snapshot == null) { Debug.LogWarning("No save for day " + day); return; }
        SaveSystem.DeleteAfter(day);
        pendingRestore = snapshot;
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

    [ContextMenu("Debug: Start next day")] void DebugNextDay() => StartDay(Mathf.Min(3, CurrentDay + 1));
    void OnDestroy() { if (Instance == this) Instance = null; }
}
