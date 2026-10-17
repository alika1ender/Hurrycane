using UnityEngine;

/// <summary>
/// Pause state (Docs/Design/06): the game is paused whenever the player has released the cursor with Esc.
/// Freezes time while open. GameUI draws the menu itself (Resume + a Restart card per saved day).
/// </summary>
[RequireComponent(typeof(MapExplorer))]
public sealed class PauseMenu : MonoBehaviour
{
    /// <summary>While a PauseMenu exists, clicking the game view does not grab the cursor; Resume does.</summary>
    public static bool Active { get; private set; }

    MapExplorer explorer;
    bool wasOpen;

    public bool Open => explorer.enabled && Cursor.lockState != CursorLockMode.Locked;

    void Awake() => explorer = GetComponent<MapExplorer>();
    void OnEnable() => Active = true;
    void OnDisable() { Active = false; Time.timeScale = 1; }
    void Update()
    {
        // Freeze the day clock, storm and doors while paused; only touch timeScale when the menu opens or closes.
        if (Open != wasOpen) { Time.timeScale = Open ? 0 : 1; wasOpen = Open; }
    }

    public static void Resume() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
}
