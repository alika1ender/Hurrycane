using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sits on every UI Text made by UiKit.Label. The game keeps writing its English text into the label; this swaps in the
/// chosen language just before the frame is drawn, and again if the language changes.
/// </summary>
[RequireComponent(typeof(Text))]
public sealed class LocalizedLabel : MonoBehaviour
{
    Text text;
    string source = "", shown;
    int version = -1;

    void Awake() => text = GetComponent<Text>();

    void LateUpdate()
    {
        if (!text) return;
        string current = text.text;
        if (current != shown)
        {
            // The game wrote new text (or this is the first frame).
            source = current;
            Loc.Observe(source);
            shown = Loc.T(source);
            version = Loc.Version;
            if (shown != current) text.text = shown;
        }
        else if (version != Loc.Version)
        {
            version = Loc.Version;
            shown = Loc.T(source);
            if (text.text != shown) text.text = shown;
        }
    }
}
