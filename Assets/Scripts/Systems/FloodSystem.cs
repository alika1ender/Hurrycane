using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Day 3 basement flooding (Docs/Design/01). Water only comes down the basement stairs. It starts 40 s into Disaster Time,
/// 10 s earlier per unrepaired ground-floor breach, and rises to 40 cm over 90 s. Towels across the stairs (StairBarrier)
/// stop it from starting, or stop the rise. At 20 cm and deeper: the safe room no longer calms, standing in the water
/// hurts (0.5/s) and slows walking to 70%.
/// </summary>
[RequireComponent(typeof(DayDirector))]
public sealed class FloodSystem : MonoBehaviour
{
    public static event Action FloodStarted;

    static readonly string[] GroundFloor = { "W01", "W02", "W03", "W04", "W05", "W06", "W13", "Door" };

    GameSession session;
    DayDirector director;
    MapExplorer player;
    FloodSettings settings;
    StairBarrier barrier;
    Transform water;
    float floorY = -3.2f, clock, startAt = -1;

    public float Depth { get; private set; }
    public bool Flooding => Depth > 0;
    public bool Stopped => barrier && barrier.Blocked;
    public bool SafeRoomFlooded => Depth >= settings.harmfulDepth;
    public bool PlayerInWater => player && SafeRoomFlooded && player.transform.position.y < floorY + Depth - .05f;
    /// <summary>Seconds into Disaster Time when water starts today, or -1 when it won't.</summary>
    public float StartsAt => startAt;

    void Awake()
    {
        session = GetComponent<GameSession>();
        director = GetComponent<DayDirector>();
        settings = director.Config.flood;
    }
    void OnEnable() { DayDirector.PhaseChanged += OnPhase; GameSession.DayStarted += OnDayStarted; }
    void OnDisable() { DayDirector.PhaseChanged -= OnPhase; GameSession.DayStarted -= OnDayStarted; }

    public void Bind(MapExplorer explorer, StairBarrier stairBarrier)
    {
        player = explorer;
        barrier = stairBarrier;
        var floor = FindObjectsByType<Collider>(FindObjectsInactive.Exclude).FirstOrDefault(c => c.name == "COL_BF_Floor");
        Bounds area = floor ? floor.bounds : new Bounds(new Vector3(-6, -3.32f, -5), new Vector3(12, .24f, 10));
        floorY = area.max.y;
        var plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(plane.GetComponent<Collider>());
        plane.name = "FloodWater";
        plane.transform.SetParent(transform, false);
        plane.transform.position = new Vector3(area.center.x, floorY, area.center.z);
        plane.transform.localScale = new Vector3(area.size.x - .05f, .02f, area.size.z - .05f);
        plane.GetComponent<Renderer>().sharedMaterial = WaterMaterial();
        water = plane.transform;
        SetDepth(0);
    }

    void OnDayStarted(int day) { startAt = -1; clock = 0; SetDepth(0); if (barrier) barrier.Urgent = false; }

    void OnPhase(DayPhase phase)
    {
        if (phase != DayPhase.Disaster || !director.Today.floods || Stopped) return;
        var breaches = FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude).Count(o => GroundFloor.Contains(o.Id) && o.State == OpeningState.Broken);
        startAt = Mathf.Max(0, settings.startSeconds - settings.earlierPerBreachSeconds * breaches);
        clock = 0;
    }

    void Update()
    {
        if (!player) return;
        bool storm = director.Phase == DayPhase.Disaster || director.Phase == DayPhase.AfterDisaster;
        if (storm && startAt >= 0)
        {
            float dt = Time.deltaTime * director.debugTimeScale;
            clock += dt;
            if (clock >= startAt && !Stopped && Depth < settings.maxDepth)
            {
                if (Depth <= 0) { barrier.Urgent = true; FloodStarted?.Invoke(); }
                SetDepth(Mathf.Min(settings.maxDepth, Depth + settings.maxDepth / Mathf.Max(1, settings.riseSeconds) * dt));
            }
        }
        if (Stopped && barrier) barrier.Urgent = false;
        bool wading = storm && PlayerInWater;
        player.speedFactor = wading ? settings.speedFactor : 1;
        if (wading) session.health = Mathf.Max(0, session.health - settings.damagePerSecond * Time.deltaTime);
    }

    void SetDepth(float depth)
    {
        Depth = depth;
        if (!water) return;
        water.gameObject.SetActive(depth > .001f);
        var p = water.position; p.y = floorY + depth; water.position = p;
    }

    static Material WaterMaterial()
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "FloodWater" };
        m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.SetColor("_BaseColor", new Color(.10f, .19f, .19f, .86f)); // murky storm water, dark enough to read in a dim basement
        m.SetFloat("_Smoothness", .96f);
        m.SetFloat("_Metallic", .2f);
        return m;
    }
}
