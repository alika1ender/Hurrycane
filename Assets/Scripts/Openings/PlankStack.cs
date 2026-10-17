using UnityEngine;

/// <summary>
/// The plank stack on the basement workbench (Docs/Design/03): tap E to fill your arms with plank sets (up to 8).
/// One set boards one window or braces the door. Placeholder cube until the plank prop exists.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlankStack : MonoBehaviour, IInteractable
{
    static Material plankMat;
    [SerializeField] int remaining;
    Vector3 fullScale;

    public int Remaining => remaining;
    public string Prompt => "Take planks (" + remaining + " left)";
    public float HoldSeconds => 0;

    public void Configure(int sets)
    {
        remaining = sets;
        fullScale = transform.localScale;
        bottom = transform.position.y - fullScale.y * .5f;
        Refresh();
    }
    float bottom;

    public string Unavailable(MapExplorer user)
    {
        if (remaining <= 0) return "No planks left";
        var group = user.Inventory.ArmfulGroup;
        if (group != null && group != CarryGroup.Planks) return "Hands full [G] drop";
        if (user.Inventory.Armful.Count >= PlayerInventory.Capacity(CarryGroup.Planks)) return "Can't carry more";
        return null;
    }

    public void Use(MapExplorer user)
    {
        int take = Mathf.Min(remaining, PlayerInventory.Capacity(CarryGroup.Planks) - user.Inventory.Armful.Count);
        for (int i = 0; i < take; i++) user.Inventory.Take(MakePlankSet());
        remaining -= take;
        Refresh();
    }

    /// <summary>A single plank set as a normal pickup, so it can be dropped and picked up again like any item.</summary>
    Pickup MakePlankSet()
    {
        var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plank.name = "PICKUP_PlankSet";
        plank.transform.localScale = new Vector3(.9f, .05f, .22f);
        plank.GetComponent<Renderer>().sharedMaterial = Material();
        var pickup = plank.AddComponent<Pickup>();
        pickup.Configure(ItemKind.PlankSet);
        return pickup;
    }

    void Refresh()
    {
        float fill = Mathf.Clamp01(remaining / 18f);
        transform.localScale = new Vector3(fullScale.x, fullScale.y * Mathf.Max(fill, .05f), fullScale.z);
        var p = transform.position; p.y = bottom + transform.localScale.y * .5f; transform.position = p;
        GetComponent<Renderer>().enabled = remaining > 0;
    }

    public static Material Material()
    {
        if (!plankMat)
        {
            plankMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            plankMat.SetColor("_BaseColor", new Color(.55f, .38f, .22f));
            plankMat.SetFloat("_Smoothness", .2f);
        }
        return plankMat;
    }
}
