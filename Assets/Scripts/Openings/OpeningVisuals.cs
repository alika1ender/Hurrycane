using System.Linq;
using UnityEngine;

/// <summary>
/// Placeholder looks for an opening's states: cube boards (reinforced), dark crack lines (cracked/broken),
/// glass faded out (broken), grey tape strips (repaired). Built around the COL_* proxy; swap for art later.
/// </summary>
public sealed class OpeningVisuals
{
    static Material boardMat, crackMat, tapeMat, shardMat, crackLight, holeMat, splinterMat;

    readonly Transform root, proxyTransform;
    readonly GameObject boards, cracks, tape, remnants, smashed;
    GameObject floorShards;
    readonly bool door;
    readonly float width, height, depth;
    readonly Vector3 inward;
    readonly Renderer mesh;
    readonly int glassSlot = -1;
    readonly Material glass, glassCracked, glassBroken;
    Material glassClear;
    readonly Vector3 meshHome, center;

    public OpeningVisuals(Collider proxy, bool isDoor)
    {
        var b = proxy.bounds;
        center = b.center;
        door = isDoor; proxyTransform = proxy.transform;
        bool thinX = b.size.x < b.size.z;
        width = thinX ? b.size.z : b.size.x; height = b.size.y; depth = thinX ? b.size.x : b.size.z;
        // Face into the house: its footprint centre is about (-6, -5).
        inward = thinX ? new Vector3(Mathf.Sign(-6 - b.center.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(-5 - b.center.z));
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
        // Damage must sit in front of (inside) WindowWeather's opaque storm-sky pane, which is 3.5 cm in from the glass
        // centre; otherwise it is hidden and a broken window looks intact.
        float front = isDoor ? depth * .5f + .03f : Mathf.Max(depth * .5f, .035f) + .025f;
        crackLight = crackLight ? crackLight : MakeEmissive(new Color(.9f, .94f, .97f), .45f);
        holeMat = holeMat ? holeMat : MakeMaterial(new Color(.03f, .03f, .035f));
        splinterMat = splinterMat ? splinterMat : MakeMaterial(new Color(.8f, .64f, .43f));
        var random = new System.Random(proxy.name.GetHashCode());
        float R(float min, float max) => min + (float)random.NextDouble() * (max - min);

        // Cracked (behind the boards): a spider-web of cracks across the whole pane / door.
        cracks = new GameObject("Cracks"); cracks.transform.SetParent(root, false);
        var crackColour = isDoor ? crackMat : crackLight; // light lines read on dark glass, dark lines on the wooden door
        Web(cracks, crackColour, new Vector2(width * R(-.2f, .2f), height * R(-.15f, .2f)), 11, front, random);
        Web(cracks, crackColour, new Vector2(width * R(-.35f, -.2f), height * R(-.35f, -.2f)), 6, front, random);

        tape = new GameObject("Tape"); tape.transform.SetParent(root, false);
        Bar(tape, tapeMat, new Vector3(0, 0, front + .01f), new Vector3(span, .06f, .004f), diagonal);
        Bar(tape, tapeMat, new Vector3(0, 0, front + .01f), new Vector3(span, .06f, .004f), -diagonal);

        // Broken window: a big hole, jagged glass left all around the frame, and the leftover glass shattered.
        shardMat = shardMat ? shardMat : MakeShardMaterial();
        remnants = new GameObject("Remnants"); remnants.transform.SetParent(root, false);
        foreach (int side in new[] { -1, 1 })
        {
            for (int i = 0; i < 4; i++) // top and bottom edges: long teeth pointing into the hole
            {
                float h = height * R(.18f, .42f), w = width * R(.12f, .2f);
                Bar(remnants, shardMat, new Vector3(width * (-.39f + .26f * i + R(-.04f, .04f)), side * (height * .5f - h * .4f), front), new Vector3(w, h, .006f), R(-32, 32));
            }
            for (int i = 0; i < 3; i++) // left and right edges
            {
                float h = height * R(.12f, .2f), w = width * R(.16f, .32f);
                Bar(remnants, shardMat, new Vector3(side * (width * .5f - w * .4f), height * (-.3f + .3f * i + R(-.04f, .04f)), front), new Vector3(w, h, .006f), R(-35, 35));
            }
            foreach (int corner in new[] { -1, 1 }) // solid corner pieces
                Bar(remnants, shardMat, new Vector3(side * width * .42f, corner * height * .42f, front), new Vector3(width * .2f, height * .2f, .006f), R(-20, 20));
        }
        // Cracks running through what's left, from the impact point outwards.
        Web(remnants, crackLight, new Vector2(width * R(-.12f, .12f), height * R(-.1f, .1f)), 14, front + .004f, random);
        // A couple of shards still hanging loose in the middle.
        for (int i = 0; i < 3; i++)
            Bar(remnants, shardMat, new Vector3(width * R(-.25f, .25f), height * R(.05f, .3f), front + .01f), new Vector3(width * R(.05f, .1f), height * R(.12f, .25f), .006f), R(-50, 50));

        // Broken door: smashed through, a jagged hole by the lock, splintered wood and a long split.
        smashed = new GameObject("Smashed"); smashed.transform.SetParent(root, false);
        if (isDoor)
        {
            var hole = new Vector2(width * .18f, -height * .05f);
            for (int i = 0; i < 9; i++) // an irregular dark hole made of overlapping rotated pieces
                Bar(smashed, holeMat, new Vector3(hole.x + R(-.16f, .16f), hole.y + R(-.22f, .22f), front), new Vector3(R(.16f, .3f), R(.18f, .34f), .01f), R(0, 90));
            for (int i = 0; i < 12; i++) // raw splinters sticking out around the hole
            {
                float a = R(0, Mathf.PI * 2);
                var p = new Vector3(hole.x + Mathf.Cos(a) * R(.22f, .32f), hole.y + Mathf.Sin(a) * R(.26f, .38f), front + R(.01f, .05f));
                var splinter = Bar(smashed, splinterMat, p, new Vector3(R(.14f, .32f), R(.025f, .05f), .02f), a * Mathf.Rad2Deg + R(-25, 25));
                splinter.transform.localRotation *= Quaternion.Euler(0, R(-35, 35), 0); // pointing into the room a bit
            }
            // A long split from the top of the door down to the hole, in jagged steps.
            var from = new Vector2(hole.x + R(-.1f, .1f), height * .48f);
            for (int i = 0; i < 4; i++)
            {
                var to = Vector2.Lerp(from, hole, 1f / (4 - i)) + new Vector2(R(-.06f, .06f), 0);
                Segment(smashed, holeMat, from, to, .035f, front + .002f);
                from = to;
            }
            Web(smashed, crackMat, hole, 10, front + .003f, random);
            Web(smashed, crackMat, new Vector2(-width * .25f, height * .25f), 6, front + .003f, random);
        }

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
        bool broken = state == OpeningState.Broken;
        boards.SetActive(state == OpeningState.Reinforced || state == OpeningState.Cracked);
        cracks.SetActive(state == OpeningState.Cracked);
        tape.SetActive(state == OpeningState.Repaired);
        remnants.SetActive(broken && !door);
        smashed.SetActive(broken && door);
        if (broken && !door && !floorShards) floorShards = ScatterFloorShards();
        if (floorShards) floorShards.SetActive(broken && !door);
        if (!glass) return;
        var slots = mesh.sharedMaterials;
        slots[glassSlot] = state == OpeningState.Broken ? glassBroken : state == OpeningState.Cracked ? glassCracked : glass;
        mesh.sharedMaterials = slots;
    }

    /// <summary>Swaps intact glass for a faint, see-through pane (or back). Broken glass stays gone.</summary>
    public void SetClearGlass(bool clear, OpeningState state)
    {
        if (!glass || state == OpeningState.Broken) return;
        if (clear && !glassClear)
        {
            glassClear = new Material(glassBroken) { name = glass.name + "_Clear" };
            var c = glass.GetColor("_BaseColor"); c.a = .12f; glassClear.SetColor("_BaseColor", c);
        }
        var slots = mesh.sharedMaterials;
        slots[glassSlot] = clear ? glassClear : state == OpeningState.Cracked ? glassCracked : glass;
        mesh.sharedMaterials = slots;
    }

    /// <summary>Jitters the window mesh and boards by up to this many metres; 0 puts them back.</summary>
    public void Shake(float amount)
    {
        var offset = amount > 0 ? Random.insideUnitSphere * amount : Vector3.zero;
        root.position = center + offset;
        if (mesh) mesh.transform.localPosition = meshHome + mesh.transform.parent.InverseTransformVector(offset);
    }

    /// <summary>Broken glass on the floor just inside the window. Not parented to the shaking root; no colliders.</summary>
    GameObject ScatterFloorShards()
    {
        var group = new GameObject(proxyTransform.name + "_FloorShards");
        group.transform.SetParent(proxyTransform, true);
        // The room's floor, not whatever is hit first: furniture and invisible collision boxes under bedroom windows left
        // the shards floating in mid-air. The floor is the lowest flat surface within 2.5 m below the window (the storey
        // below is further down than that).
        float? lowest = null;
        foreach (var h in Physics.RaycastAll(center + inward * .45f, Vector3.down, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (h.normal.y > .95f && (lowest == null || h.point.y < lowest)) lowest = h.point.y;
        if (lowest is not float level) return group;
        var across = Vector3.Cross(Vector3.up, inward);
        var random = new System.Random(proxyTransform.name.GetHashCode() * 7);
        float R(float min, float max) => min + (float)random.NextDouble() * (max - min);
        for (int i = 0; i < 18; i++)
        {
            var spot = new Vector3(center.x, level, center.z) + across * (width * R(-.45f, .45f)) + inward * R(.12f, 1f);
            // Only on bare floor at that level (not down the stairs, under furniture or on top of it).
            if (FloorHeight(spot, level) is not float y || Mathf.Abs(y - level) > .05f) continue;
            var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(shard.GetComponent<Collider>());
            shard.name = "Shard";
            shard.transform.SetParent(group.transform, false);
            shard.transform.position = new Vector3(spot.x, y + .006f, spot.z);
            shard.transform.rotation = Quaternion.Euler(R(-6, 6), R(0, 360), R(-6, 6));
            float size = R(.04f, .13f);
            shard.transform.localScale = new Vector3(size, .005f, size * R(.35f, .8f));
            shard.GetComponent<Renderer>().sharedMaterial = shardMat;
        }
        return group;
    }

    /// <summary>
    /// Height of the floor below a point near the expected floor level. The first thing hit from above must be that
    /// floor; if furniture or a collision box covers the spot, it doesn't count (null).
    /// </summary>
    static float? FloorHeight(Vector3 point, float floorY)
    {
        var from = new Vector3(point.x, floorY + 1.4f, point.z);
        if (!Physics.Raycast(from, Vector3.down, out var hit, 1.7f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return null;
        if (Mathf.Abs(hit.point.y - floorY) > .1f || hit.normal.y < .95f) return null;
        return hit.point.y;
    }

    static Material MakeShardMaterial()
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "BrokenGlass" };
        m.SetColor("_BaseColor", new Color(.78f, .86f, .88f, .55f));
        MakeInvisible(m);
        var c = new Color(.78f, .86f, .88f, .55f); m.SetColor("_BaseColor", c); // MakeInvisible zeroes alpha
        m.SetFloat("_Smoothness", .95f);
        m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.78f, .86f, .88f) * .18f); // reads against the dark storm sky
        return m;
    }

    static bool IsGlass(Material m) => m && (m.name.Contains("Window") || m.name.Contains("Glass") || m.name.Contains("Frosted"));

    static GameObject Bar(GameObject parent, Material material, Vector3 localPosition, Vector3 size, float rollDegrees)
    {
        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(bar.GetComponent<Collider>());
        bar.transform.SetParent(parent.transform, false);
        bar.transform.localPosition = localPosition;
        bar.transform.localRotation = Quaternion.Euler(0, 0, rollDegrees);
        bar.transform.localScale = size;
        bar.GetComponent<Renderer>().sharedMaterial = material;
        return bar;
    }

    /// <summary>A straight crack line between two points on the pane (local x/y), at depth z.</summary>
    static void Segment(GameObject parent, Material material, Vector2 from, Vector2 to, float thickness, float z)
    {
        var d = to - from;
        if (d.sqrMagnitude < 1e-6f) return;
        var mid = (from + to) * .5f;
        Bar(parent, material, new Vector3(mid.x, mid.y, z), new Vector3(d.magnitude, thickness, .004f), Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    /// <summary>
    /// A spider-web crack: jagged rays from an impact point out to the frame, joined by two broken rings.
    /// Everything stays inside the opening's rectangle.
    /// </summary>
    void Web(GameObject parent, Material material, Vector2 origin, int rays, float z, System.Random random)
    {
        float R(float min, float max) => min + (float)random.NextDouble() * (max - min);
        float halfW = width * .49f, halfH = height * .49f;
        var ends = new Vector2[rays];
        for (int i = 0; i < rays; i++)
        {
            float angle = (i + R(-.3f, .3f)) / rays * Mathf.PI * 2;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            // Distance from the origin to the frame along this direction.
            float reach = Mathf.Min(dir.x != 0 ? ((dir.x > 0 ? halfW : -halfW) - origin.x) / dir.x : float.MaxValue,
                                    dir.y != 0 ? ((dir.y > 0 ? halfH : -halfH) - origin.y) / dir.y : float.MaxValue);
            float length = reach * R(.65f, 1f);
            var p = origin;
            for (int step = 1; step <= 3; step++) // three kinked pieces per ray
            {
                var next = origin + dir * (length * step / 3f) + new Vector2(-dir.y, dir.x) * R(-.03f, .03f);
                next.x = Mathf.Clamp(next.x, -halfW, halfW); next.y = Mathf.Clamp(next.y, -halfH, halfH);
                Segment(parent, material, p, next, step == 1 ? .014f : .009f, z);
                p = next;
            }
            ends[i] = origin + dir * length;
        }
        foreach (float ring in new[] { .3f, .6f })
            for (int i = 0; i < rays; i++)
            {
                if (R(0, 1) < .3f) continue; // broken rings look more natural
                var a = Vector2.Lerp(origin, ends[i], ring * R(.85f, 1.15f));
                var b = Vector2.Lerp(origin, ends[(i + 1) % rays], ring * R(.85f, 1.15f));
                Segment(parent, material, a, b, .008f, z);
            }
    }

    static Material MakeEmissive(Color color, float glow)
    {
        var material = MakeMaterial(color);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * glow); // still readable in the blackout
        return material;
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
