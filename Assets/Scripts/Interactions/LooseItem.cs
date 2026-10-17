using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class LooseConfig
{
    public float chanceBonus = .1f, moveDistance = 1.3f, holdSeconds = 1.5f;
    public LooseDefinition[] items = new LooseDefinition[0];

    public static LooseConfig Load()
    {
        var file = Resources.Load<TextAsset>("HurrycaneData/loose");
        return file ? JsonUtility.FromJson<LooseConfig>(file.text) : new LooseConfig();
    }
}

[Serializable] public sealed class LooseDefinition { public string @object, label, window, proxy; public int day; }

/// <summary>
/// A loose object near a window (Docs/Design/03, 04): until moved it can become a projectile, adding to that window's
/// break chance. Hold E to move it about a metre into the room. It stays moved for the rest of the game.
/// </summary>
[DisallowMultipleComponent]
public sealed class LooseItem : MonoBehaviour, IInteractable
{
    static readonly List<LooseItem> all = new List<LooseItem>();

    [SerializeField] string label, window;
    [SerializeField] int day;
    LooseConfig config;
    Transform proxy;

    public bool Moved { get; private set; }
    public string Window => window;
    public int Day => day;
    public string Key => name;
    public static IEnumerable<LooseItem> All => all;

    public void Configure(LooseDefinition definition, LooseConfig settings, Transform collisionProxy)
    {
        label = definition.label; window = definition.window; day = definition.day; config = settings; proxy = collisionProxy;
    }

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    /// <summary>Extra break chance for a window from the loose items still next to it.</summary>
    public static float BonusFor(string windowId) => all.Where(i => !i.Moved && i.window == windowId).Sum(i => i.config.chanceBonus);
    public static bool AllMoved(int forDay) => all.Any(i => i.day == forDay) && all.Where(i => i.day == forDay).All(i => i.Moved);

    public string Prompt => Moved ? null : "Move " + label + " away from the window";
    public float HoldSeconds => config.holdSeconds;
    public string Unavailable(MapExplorer user) => null;
    public void Use(MapExplorer user) => MoveAway();

    /// <summary>Slides the item into the room (away from its window) and sets it down on whatever surface is there.</summary>
    public void MoveAway()
    {
        if (Moved) return;
        Moved = true;
        var opening = FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude).FirstOrDefault(o => o.Id == window);
        var bounds = Bounds();
        Vector3 inward = Vector3.zero;
        if (opening)
        {
            var b = opening.GetComponent<Collider>().bounds;
            bool thinX = b.size.x < b.size.z;
            inward = thinX ? new Vector3(Mathf.Sign(-6 - b.center.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(-5 - b.center.z));
        }
        var target = bounds.center + inward * config.moveDistance;
        float floorBelow = bounds.min.y;
        if (Physics.Raycast(target + Vector3.up * .6f, Vector3.down, out var hit, 4, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && !hit.collider.transform.IsChildOf(transform) && (!proxy || !hit.collider.transform.IsChildOf(proxy)))
            floorBelow = hit.point.y;
        var shift = new Vector3(target.x - bounds.center.x, floorBelow - bounds.min.y + .005f, target.z - bounds.center.z);
        transform.position += shift;
        if (proxy) proxy.position += shift;
        Physics.SyncTransforms();
    }

    Bounds Bounds()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}
