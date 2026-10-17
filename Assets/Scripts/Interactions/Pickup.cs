using UnityEngine;

/// <summary>A supply item the player can take with E (one of the PICKUP_* objects).</summary>
[DisallowMultipleComponent]
public sealed class Pickup : MonoBehaviour, IInteractable
{
    [SerializeField] ItemKind kind;

    public ItemKind Kind => kind;
    /// <summary>Set while the item sits in the basement shelter stash.</summary>
    public ShelterStash Stash { get; set; }

    public string Prompt => "Take " + DisplayName(kind).ToLowerInvariant();
    public float HoldSeconds => 0;
    public string Unavailable(MapExplorer user) => user.Inventory.CannotTake(this);
    public void Use(MapExplorer user)
    {
        Stash = null;
        user.Inventory.Take(this);
    }

    public void Configure(ItemKind itemKind) => kind = itemKind;

    /// <summary>
    /// Reactivates the item at a world point. With dropToFloor the item falls straight down onto the first surface
    /// below; otherwise its lowest point rests at the given height.
    /// </summary>
    public void PlaceAt(Vector3 point, Transform parent, bool dropToFloor)
    {
        transform.SetParent(parent, true);
        transform.position = point;
        gameObject.SetActive(true);
        Physics.SyncTransforms();
        var bounds = Bounds();
        float floor = point.y;
        if (dropToFloor && Physics.Raycast(new Vector3(point.x, bounds.min.y + .01f, point.z), Vector3.down, out var hit, 6, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            floor = hit.point.y;
        transform.position += Vector3.up * (floor - bounds.min.y);
        Physics.SyncTransforms();
    }

    Bounds Bounds()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(transform.position, Vector3.zero);
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>Item kind from a scene name such as PICKUP_Basement_Water_03. Null if the name is not recognised.</summary>
    public static ItemKind? KindFromName(string name)
    {
        if (name.Contains("Water")) return ItemKind.Water;
        if (name.Contains("Food")) return ItemKind.Food;
        if (name.Contains("FirstAid")) return ItemKind.FirstAid;
        if (name.Contains("Flashlight")) return ItemKind.Flashlight;
        if (name.Contains("Batteries")) return ItemKind.Batteries;
        if (name.Contains("Radio")) return ItemKind.Radio;
        if (name.Contains("Backpack")) return ItemKind.Backpack;
        if (name.Contains("Blanket")) return ItemKind.Blanket;
        if (name.Contains("Toolkit")) return ItemKind.Toolkit;
        if (name.Contains("Tape")) return ItemKind.RepairTape;
        if (name.Contains("Towel")) return ItemKind.Towel;
        if (name.Contains("Plank")) return ItemKind.PlankSet;
        return null;
    }

    public static string DisplayName(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.FirstAid: return "First aid kit";
            case ItemKind.RepairTape: return "Repair tape";
            case ItemKind.PlankSet: return "Planks";
            default: return kind.ToString();
        }
    }
}
