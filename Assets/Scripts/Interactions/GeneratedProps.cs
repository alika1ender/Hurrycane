using UnityEngine;

/// <summary>
/// Pickups made at runtime because the scene has no prop for them yet: plank sets (PlankStack) and towels.
/// Snapshots record their kind and recreate them through here. Swap the cubes for real props when art exists.
/// </summary>
public static class GeneratedProps
{
    static Material towelMat;

    public static Pickup Create(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.PlankSet: return PlankStack.CreatePlankSet();
            case ItemKind.Towel: return CreateTowel();
            default: Debug.LogWarning("No generated prop for " + kind); return null;
        }
    }

    public static Material TowelMaterial
    {
        get
        {
            if (!towelMat)
            {
                towelMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                towelMat.SetColor("_BaseColor", new Color(.62f, .78f, .86f));
                towelMat.SetFloat("_Smoothness", .05f);
            }
            return towelMat;
        }
    }

    /// <summary>A folded towel: a soft pale-blue slab.</summary>
    public static Pickup CreateTowel()
    {
        var towel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        towel.name = "PICKUP_Towel";
        towel.transform.localScale = new Vector3(.32f, .06f, .24f);
        towel.GetComponent<Renderer>().sharedMaterial = TowelMaterial;
        var pickup = towel.AddComponent<Pickup>();
        pickup.Configure(ItemKind.Towel);
        pickup.Generated = true;
        return pickup;
    }

    /// <summary>A new game's towels (Docs/Design/04): 4 on the bathroom vanity, 2 on the floor by the basement shelter.</summary>
    public static void StageTowels()
    {
        Transform vanity = null, stash = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (t.name == "Bath_Vanity_Visual") vanity = t;
            if (t.name == "Basement_ShelterStash_Visual") stash = t;
        }
        if (vanity)
        {
            var top = vanity.GetComponent<Renderer>().bounds;
            for (int i = 0; i < 4; i++) CreateTowel().PlaceAt(new Vector3(top.center.x, top.max.y + .05f + i * .07f, top.center.z), null, true);
        }
        if (stash)
        {
            var b = stash.GetComponent<Renderer>().bounds;
            for (int i = 0; i < 2; i++) CreateTowel().PlaceAt(new Vector3(b.max.x + .35f, b.min.y + .3f + i * .07f, b.center.z), null, true);
        }
    }
}
