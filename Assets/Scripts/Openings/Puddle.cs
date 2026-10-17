using System.Linq;
using UnityEngine;

/// <summary>
/// A rain puddle under a broken window or behind a broken front door (Docs/Design/03). Slows walking to 60%;
/// hold E with a towel in your arms to wipe it up (the towel is not used up). Spawned by PuddleSystem.
/// </summary>
[DisallowMultipleComponent]
public sealed class Puddle : MonoBehaviour, IInteractable
{
    PuddleSystem system;
    MapExplorer player;

    /// <summary>What made it, e.g. "W04" or "Door#1". Used to avoid duplicates and in snapshots.</summary>
    public string Source { get; private set; }
    public float Radius { get; private set; }

    public void Configure(PuddleSystem owner, MapExplorer explorer, string source, float radius)
    {
        system = owner; player = explorer; Source = source; Radius = radius;
    }

    bool HasTowel => player && player.Inventory.Armful.Any(p => p.Kind == ItemKind.Towel);

    public string Prompt => HasTowel ? "Wipe up the water" : "Wet floor";
    public float HoldSeconds => system.WipeSeconds;
    public string Unavailable(MapExplorer user) => HasTowel ? null : "Need: a towel in your arms (bathroom vanity, basement)";
    public void Use(MapExplorer user) => system.Remove(this);
}
