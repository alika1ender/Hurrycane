using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Settings card, opened from the main menu and the pause menu: pick English or Turkish. The choice is saved and every
/// label changes at once (Loc / LocalizedLabel). Built by GameUI, drawn above the menus.
/// </summary>
public sealed class SettingsOverlay : MonoBehaviour
{
    static SettingsOverlay instance;
    public static bool IsOpen => instance && instance.gameObject.activeSelf;
    Text englishLabel, turkishLabel;
    bool fromPause;
    DayDirector director;

    public static GameObject Build(Transform root)
    {
        var shade = UiKit.Panel(root, "Settings", new Color(0, 0, 0, .86f), false);
        shade.rectTransform.Stretch();
        shade.raycastTarget = true;
        var overlay = shade.gameObject.AddComponent<SettingsOverlay>();
        instance = overlay;
        var panel = UiKit.Panel(shade.transform, "Panel", UiKit.PanelColor);
        panel.rectTransform.Place(new Vector2(.5f, .5f), Vector2.zero, new Vector2(660, 0));
        panel.Layout<VerticalLayoutGroup>(32, 14, TextAnchor.UpperCenter);
        panel.Fit(false, true);
        UiKit.Label(panel.transform, "Title", UiKit.Bold, 64, UiKit.TextColor, TextAnchor.MiddleCenter).text = "SETTINGS";
        UiKit.Label(panel.transform, "LanguageHeading", UiKit.SemiBold, 24, UiKit.Dim, TextAnchor.MiddleCenter).text = "LANGUAGE";
        UiKit.Button(panel.transform, "English", "ENGLISH", () => Pick(Lang.English), out overlay.englishLabel, 32).Size(-1, 64);
        UiKit.Button(panel.transform, "Turkish", "TÜRKÇE", () => Pick(Lang.Turkish), out overlay.turkishLabel, 32).Size(-1, 64);
        UiKit.Button(panel.transform, "Back", "BACK", Close, out _, 30).Size(-1, 56);
        overlay.Refresh();
        shade.gameObject.SetActive(false);
        return shade.gameObject;
    }

    public static void Open(bool fromPauseMenu)
    {
        if (!instance) return;
        instance.fromPause = fromPauseMenu;
        instance.Refresh();
        instance.transform.SetAsLastSibling();
        instance.gameObject.SetActive(true);
    }

    public static void Close()
    {
        if (instance) instance.gameObject.SetActive(false);
    }

    static void Pick(Lang language)
    {
        Loc.Set(language);
        if (instance) instance.Refresh();
    }

    /// <summary>The chosen language is shown in amber.</summary>
    void Refresh()
    {
        englishLabel.text = Loc.Current == Lang.English ? "<color=#F2A93B>ENGLISH</color>" : "ENGLISH";
        turkishLabel.text = Loc.Current == Lang.Turkish ? "<color=#F2A93B>TÜRKÇE</color>" : "TÜRKÇE";
    }

    void Update()
    {
        // Closes itself when the menu it was opened from goes away (Esc on the pause menu, New Game on the main menu).
        if (!director) director = FindAnyObjectByType<DayDirector>();
        if (fromPause ? !PauseMenu.IsPaused : (director && director.Phase != DayPhase.Menu)) Close();
    }
}
