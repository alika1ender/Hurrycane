using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// For artwork with its text baked into the picture (the phone alert): the picture is shown in English, and a drawn
/// version made of real labels (which Loc can translate) is shown in Turkish. Also keeps the "tap to open" label of the
/// drawn version pulsing in step with the picture's one.
/// </summary>
public sealed class LocalizedSwap : MonoBehaviour
{
    GameObject[] english;
    GameObject turkish;
    Text pulseSource, pulseTarget;
    int shownVersion = -1;

    public void Setup(GameObject[] englishParts, GameObject turkishPart, Text sourceFooter, Text targetFooter)
    {
        english = englishParts; turkish = turkishPart; pulseSource = sourceFooter; pulseTarget = targetFooter;
        Apply();
    }

    void Apply()
    {
        shownVersion = Loc.Version;
        bool tr = Loc.IsTurkish;
        foreach (var part in english) if (part) part.SetActive(!tr);
        if (turkish) turkish.SetActive(tr);
    }

    void LateUpdate()
    {
        if (shownVersion != Loc.Version) Apply();
        if (pulseSource && pulseTarget) pulseTarget.color = pulseSource.color;
    }
}
