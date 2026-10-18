using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Day 3 basement flooding (Docs/Design/01). Water only comes down the basement stairs. It starts 40 s into Disaster Time,
/// 10 s earlier per unrepaired ground-floor breach, and rises to 40 cm over 90 s. Towels across the stairs (StairBarrier)
/// stop it from starting, or stop the rise. At 20 cm and deeper wading slows walking to 70%.
/// While the main breaker is on, the water is live: it crackles with arcs and Cane can't step into it (he's pushed back
/// and the breaker glows) until the power is switched off. No damage: it's a gate, not a trap.
/// </summary>
[RequireComponent(typeof(DayDirector))]
public sealed class FloodSystem : MonoBehaviour
{
    public static event Action FloodStarted;
    /// <summary>Raised when Cane tries to step into live water (breaker still on) and is pushed back.</summary>
    public static event Action Shocked;

    static readonly string[] GroundFloor = { "W01", "W02", "W03", "W04", "W05", "W06", "W13", "Door" };

    GameSession session;
    DayDirector director;
    MapExplorer player;
    FloodSettings settings;
    StairBarrier barrier;
    Transform water;
    Material waterMaterial;
    Switchable breaker;
    float floorY = -3.2f, clock, startAt = -1, shockReadyAt, blockedAt = -100;
    Vector3 lastDry; bool hasDry, wasTouching;
    Bounds area;
    LineRenderer[] arcs, halos;
    float[] arcUntil;
    Light arcLight;
    Material arcMaterial;

    /// <summary>The main breaker switch (Basement_FusePanel).</summary>
    public Switchable Breaker { get { if (!breaker) foreach (var s in Switchable.All) if (s.Key == "Basement_FusePanel") breaker = s; return breaker; } }
    /// <summary>Cane was just pushed back from live water: the breaker glows and the screen tells him what to do.</summary>
    public bool BlockedRecently => Live && Flooding && Time.time - blockedAt < 8;

    /// <summary>The main breaker was never switched off, so any flood water is electrified.</summary>
    public bool Live
    {
        get
        {
            var b = Breaker;
            return b && !b.Off;
        }
    }
    /// <summary>Feet in the water at any depth (shocks don't wait for the 20 cm "harmful" depth).</summary>
    public bool PlayerTouchingWater => player && Depth > .02f && player.transform.position.y < floorY + Depth - .01f;

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
        area = floor ? floor.bounds : new Bounds(new Vector3(-6, -3.32f, -5), new Vector3(12, .24f, 10));
        floorY = area.max.y;
        var plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(plane.GetComponent<Collider>());
        plane.name = "FloodWater";
        plane.transform.SetParent(transform, false);
        plane.transform.position = new Vector3(area.center.x, floorY, area.center.z);
        plane.transform.localScale = new Vector3(area.size.x - .05f, .02f, area.size.z - .05f);
        waterMaterial = WaterMaterial();
        plane.GetComponent<Renderer>().sharedMaterial = waterMaterial;
        water = plane.transform;
        BuildArcs();
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
        GateLiveWater();
        AnimateWater();
        AnimateArcs();
    }

    /// <summary>
    /// Live water is a wall: stepping into it puts Cane back where he last stood dry. If the water rose under him he may
    /// still walk out (any step that leaves it is allowed).
    /// </summary>
    void GateLiveWater()
    {
        bool touching = PlayerTouchingWater;
        var pos = player.transform.position;
        if (!touching) { lastDry = pos; hasDry = true; wasTouching = false; return; }
        if (Live && Flooding && hasDry && !wasTouching)
        {
            var controller = player.GetComponent<CharacterController>();
            if (controller) controller.enabled = false;
            player.transform.position = lastDry;
            if (controller) controller.enabled = true;
            blockedAt = Time.time;
            if (Time.time >= shockReadyAt) { shockReadyAt = Time.time + 1.5f; Shocked?.Invoke(); Zap(); }
            return;
        }
        wasTouching = true;
    }

    void SetDepth(float depth)
    {
        Depth = depth;
        if (!water) return;
        water.gameObject.SetActive(depth > .001f);
        var p = water.position; p.y = floorY + depth; water.position = p;
    }

    /// <summary>A gentle swell so the surface reads as moving water, and a crackling flicker while it's electrified.</summary>
    void AnimateWater()
    {
        if (!water || !water.gameObject.activeSelf) return;
        float t = Time.time;
        var p = water.position; p.y = floorY + Depth + Mathf.Sin(t * 1.3f) * .008f; water.position = p;
        var glow = BaseGlow;
        // Live: a cold blue tint with brief flickers (kept dark enough that the white bolts stand out against it).
        if (Live) glow += new Color(.01f, .03f, .07f) + (UnityEngine.Random.value < .2f ? new Color(.04f, .1f, .25f) * UnityEngine.Random.Range(.3f, 1f) : Color.clear);
        waterMaterial.SetColor("_EmissionColor", glow);
    }

    // ---- electric arcs over live water ----

    void BuildArcs()
    {
        arcMaterial = new Material(Shader.Find("Sprites/Default")) { name = "FloodArc" };
        arcs = new LineRenderer[18];
        halos = new LineRenderer[arcs.Length];
        arcUntil = new float[arcs.Length];
        for (int i = 0; i < arcs.Length; i++)
        {
            arcs[i] = ArcLine("FloodArc" + i); arcs[i].sortingOrder = 1;
            halos[i] = ArcLine("FloodArcHalo" + i); // wide faint copy behind the bolt: the glow
        }
        var lightGo = new GameObject("FloodArcLight");
        lightGo.transform.SetParent(transform, false);
        arcLight = lightGo.AddComponent<Light>();
        arcLight.type = LightType.Point;
        arcLight.color = new Color(.45f, .7f, 1f);
        arcLight.range = 9;
        arcLight.shadows = LightShadows.None;
        arcLight.enabled = false;
    }

    LineRenderer ArcLine(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = arcMaterial;
        line.useWorldSpace = true;
        line.positionCount = 9;
        line.numCapVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;
        return line;
    }

    /// <summary>A burst of arcs where Cane tried to step in.</summary>
    void Zap()
    {
        if (arcs == null) return;
        var at = player.transform.position; at.y = floorY + Depth + .02f;
        for (int i = 0; i < 3; i++)
        {
            Strike(arcs[i], halos[i], at + new Vector3(UnityEngine.Random.Range(-.6f, .6f), 0, UnityEngine.Random.Range(-.6f, .6f)), 1.6f, .45f);
            arcUntil[i] = Time.time + .35f;
        }
        arcLight.transform.position = at + Vector3.up * .4f;
        arcLight.intensity = 7;
    }

    void AnimateArcs()
    {
        if (arcs == null) return;
        bool on = Live && Flooding && water.gameObject.activeSelf;
        for (int i = 0; i < arcs.Length; i++)
        {
            var line = arcs[i];
            if (!on) { line.enabled = halos[i].enabled = false; continue; }
            if (Time.time < arcUntil[i])
            {
                if (UnityEngine.Random.value < .5f) Jitter(line, halos[i]);
                continue;
            }
            line.enabled = halos[i].enabled = false;
            if (UnityEngine.Random.value > 3.5f * Time.deltaTime) continue; // about 3.5 new arcs per second per line
            var start = new Vector3(UnityEngine.Random.Range(area.min.x + .3f, area.max.x - .3f), floorY + Depth + .02f, UnityEngine.Random.Range(area.min.z + .3f, area.max.z - .3f));
            Strike(line, halos[i], start, UnityEngine.Random.Range(.8f, 2.2f), UnityEngine.Random.Range(.1f, .35f));
            arcUntil[i] = Time.time + UnityEngine.Random.Range(.08f, .25f);
            if (arcLight.intensity < 4) { arcLight.transform.position = start + Vector3.up * .5f; arcLight.intensity = UnityEngine.Random.Range(2.5f, 5f); }
        }
        arcLight.enabled = on;
        if (on) arcLight.intensity = Mathf.MoveTowards(arcLight.intensity, UnityEngine.Random.value < .1f ? 2f : .5f, Time.deltaTime * 30);
    }

    /// <summary>A jagged bolt skimming the surface, with little hops up off the water.</summary>
    static void Strike(LineRenderer line, LineRenderer halo, Vector3 start, float length, float hop)
    {
        float angle = UnityEngine.Random.Range(0, Mathf.PI * 2);
        var dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
        var side = new Vector3(-dir.z, 0, dir.x);
        int n = line.positionCount;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            float jag = i > 0 && i < n - 1 ? UnityEngine.Random.Range(-.12f, .12f) : 0;
            line.SetPosition(i, start + dir * (length * t) + side * jag + Vector3.up * (Mathf.Sin(t * Mathf.PI) * hop + UnityEngine.Random.Range(0, .05f)));
        }
        float w = UnityEngine.Random.Range(.035f, .07f);
        line.startWidth = w; line.endWidth = w * .5f;
        line.startColor = new Color(.92f, .97f, 1f, 1f); line.endColor = new Color(.55f, .78f, 1f, .9f);
        line.enabled = true;
        CopyTo(line, halo);
        halo.startWidth = w * 5; halo.endWidth = w * 3;
        halo.startColor = new Color(.35f, .6f, 1f, .35f); halo.endColor = new Color(.3f, .5f, 1f, .15f);
        halo.enabled = true;
    }

    static void Jitter(LineRenderer line, LineRenderer halo)
    {
        for (int i = 1; i < line.positionCount - 1; i++)
            line.SetPosition(i, line.GetPosition(i) + new Vector3(UnityEngine.Random.Range(-.05f, .05f), UnityEngine.Random.Range(-.02f, .03f), UnityEngine.Random.Range(-.05f, .05f)));
        CopyTo(line, halo);
    }

    static void CopyTo(LineRenderer from, LineRenderer to)
    {
        for (int i = 0; i < from.positionCount; i++) to.SetPosition(i, from.GetPosition(i));
    }

    static readonly Color BaseGlow = new Color(.035f, .05f, .04f);

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
        // Muddy brown-green storm water, opaque enough to stand out on the basement floor, with a faint glow so it still
        // reads in the blackout.
        m.SetColor("_BaseColor", new Color(.36f, .32f, .2f, .92f));
        m.SetFloat("_Smoothness", .9f);
        m.SetFloat("_Metallic", .1f);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.SetColor("_EmissionColor", BaseGlow);
        return m;
    }
}
