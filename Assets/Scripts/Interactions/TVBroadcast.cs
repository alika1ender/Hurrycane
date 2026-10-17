using UnityEngine;

public enum TVFrame { Anchor, StormWatch, Prepare, Match }

/// <summary>Owns only the TV screen slot, including the unplugged screen and restored saves.</summary>
public sealed class TVBroadcast : MonoBehaviour
{
    Renderer screen;
    int slot = -1;
    Material original, black;
    readonly Material[] frames = new Material[4];
    Switchable plug;
    TVFrame current = TVFrame.Anchor;
    public TVFrame Current => current;

    void Awake()
    {
        plug = GetComponent<Switchable>();
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            int index = System.Array.FindIndex(renderer.sharedMaterials, m => m && m.name.Contains("TVStorm"));
            if (index < 0) continue;
            screen = renderer; slot = index; original = renderer.sharedMaterials[index]; break;
        }
        if (!screen) { Debug.LogWarning("TVBroadcast: TV screen material not found"); return; }
        foreach (TVFrame frame in System.Enum.GetValues(typeof(TVFrame)))
        {
            var texture = Resources.Load<Texture2D>("TVBroadcast/" + frame);
            if (!texture) { Debug.LogError("Missing TV broadcast image: " + frame); continue; }
            var material = new Material(original) { name = "TV_" + frame };
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_EmissionColor", Color.white * .8f);
            material.EnableKeyword("_EMISSION");
            frames[(int)frame] = material;
        }
        black = new Material(original) { name = "TV_Unplugged" };
        black.SetTexture("_BaseMap", null);
        black.SetColor("_BaseColor", Color.black);
        black.SetTexture("_EmissionMap", null);
        black.SetColor("_EmissionColor", Color.black);
        black.DisableKeyword("_EMISSION");
        black.SetFloat("_Smoothness", 0);
        RefreshPower();
    }

    public void Show(TVFrame frame) { current = frame; RefreshPower(); }

    public void RefreshPower()
    {
        if (!screen) return;
        var slots = screen.sharedMaterials;
        slots[slot] = plug && plug.Off ? black : frames[(int)current] ? frames[(int)current] : original;
        screen.sharedMaterials = slots;
    }

    void OnDestroy()
    {
        if (screen && original) { var slots = screen.sharedMaterials; slots[slot] = original; screen.sharedMaterials = slots; }
        foreach (var frame in frames) if (frame) Destroy(frame);
        if (black) Destroy(black);
    }
}
