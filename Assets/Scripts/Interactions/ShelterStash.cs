using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The master bedroom shelter supply drop. Hold E with water, food or the blanket in your arms to store them here.
/// Stored items are laid out on top of the stash and can be taken back.
/// </summary>
[DisallowMultipleComponent]
public sealed class ShelterStash : MonoBehaviour, IInteractable
{
    const float Spacing = .16f, LayerHeight = .42f;
    readonly List<Pickup> stored = new List<Pickup>();
    Bounds? surface;

    public float HoldSeconds => 1.5f;
    public string Prompt => "Store supplies in shelter";
    public int Count(ItemKind kind) => stored.Count(p => p && p.Stash == this && p.gameObject.activeInHierarchy && p.Kind == kind);

    public string Unavailable(MapExplorer user)
    {
        var group = user.Inventory.ArmfulGroup;
        return group == CarryGroup.Supplies || group == CarryGroup.Blanket ? null : "Carry water, food or the blanket here";
    }

    public void Use(MapExplorer user)
    {
        foreach (var item in user.Inventory.TakeArmful()) Store(item);
    }

    /// <summary>Puts one item on the next free spot of the stash.</summary>
    public void Store(Pickup item)
    {
        stored.RemoveAll(p => !p || p.Stash != this);
        item.PlaceAt(Slot(stored.Count), transform, false);
        item.Stash = this;
        stored.Add(item);
    }

    /// <summary>Eats/drinks one stored item of this kind (overnight recovery). False if none is stored.</summary>
    public bool Consume(ItemKind kind)
    {
        var item = stored.FirstOrDefault(p => p && p.Stash == this && p.Kind == kind);
        if (!item) return false;
        stored.Remove(item);
        Destroy(item.gameObject);
        return true;
    }

    /// <summary>Forgets what is stored (used before restoring a snapshot, which places items again).</summary>
    public void ClearStored()
    {
        foreach (var item in stored) if (item && item.Stash == this) item.Stash = null;
        stored.Clear();
    }

    /// <summary>Grid positions across the top of the stash, filled row by row.</summary>
    Vector3 Slot(int index)
    {
        // Measure the stash itself once, before stored items (its children) start adding to the bounds.
        if (surface == null)
            surface = GetComponentsInChildren<Renderer>().Where(r => !r.GetComponentInParent<Pickup>())
                .Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
        var bounds = surface.Value;
        int perRow = Mathf.Max(1, Mathf.FloorToInt((bounds.size.x - Spacing * .5f) / Spacing));
        int rows = Mathf.Max(1, Mathf.FloorToInt((bounds.size.z - Spacing * .5f) / Spacing));
        int row = (index / perRow) % rows, column = index % perRow;
        float layer = index / (perRow * rows) * LayerHeight;
        return new Vector3(bounds.min.x + Spacing * (column + .75f), bounds.max.y + layer, bounds.min.z + Spacing * (row + .75f));
    }
}
