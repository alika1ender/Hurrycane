using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum PulseZone { Calm, Tense, Panic, Peak }

/// <summary>
/// Cane's pulse (Docs/Design/05). Drifts to the day/phase resting level; spikes on breaks, the last 10 s of a timer,
/// unfinished tasks at a phase flip and sprinting (Day 3); drops on finished tasks, the basement safe room and
/// breathing (hold Q). Panic/Peak add vignette, shake and (Day 3) slower hold actions. Never blocks a task.
/// Darkness comes from PowerSystem, Broski texts from HandheldSystem. Not built: thunder.
/// </summary>
[RequireComponent(typeof(DayDirector))]
public sealed class PulseSystem : MonoBehaviour
{
    GameSession session;
    DayDirector director;
    MapExplorer player;
    PulseSettings settings;
    float breathHeld, cooldownUntil;
    Vignette vignette;
    ColorAdjustments color;
    DepthOfField blur;
    float blurAmount;
    Vector3 cameraHome;

    public float Bpm => session.pulse;
    /// <summary>Critical heart rate (Day 3 only in practice): slower, blurry, the monitor blinks.</summary>
    /// <summary>Frozen in place by a 180 BPM heart rate (5 s).</summary>
    public bool Frozen => Time.time < frozenUntil;
    public float FrozenLeft => Mathf.Max(0, frozenUntil - Time.time);
    float frozenUntil;
    bool freezeArmed = true, breathLock;
    public bool Critical => Active && Bpm >= settings.criticalBpm;
    public PulseZone Zone => Bpm >= 160 ? PulseZone.Peak : Bpm >= 130 ? PulseZone.Panic : Bpm >= 100 ? PulseZone.Tense : PulseZone.Calm;
    public bool Breathing => breathHeld > 0;
    /// <summary>0..1 while winding up a breath, 1 while it is lowering the pulse.</summary>
    public float BreathProgress => Mathf.Clamp01(breathHeld / Mathf.Max(.01f, settings.breathWindupSeconds));
    public float BreathCooldown => Mathf.Max(0, cooldownUntil - Time.time);

    void Awake()
    {
        session = GetComponent<GameSession>();
        director = GetComponent<DayDirector>();
        settings = director.Config.pulse;
    }
    void OnEnable()
    {
        TaskEngine.TaskCompleted += OnTaskDone;
        BreakableOpening.HitLanded += OnHit;
        DayDirector.PhaseChanged += OnPhase;
        HandheldSystem.MessageReceived += OnText;
    }
    void OnDisable()
    {
        TaskEngine.TaskCompleted -= OnTaskDone;
        BreakableOpening.HitLanded -= OnHit;
        DayDirector.PhaseChanged -= OnPhase;
        HandheldSystem.MessageReceived -= OnText;
    }

    public void Bind(MapExplorer explorer)
    {
        player = explorer;
        cameraHome = player.viewCamera.transform.localPosition;
        var volume = new GameObject("PulseVolume").AddComponent<Volume>();
        volume.transform.SetParent(transform, false);
        volume.isGlobal = true; volume.priority = 20;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        vignette = profile.Add<Vignette>(true); vignette.intensity.Override(0); vignette.smoothness.Override(.45f); vignette.color.Override(Color.black);
        color = profile.Add<ColorAdjustments>(true); color.saturation.Override(0);
        // Critical heart rate blurs vision: a Gaussian depth of field that starts right in front of the eyes.
        blur = profile.Add<DepthOfField>(true);
        blur.mode.Override(DepthOfFieldMode.Gaussian);
        blur.gaussianStart.Override(12f); blur.gaussianEnd.Override(20f); blur.gaussianMaxRadius.Override(.5f); blur.highQualitySampling.Override(true);
        blur.active = false;
        volume.sharedProfile = profile;
    }

    bool Active => director.Phase >= DayPhase.Preparation && director.Phase <= DayPhase.AfterDisaster;
    void Add(float bpm) { if (Active) session.pulse = Mathf.Clamp(session.pulse + bpm, 60, director.Today.pulseCeiling); }

    void OnTaskDone(TaskState task) => Add(-settings.taskDoneDrop);
    void OnText(PhoneMessage message) { var phone = GetComponent<HandheldSystem>(); Add(-(phone ? phone.PulseDropPerText : 3)); }

    void OnHit(BreakableOpening opening, OpeningState result)
    {
        if (!player) return;
        var today = director.Today;
        if (opening.IsDoor && result == OpeningState.Broken) { Add(today.doorBreakSpike); return; }
        if (opening.DistanceTo(player.viewCamera.transform.position) > settings.nearbyRadius) return;
        Add(result == OpeningState.Broken ? today.windowBreakSpike : today.windowBreakSpike * .5f);
    }

    void OnPhase(DayPhase phase)
    {
        if (phase == DayPhase.Preparation) { session.pulse = director.Today.restingPrep; return; }
        if (phase != DayPhase.Disaster && phase != DayPhase.AfterDisaster) return;
        var tasks = GetComponent<TaskEngine>();
        int unfinished = tasks ? tasks.Today.Count(t => t.status == TaskStatus.Open && t.definition.required) : 0;
        Add(Mathf.Min(unfinished * settings.unfinishedTaskRise, settings.unfinishedTaskRiseMax));
    }

    void Update()
    {
        if (!player) return;
        if (!Active) { ApplyEffects(PulseZone.Calm, false); player.movementLocked = false; player.pulseSpeedFactor = 1; breathHeld = 0; breathLock = false; frozenUntil = 0; freezeArmed = true; return; }
        var today = director.Today;
        float dt = Time.deltaTime, pulse = session.pulse, peak = pulse; // peak: spikes land between frames, drift would hide them
        var power = GetComponent<PowerSystem>();
        var flood = GetComponent<FloodSystem>();
        float resting = today.Resting(director.Phase) + (power ? power.RestingRise : 0);
        bool disaster = director.Phase == DayPhase.Disaster;
        float drift = settings.driftPerSecond * (disaster && pulse < resting ? settings.disasterRiseSpeedMultiplier : 1);
        pulse = Mathf.MoveTowards(pulse, resting, drift * dt);
        if (disaster) pulse += settings.disasterRisePerSecond * dt;
        if (director.TimeLeft <= 10) pulse += settings.lastSecondsRisePerSecond * dt;
        if (today.sprintRaisesPulse && player.Sprinting) pulse += settings.sprintRisePerSecond * dt;
        bool safeRoom = InShelter(player.transform.position, flood);
        if (safeRoom) pulse -= settings.safeRoomDropPerSecond * dt;
        pulse -= Breathe(dt);
        session.pulse = Mathf.Clamp(pulse, 60, today.pulseCeiling);

        // 180 BPM: Cane locks up for a few seconds ("Press Q to take a deep breath"). Re-arms once the pulse drops.
        if (freezeArmed && Mathf.Max(peak, session.pulse) >= settings.freezeBpm) { frozenUntil = Time.time + settings.freezeSeconds; freezeArmed = false; }
        if (!freezeArmed && !Frozen && session.pulse < settings.freezeBpm - settings.freezeRearm) freezeArmed = true;
        player.movementLocked = breathLock || Frozen;

        var zone = Zone;
        player.pulseSpeedFactor = Critical ? settings.criticalSpeedFactor : 1;
        player.pulseHoldFactor = !today.pulsePenalties ? 1 : zone == PulseZone.Peak ? 1 + settings.peakTaskPenalty : zone == PulseZone.Panic ? 1 + settings.panicTaskPenalty : 1;
        ApplyEffects(zone, today.pulseVisuals);
    }

    Bounds? shelter;
    bool shelterRead;

    /// <summary>The safe room is now the master bedroom (where the supply box is). Falls back to the basement rule.</summary>
    bool InShelter(Vector3 feet, FloodSystem flood)
    {
        if (!shelterRead)
        {
            shelterRead = true;
            var room = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude).FirstOrDefault(r => r.name == "20_Master_Bedroom_Visual");
            if (room) shelter = room.bounds;
        }
        if (shelter is Bounds b)
            return feet.x > b.min.x && feet.x < b.max.x && feet.z > b.min.z && feet.z < b.max.z && feet.y > b.min.y - .5f && feet.y < b.max.y;
        return feet.y < settings.safeRoomBelowY && !(flood && flood.SafeRoomFlooded);
    }

    /// <summary>Hold Q (standing still): nothing for the wind-up, then the pulse drops for a few seconds; then a cooldown.</summary>
    float Breathe(float dt)
    {
        var keyboard = Keyboard.current;
        // While frozen at 180 BPM the screen says "press Q", so breathing ignores the cooldown then.
        bool canBreathe = player.enabled && Cursor.lockState == CursorLockMode.Locked && (Time.time >= cooldownUntil || Frozen);
        bool held = canBreathe && keyboard != null && keyboard.qKey.isPressed;
        if (!held)
        {
            if (breathHeld > settings.breathWindupSeconds) cooldownUntil = Time.time + settings.breathCooldownSeconds;
            if (breathHeld > 0) breathLock = false;
            breathHeld = 0;
            return 0;
        }
        breathHeld += dt;
        breathLock = true;
        if (breathHeld >= settings.breathWindupSeconds + settings.breathMaxSeconds)
        {
            cooldownUntil = Time.time + settings.breathCooldownSeconds;
            breathHeld = 0; breathLock = false;
            return 0;
        }
        return breathHeld > settings.breathWindupSeconds ? settings.breathDropPerSecond * dt : 0;
    }

    void ApplyEffects(PulseZone zone, bool visuals)
    {
        if (vignette == null) return;
        float targetVignette = !visuals ? 0 : zone == PulseZone.Peak ? .5f : zone == PulseZone.Panic ? .38f : zone == PulseZone.Tense ? .2f : 0;
        float targetSaturation = visuals && zone == PulseZone.Peak ? -55 : 0;
        vignette.intensity.value = Mathf.MoveTowards(vignette.intensity.value, targetVignette, Time.unscaledDeltaTime * .5f);
        color.saturation.value = Mathf.MoveTowards(color.saturation.value, targetSaturation, Time.unscaledDeltaTime * 60);
        // Fade the blur in from far away to right in front of the eyes. (URP clamps the blur radius to at least 0.5,
        // so "no blur" has to switch the effect off rather than set the radius to 0.)
        blurAmount = Mathf.MoveTowards(blurAmount, Critical ? 1 : 0, Time.unscaledDeltaTime * .8f);
        blur.active = blurAmount > .01f;
        blur.gaussianStart.value = Mathf.Lerp(12f, .2f, blurAmount);
        blur.gaussianEnd.value = Mathf.Lerp(20f, 3f, blurAmount);
        blur.gaussianMaxRadius.value = Mathf.Lerp(.5f, 1.5f, blurAmount);
        float shake = !visuals || Time.timeScale == 0 ? 0 : zone == PulseZone.Peak ? .007f : zone == PulseZone.Panic ? .0035f : 0;
        player.viewCamera.transform.localPosition = cameraHome + (shake > 0 ? Random.insideUnitSphere * shake : Vector3.zero);
    }
}
