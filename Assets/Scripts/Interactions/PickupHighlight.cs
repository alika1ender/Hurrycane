using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Outlines items that can be picked up: a faint amber outline on pickups close by, a bright one on the item under the
/// crosshair. Draws with Graphics.RenderMesh each frame, so no objects are added under the pickups (their bounds,
/// snapshots and stash layout stay untouched). Shader: Resources/PickupOutline.
/// </summary>
public sealed class PickupHighlight : MonoBehaviour
{
    const float NearRadius = 3.5f, RefreshSeconds = .5f;
    static readonly Color Near = new Color(1, .82f, .35f, .35f), Focused = new Color(1, .86f, .4f, .95f);
    static readonly int ColorId = Shader.PropertyToID("_Color"), WidthId = Shader.PropertyToID("_Width"), CenterId = Shader.PropertyToID("_Center");

    MapExplorer player;
    DayDirector director;
    Material material;
    MaterialPropertyBlock block;
    readonly List<Component> targets = new List<Component>();
    readonly Dictionary<Component, MeshFilter[]> meshes = new Dictionary<Component, MeshFilter[]>();
    float refreshAt;

    public void Bind(MapExplorer explorer)
    {
        player = explorer;
        director = GetComponent<DayDirector>();
        var shader = Resources.Load<Shader>("PickupOutline");
        if (!shader) { Debug.LogError("PickupHighlight: PickupOutline shader missing"); enabled = false; return; }
        material = new Material(shader) { name = "PickupOutline" };
        block = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        if (!player || !material || !player.enabled || !director) return;
        if (director.Phase < DayPhase.Preparation || director.Phase > DayPhase.AfterDisaster) return;
        if (Time.time >= refreshAt) Refresh();
        var view = player.viewCamera.transform;
        var focus = player.Focus as Component;
        foreach (var target in targets)
        {
            if (!target || !target.gameObject.activeInHierarchy) continue;
            bool focused = target == focus;
            var offset = target.transform.position - view.position;
            if (!focused && (offset.sqrMagnitude > NearRadius * NearRadius || Vector3.Dot(offset.normalized, view.forward) < .35f)) continue;
            Draw(target, focused);
        }
    }

    void Refresh()
    {
        refreshAt = Time.time + RefreshSeconds;
        targets.Clear();
        targets.AddRange(FindObjectsByType<Pickup>(FindObjectsInactive.Exclude));
        targets.AddRange(FindObjectsByType<PlankStack>(FindObjectsInactive.Exclude));
    }

    void Draw(Component target, bool focused)
    {
        if (!meshes.TryGetValue(target, out var filters) || filters == null) meshes[target] = filters = target.GetComponentsInChildren<MeshFilter>();
        var bounds = new Bounds(); bool any = false;
        foreach (var filter in filters)
        {
            if (!filter) continue;
            var r = filter.GetComponent<Renderer>();
            if (!r || !r.enabled) continue;
            if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
        }
        if (!any) return;
        block.SetColor(ColorId, focused ? Focused : Near);
        block.SetFloat(WidthId, focused ? .014f : .007f);
        block.SetVector(CenterId, bounds.center);
        var parameters = new RenderParams(material) { matProps = block, shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows = false };
        foreach (var filter in filters)
        {
            if (!filter || !filter.sharedMesh) continue;
            var r = filter.GetComponent<Renderer>();
            if (!r || !r.enabled) continue;
            for (int sub = 0; sub < filter.sharedMesh.subMeshCount; sub++)
                Graphics.RenderMesh(parameters, filter.sharedMesh, sub, filter.transform.localToWorldMatrix);
        }
    }

    void OnDestroy() { if (material) Destroy(material); }
}
