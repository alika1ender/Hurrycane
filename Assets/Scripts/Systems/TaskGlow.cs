using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes the current task's targets shine through walls once its location is revealed (TaskEngine.Targets): items to
/// fetch, the supply box, windows to reinforce, switches, loose items, the stair barrier, puddles. Pulses gently.
/// Draws with Graphics.RenderMesh each frame (nothing is added to the scene). Shader: Resources/TaskGlow.
/// </summary>
[RequireComponent(typeof(TaskEngine))]
public sealed class TaskGlow : MonoBehaviour
{
    static readonly Color Glow = new Color(1f, .74f, .22f);
    static readonly int ColorId = Shader.PropertyToID("_Color"), WidthId = Shader.PropertyToID("_Width"), CenterId = Shader.PropertyToID("_Center");

    TaskEngine engine;
    DayDirector director;
    MapExplorer player;
    Material material;
    MaterialPropertyBlock block;
    Mesh cube;
    readonly Dictionary<Component, MeshFilter[]> meshes = new Dictionary<Component, MeshFilter[]>();

    public void Bind(MapExplorer explorer)
    {
        player = explorer;
        engine = GetComponent<TaskEngine>();
        director = GetComponent<DayDirector>();
        var shader = Resources.Load<Shader>("TaskGlow");
        if (!shader) { Debug.LogError("TaskGlow: shader missing from Resources"); enabled = false; return; }
        material = new Material(shader) { name = "TaskGlow" };
        block = new MaterialPropertyBlock();
        cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
    }

    void LateUpdate()
    {
        if (!material || !player || director.Phase < DayPhase.Preparation || director.Phase > DayPhase.AfterDisaster) return;
        var targets = engine.Targets;
        if (targets.Count == 0) return;
        // Fade in after the reveal, then a slow shine.
        float sinceReveal = Time.time - engine.AssignedAt - director.Today.locationDelaySeconds;
        float fadeIn = engine.Current != null && engine.Current.definition.id == "store" ? 1 : Mathf.Clamp01(sinceReveal / .6f);
        float pulse = .55f + .45f * Mathf.Sin(Time.time * 4f);
        var color = Glow; color.a = fadeIn * Mathf.Lerp(.45f, .95f, pulse);
        foreach (var target in targets) if (target && target.gameObject.activeInHierarchy) Draw(target, color);
    }

    void Draw(Component target, Color color)
    {
        if (!meshes.TryGetValue(target, out var filters)) meshes[target] = filters = MeshesOf(target);
        var parameters = new RenderParams(material) { matProps = block, shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows = false };
        block.SetColor(ColorId, color);
        block.SetFloat(WidthId, .02f);
        if (filters.Length == 0)
        {
            // Nothing to draw (barrier, door gap): glow its collider box instead.
            var collider = target.GetComponent<Collider>();
            if (!collider) return;
            var b = collider.bounds;
            block.SetVector(CenterId, b.center);
            Graphics.RenderMesh(parameters, cube, 0, Matrix4x4.TRS(b.center, Quaternion.identity, b.size));
            return;
        }
        var bounds = new Bounds(); bool any = false;
        foreach (var f in filters) if (f) { var r = f.GetComponent<Renderer>(); var fb = r ? r.bounds : new Bounds(f.transform.position, Vector3.zero); if (!any) { bounds = fb; any = true; } else bounds.Encapsulate(fb); }
        block.SetVector(CenterId, bounds.center);
        foreach (var filter in filters)
        {
            if (!filter || !filter.sharedMesh) continue;
            for (int sub = 0; sub < filter.sharedMesh.subMeshCount; sub++)
                Graphics.RenderMesh(parameters, filter.sharedMesh, sub, filter.transform.localToWorldMatrix);
        }
    }

    /// <summary>The meshes that outline a target. Windows and the door use their collision proxy (the glass/door shape).</summary>
    static MeshFilter[] MeshesOf(Component target)
    {
        if (target is BreakableOpening) { var own = target.GetComponent<MeshFilter>(); return own ? new[] { own } : new MeshFilter[0]; }
        var list = new List<MeshFilter>();
        foreach (var f in target.GetComponentsInChildren<MeshFilter>())
        {
            var r = f.GetComponent<Renderer>();
            if (f.sharedMesh && r && r.enabled) list.Add(f);
        }
        return list.ToArray();
    }
}
