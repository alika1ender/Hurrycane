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
            case ItemKind.Handbook: return CreateHandbook();
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

    /// <summary>The emergency handbook (Docs/Design/07): a red book with a white band, glowing faintly so it reads as important.</summary>
    public static Pickup CreateHandbook()
    {
        var book = GameObject.CreatePrimitive(PrimitiveType.Cube);
        book.name = "PICKUP_Handbook";
        book.transform.localScale = new Vector3(.24f, .035f, .32f);
        book.GetComponent<Renderer>().sharedMaterial = BookMaterial(new Color(.75f, .14f, .12f), .25f);
        var band = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(band.GetComponent<Collider>());
        band.transform.SetParent(book.transform, false);
        band.transform.localScale = new Vector3(1.01f, 1.05f, .18f);
        band.GetComponent<Renderer>().sharedMaterial = BookMaterial(new Color(.92f, .9f, .85f), 0);
        var pickup = book.AddComponent<Pickup>();
        pickup.Configure(ItemKind.Handbook);
        pickup.Generated = true;
        return pickup;
    }

    static Material BookMaterial(Color color, float glow)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", .25f);
        if (glow > 0) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * glow); }
        return material;
    }

    /// <summary>A new game's handbook, on the living room coffee table (Docs/Design/07).</summary>
    public static void StageHandbook()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (t.name != "Living_CoffeeTable_Visual") continue;
            var top = t.GetComponent<Renderer>().bounds;
            CreateHandbook().PlaceAt(new Vector3(top.center.x - top.extents.x * .45f, top.max.y + .05f, top.center.z), null, true);
            return;
        }
        Debug.LogWarning("StageHandbook: Living_CoffeeTable_Visual not found");
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
