using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable] public sealed class PuddleRecord { public string source; public Vector3 position; }

/// <summary>
/// Rain puddles (Docs/Design/03): a broken window leaves a puddle on the floor inside it; a broken front door floods the
/// entry hall and living room (unless the door gap was blocked with towels). Puddles slow walking and stay into the next
/// day until wiped up with a towel; an unrepaired breach makes a new one each morning.
/// </summary>
[RequireComponent(typeof(DayDirector))]
public sealed class PuddleSystem : MonoBehaviour
{
    /// <summary>Puddles float this far above the floor so rugs (up to about 4 cm) don't hide them.</summary>
    const float SurfaceLift = .045f;
    readonly List<Puddle> puddles = new List<Puddle>();
    DayDirector director;
    MapExplorer player;
    PuddleSettings settings;
    DoorGap doorGap;
    Material wet;

    public IReadOnlyList<Puddle> Puddles => puddles;
    public bool Any => puddles.Count > 0;
    public float WipeSeconds => settings.wipeHoldSeconds;

    void Awake() { director = GetComponent<DayDirector>(); settings = director.Config.puddles; }
    void OnEnable() { BreakableOpening.HitLanded += OnHit; GameSession.DayStarted += OnDayStarted; }
    void OnDisable() { BreakableOpening.HitLanded -= OnHit; GameSession.DayStarted -= OnDayStarted; }
    public void Bind(MapExplorer explorer, DoorGap gap) { player = explorer; doorGap = gap; }

    void OnHit(BreakableOpening opening, OpeningState result) { if (result == OpeningState.Broken) SpawnFor(opening); }

    // Unrepaired breaches keep leaking: each morning they get a puddle if theirs was wiped.
    void OnDayStarted(int day)
    {
        foreach (var opening in FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude))
            if (opening.State == OpeningState.Broken) SpawnFor(opening);
    }

    public void SpawnFor(BreakableOpening opening)
    {
        var c = opening.Center;
        var b = opening.GetComponent<Collider>().bounds;
        bool thinX = b.size.x < b.size.z;
        var inward = thinX ? new Vector3(Mathf.Sign(-6 - c.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(-5 - c.z));
        float floorY = c.y >= 3 ? 3.3f : 0f; // upper floor or ground floor (no basement windows)
        if (!opening.IsDoor) { Spawn(opening.Id, new Vector3(c.x, floorY, c.z) + inward * (settings.radius * .7f)); return; }
        if (doorGap && doorGap.Blocked) return; // towels at the gap keep the water out
        var hall = new Vector3(c.x, floorY, c.z) + inward * 1.3f;
        Spawn("Door#0", hall);
        Spawn("Door#1", hall + new Vector3(3.6f, 0, -.4f)); // into the living room
    }

    /// <summary>Creates a puddle (no-op if that source already has one). Also used by snapshot restores.</summary>
    public void Spawn(string source, Vector3 position)
    {
        if (puddles.Any(p => p && p.Source == source)) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Puddle_" + source;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        Destroy(go.GetComponent<Collider>());
        float r = settings.radius;
        go.transform.position = position + Vector3.up * SurfaceLift;
        go.transform.localScale = new Vector3(r * 2, .004f, r * 2);
        go.GetComponent<Renderer>().sharedMaterial = WetMaterial();
        var box = go.AddComponent<BoxCollider>(); // thin, aimable; the player walks through it
        box.size = new Vector3(1, 10, 1);
        if (player) Physics.IgnoreCollision(box, player.GetComponent<CharacterController>());
        var puddle = go.AddComponent<Puddle>();
        puddle.Configure(this, player, source, r);
        puddles.Add(puddle);
    }

    public void Remove(Puddle puddle)
    {
        puddles.Remove(puddle);
        if (puddle) Destroy(puddle.gameObject);
    }

    /// <summary>Snapshot restore: wipe all, then the snapshot's puddles are spawned again.</summary>
    public void Clear() { foreach (var p in puddles.ToList()) Remove(p); }

    /// <summary>Snapshot restore: exactly the saved puddles, where they were.</summary>
    public void Restore(PuddleRecord[] records)
    {
        Clear();
        if (records == null) return;
        foreach (var record in records) Spawn(record.source, record.position);
    }

    public PuddleRecord[] Records() =>
        puddles.Where(p => p).Select(p => new PuddleRecord { source = p.Source, position = p.transform.position - Vector3.up * SurfaceLift }).ToArray();

    void Update()
    {
        if (!player) return;
        var feet = player.transform.position;
        bool inside = puddles.Any(p => p && Mathf.Abs(p.transform.position.y - feet.y) < .6f
            && new Vector2(p.transform.position.x - feet.x, p.transform.position.z - feet.z).magnitude < p.Radius);
        player.puddleSpeedFactor = inside ? settings.speedFactor : 1;
    }

    Material WetMaterial()
    {
        if (wet) return wet;
        // Placeholder: an opaque dark, slightly glossy patch. (A transparent film either vanished or turned white by
        // mirroring the bright interior reflection probe.) Swap for a proper wet decal when art has one.
        wet = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Puddle" };
        wet.SetColor("_BaseColor", new Color(.07f, .08f, .09f));
        wet.SetFloat("_Smoothness", .72f);
        return wet;
    }
}
