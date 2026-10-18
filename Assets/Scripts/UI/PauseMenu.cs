using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Explicit Escape pause state; independent of cursor focus. GameUI draws the menu.</summary>
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(MapExplorer))]
public sealed class PauseMenu : MonoBehaviour
{
    static PauseMenu instance;
    public static bool Active => instance;
    public bool Open { get; private set; }
    public static bool IsPaused => instance && instance.Open;
    void Awake() { instance = this; }
    void OnDisable() { Open = false; Time.timeScale = 1; if (instance == this) instance = null; }
    void Update()
    {
        var s = GameSession.Instance;
        var d = s ? s.GetComponent<DayDirector>() : null;
        bool playing = d && d.Phase >= DayPhase.Preparation && d.Phase <= DayPhase.AfterDisaster;
        if (!playing) { if (Open) SetOpen(false); return; }
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            HandleEscape();
    }
    public void HandleEscape()
    {
        if (SettingsOverlay.IsOpen) { SettingsOverlay.Close(); return; }
        var ui = FindAnyObjectByType<GameUI>();
        if (ui && ui.CloseManualControls()) return;
        SetOpen(!Open);
    }
    public void SetOpen(bool open)
    {
        Open = open; Time.timeScale = open ? 0 : 1;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }
    public static void Resume() { if (instance) instance.SetOpen(false); }
}
