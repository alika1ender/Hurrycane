using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Esc menu (Docs/Design/06): Resume, and one card per saved day to restart from. Placeholder IMGUI until the real UI.
/// Opens whenever the player has released the cursor with Esc.
/// </summary>
[RequireComponent(typeof(MapExplorer))]
public sealed class PauseMenu : MonoBehaviour
{
    /// <summary>While a PauseMenu exists, clicking the game view does not grab the cursor; Resume does.</summary>
    public static bool Active { get; private set; }

    MapExplorer explorer;
    List<HouseSnapshot> cards;

    bool Open => explorer.enabled && Cursor.lockState != CursorLockMode.Locked;

    void Awake() => explorer = GetComponent<MapExplorer>();
    void OnEnable() => Active = true;
    void OnDisable() { Active = false; Time.timeScale = 1; }
    void Update()
    {
        if (!Open) cards = null;
        // Freeze the day clock, storm and doors while paused; only touch timeScale when the menu opens or closes.
        if (Open != wasOpen) { Time.timeScale = Open ? 0 : 1; wasOpen = Open; }
    }
    bool wasOpen;

    void OnGUI()
    {
        if (!Open) return;
        if (cards == null) cards = SaveSystem.All();
        var session = GameSession.Instance;
        float width = 460, height = 130 + cards.Count * 64;
        var area = new Rect((Screen.width - width) * .5f, (Screen.height - height) * .5f, width, height);
        GUI.Box(area, "PAUSED" + (session ? "  ·  Day " + session.CurrentDay : ""));
        GUILayout.BeginArea(new Rect(area.x + 16, area.y + 30, width - 32, height - 40));
        if (GUILayout.Button("Resume", GUILayout.Height(34)))
        {
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }
        GUILayout.Space(8);
        GUILayout.Label(cards.Count == 0 ? "No saved days yet." : "Restart from the start of a day:");
        foreach (var card in cards)
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("Day " + card.day + "   Health " + Mathf.RoundToInt(card.health) + "%   Boarded " + card.reinforced + "/15\n"
                + "Water " + card.water + "   Food " + card.food + "   (" + card.savedAt + ")", GUILayout.Height(40));
            if (GUILayout.Button("Restart", GUILayout.Width(90), GUILayout.Height(40))) GameSession.RestartFromDay(card.day);
            GUILayout.EndHorizontal();
        }
        GUILayout.EndArea();
    }
}
