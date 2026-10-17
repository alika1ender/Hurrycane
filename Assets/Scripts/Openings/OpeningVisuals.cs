using System.Linq;
using UnityEngine;

/// <summary>
/// Placeholder looks for an opening's states: cube boards (reinforced), dark crack lines (cracked/broken),
/// glass faded out (broken), grey tape strips (repaired). Built around the COL_* proxy; swap for art later.
/// </summary>
public sealed class OpeningVisuals
{
    static Material boardMat, crackMat, tapeMat;

    readonly Transform root;
    readonly GameObject boards, cracks, tape;
    readonly Renderer mesh;
    readonly int glassSlot = -1;
    readonly Material glass, glassCracked, glassBroken;
    readonly Vector3 meshHome, center;

    public OpeningVisuals(Collider proxy, bool isDoor)
    {
        var b = proxy.bounds;
        center = b.center;
        bool thinX = b.size.x < b.size.z;
        float width = thinX ? b.size.z : b.size.x, height = b.size.y, depth = thinX ? b.size.x : b.size.z;
        // Face into the house: its footprint centre is about (-6, -5).
        Vector3 inward = thinX ? new Vector3(Mathf.Sign(-6 - b.center.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(-5 - b.center.z));
        root = new GameObject(proxy.name + "_StateFX").transform;
        root.SetParent(proxy.transform, true);
        root.SetPositionAndRotation(b.center, Quaternion.LookRotation(inward));

        boardMat = boardMat ? boardMat : MakeMaterial(new Color(.55f, .38f, .22f));
        crackMat = crackMat ? crackMat : MakeMaterial(new Color(.08f, .08f, .09f));
        tapeMat = tapeMat ? tapeMat : MakeMaterial(new Color(.72f, .72f, .68f));

        boards = new GameObject("Boards"); boards.transform.SetParent(root, false);
        if (isDoor)
        {
            float angle = Mathf.Atan2(height, width) * Mathf.Rad2Deg, length = Mathf.Sqrt(width * width + height * height) * .95f;
            Bar(boards, boardMat, new Vector3(0, 0, .14f), new Vector3(length, .24f, .04f), angle);
            Bar(boards, boardMat, new Vector3(0, 0, .16f), new Vector3(length, .24f, .04f), -angle);
        }
        else
            foreach (float y in new[] { -.33f, 0, .33f })
                Bar(boards, boardMat, new Vector3(0, y * height, .34f), new Vector3(width + .24f, .23f, .03f), 0);

        float diagonal = Mathf.Atan2(height, width) * Mathf.Rad2Deg, span = Mathf.Sqrt(width * width + height * height) * .8f;
        cracks = new GameObject("Cracks"); cracks.transform.SetParent(root, false);
        Bar(cracks, crackMat, new Vector3(0, 0, depth * .5f + .02f), new Vector3(span, .012f, .004f), diagonal * .7f);
        Bar(cracks, crackMat, new Vector3(-.1f * width, .05f, depth * .5f + .02f), new Vector3(span * .6f, .012f, .004f), -diagonal * .9f);
        Bar(cracks, crackMat, new Vector3(.15f * width, -.1f, depth * .5f + .02f), new Vector3(span * .4f, .012f, .004f), 15);

        tape = new GameObject("Tape"); tape.transform.SetParent(root, false);
        Bar(tape, tapeMat, new Vector3(0, 0, depth * .5f + .03f), new Vector3(span, .06f, .004f), diagonal);
        Bar(tape, tapeMat, new Vector3(0, 0, depth * .5f + .03f), new Vector3(span, .06f, .004f), -diagonal);

        // The visible window mesh: the smallest renderer around the proxy, preferring one with a glass slot.
        var candidates = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)
            .Where(r => r.enabled && !r.name.StartsWith("COL_") && !r.transform.IsChildOf(proxy.transform))
            .Where(r => { var e = r.bounds; e.Expand(.1f); return e.Contains(b.center) && r.bounds.size.magnitude < 6; })
            .OrderBy(r => r.sharedMaterials.Any(IsGlass) ? 0 : 1).ThenBy(r => r.bounds.size.sqrMagnitude).ToList();
        mesh = candidates.FirstOrDefault();
        if (!mesh) { Debug.LogWarning("OpeningVisuals: no visible mesh found for " + proxy.name); return; }
        meshHome = mesh.transform.localPosition;
        glassSlot = System.Array.FindIndex(mesh.sharedMaterials, IsGlass);
        if (isDoor || glassSlot < 0) return;
        glass = mesh.sharedMaterials[glassSlot];
        glassCracked = new Material(glass) { name = glass.name + "_Cracked" };
        glassCracked.SetColor("_BaseColor", glass.GetColor("_BaseColor") * new Color(.7f, .7f, .7f, 1));
        glassBroken = new Material(glass) { name = glass.name + "_Broken" };
        MakeInvisible(glassBroken);
    }

    public string MeshName => mesh ? mesh.name : "none";
    public bool HasGlass => glass;

    public void Show(OpeningState state)
    {
        boards.SetActive(state == OpeningState.Reinforced || state == OpeningState.Cracked);
        cracks.SetActive(state == OpeningState.Cracked || (state == OpeningState.Broken && !glass)); // with no glass left, lines would float
        tape.SetActive(state == OpeningState.Repaired);
        if (!glass) return;
        var slots = mesh.sharedMaterials;
        slots[glassSlot] = state == OpeningState.Broken ? glassBroken : state == OpeningState.Cracked ? glassCracked : glass;
        mesh.sharedMaterials = slots;
    }

    /// <summary>Jitters the window mesh and boards by up to this many metres; 0 puts them back.</summary>
    public void Shake(float amount)
    {
        var offset = amount > 0 ? Random.insideUnitSphere * amount : Vector3.zero;
        root.position = center + offset;
        if (mesh) mesh.transform.localPosition = meshHome + mesh.transform.parent.InverseTransformVector(offset);
    }

    static bool IsGlass(Material m) => m && (m.name.Contains("Window") || m.name.Contains("Glass") || m.name.Contains("Frosted"));

    static void Bar(GameObject parent, Material material, Vector3 localPosition, Vector3 size, float rollDegrees)
    {
        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(bar.GetComponent<Collider>());
        bar.transform.SetParent(parent.transform, false);
        bar.transform.localPosition = localPosition;
        bar.transform.localRotation = Quaternion.Euler(0, 0, rollDegrees);
        bar.transform.localScale = size;
        bar.GetComponent<Renderer>().sharedMaterial = material;
    }

    static Material MakeMaterial(Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", .2f);
        return material;
    }

    /// <summary>Turns a copy of the glass material fully transparent so the pane looks gone.</summary>
    static void MakeInvisible(Material m)
    {
        m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        var c = m.GetColor("_BaseColor"); c.a = 0; m.SetColor("_BaseColor", c);
    }
}
