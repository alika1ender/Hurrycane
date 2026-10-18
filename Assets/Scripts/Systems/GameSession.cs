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
    /// <summary>Raised at the start of a fresh day, before its autosave (not on restores). For morning setup like Day 1 staging.</summary>
    public static event Action<int> DayStarting;
    /// <summary>Survives the scene reload that a restart does.</summary>
    static HouseSnapshot pendingRestore;

    public float health = 100, pulse = 72, phoneBattery = 40, flashlightCharge = 100;
    // Running totals for the final score (Docs/Design/08); saved in snapshots.
    public int tasksDone, tasksRequired, hits;
    [NonSerialized] public System.Collections.Generic.HashSet<string> reinforcedIds = new System.Collections.Generic.HashSet<string>();
    MapExplorer player;

    public int CurrentDay { get; private set; }

    void OnEnable() => BreakableOpening.StateChanged += OnOpeningChanged;
    void OnDisable() => BreakableOpening.StateChanged -= OnOpeningChanged;
    void OnOpeningChanged(BreakableOpening opening, OpeningState before, OpeningState after)
    {
        if (after == OpeningState.Reinforced) reinforcedIds.Add(opening.Id);
    }

    /// <summary>Throws away all saves and starts again from Day 1 (end screen "Play again").</summary>
    public static void NewGame()
    {
        SaveSystem.DeleteAll();
        pendingRestore = null;
        skipMenuOnce = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

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
        this.firstDay = firstDay;
        // Remember where the scene put Cane (by the front door) before the menu moves the camera.
        startPosition = player.transform.position; startRotation = player.transform.rotation;
        var director = GetComponent<DayDirector>();
        bool menu = !skipMenuOnce && director && director.Config.showMainMenu;
        skipMenuOnce = false;
        if (menu) director.ShowMenu();
        else StartNewGame();
    }

    int firstDay = 1;
    Vector3 startPosition; Quaternion startRotation;
    /// <summary>Set by "Play again" / "New game" on end screens so the reload goes straight into Day 1.</summary>
    static bool skipMenuOnce;

    /// <summary>Main menu NEW GAME: throw away old saves and start Day 1 here (no reload).</summary>
    /// <summary>Raised when a fresh game begins (menu New Game or Play again), e.g. to show first-time control tips again.</summary>
    public static event Action NewGameStarted;

    public void StartNewGame()
    {
        NewGameStarted?.Invoke();
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.SetPositionAndRotation(startPosition, startRotation);
        player.viewCamera.transform.localRotation = Quaternion.identity;
        controller.enabled = true;
        SaveSystem.DeleteAll();
        StartDay(firstDay);
    }

    /// <summary>Return to the title; retain day-start saves so Continue works.</summary>
    public static void ReturnToMenu()
    {
        Time.timeScale = 1;
        pendingRestore = null; skipMenuOnce = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

    /// <summary>Main menu CONTINUE: restart from the latest saved day.</summary>
    public static void ContinueLatest()
    {
        var saves = SaveSystem.All();
        if (saves.Count > 0) RestartFromDay(saves[saves.Count - 1].day);
    }

    /// <summary>Begins a day: brings that day's windows into play, then autosaves. The phase timer will call this each morning.</summary>
    public void StartDay(int day)
    {
        CurrentDay = day;
        DayStarting?.Invoke(day);
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
