using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// All game sound in one place. It listens to the game's existing events and reads the existing systems, so no other
/// script had to change: storm beds and wind that rise with the clock, thunder, the siren, the pulse heartbeat and
/// breathing, footsteps, glass and door breaks, hammering, pickups, drawers and cabinets, the radio, the phone and
/// handbook, and the opening cutscene's football match. Clips are loaded by name from Resources/HurrycaneAudio.
/// Created automatically in any scene that has a MapExplorer (same pattern as GameplaySetup). Plan: AUDIO_SELECTION.md.
/// </summary>
public sealed class AudioDirector : MonoBehaviour
{
    const string Folder = "HurrycaneAudio/";
    public static AudioDirector Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance) return;
        MapExplorer explorer = null;
        foreach (var root in scene.GetRootGameObjects()) if (!explorer) explorer = root.GetComponentInChildren<MapExplorer>(true);
        if (!explorer) return;
        var host = new GameObject("AudioDirector");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<AudioDirector>();
    }

    // ------------------------------------------------------------------ clips and voices

    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    static readonly HashSet<string> warned = new HashSet<string>();
    static AudioClip Clip(string name)
    {
        if (cache.TryGetValue(name, out var clip)) { if (clip) return clip; if (warned.Contains(name)) return null; }
        clip = Resources.Load<AudioClip>(Folder + name);
        if (!clip && warned.Add(name)) Debug.LogWarning("AudioDirector: missing clip " + name);
        cache[name] = clip;
        return clip;
    }

    /// <summary>A looping source whose volume moves smoothly toward a target.</summary>
    sealed class Voice
    {
        public AudioSource source;
        public float target, rate = .4f;
        public void Tick(float dt)
        {
            source.volume = Mathf.MoveTowards(source.volume, target, rate * dt);
            bool want = source.volume > .001f || target > .001f;
            if (want && !source.isPlaying) source.Play();
            else if (!want && source.isPlaying) source.Pause();
        }
    }

    Voice MakeVoice(string clip, float spatial = 0)
    {
        var go = new GameObject("Voice_" + clip);
        go.transform.SetParent(transform, false);
        var source = go.AddComponent<AudioSource>();
        source.clip = Clip(clip); source.loop = true; source.playOnAwake = false; source.volume = 0; source.spatialBlend = spatial;
        // Music, breath and siren stay dry; the storm beds pick up a little of the room.
        source.reverbZoneMix = clip == "ambience_loop" || clip == "breath_loop" || clip == "siren_alert_full" || clip == "cutscene_football_20s" ? .1f : .5f;
        return new Voice { source = source };
    }

    readonly List<AudioSource> pool = new List<AudioSource>();
    int poolNext;

    AudioSource Source()
    {
        if (pool.Count < 20)
        {
            var go = new GameObject("Shot" + pool.Count);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            pool.Add(s);
            return s;
        }
        var next = pool[poolNext]; poolNext = (poolNext + 1) % pool.Count;
        return next;
    }

    /// <summary>Plays a clip once. A position makes it 3D (spatial blend 0.85); no position keeps it flat.</summary>
    public void Shot(string name, float volume = 1, float pitch = 1, Vector3? at = null, float pan = 0)
    {
        var clip = Clip(name);
        if (!clip) return;
        var s = Source();
        s.Stop();
        s.clip = clip; s.volume = Mathf.Clamp01(volume); s.pitch = pitch; s.panStereo = pan; s.loop = false;
        s.reverbZoneMix = Dry.Contains(name) ? 0 : 1; // the room reverb is for the world's sounds, not for Cane's body or the phone
        if (at.HasValue)
        {
            s.transform.position = at.Value;
            s.spatialBlend = .85f; s.minDistance = 3; s.maxDistance = 35; s.rolloffMode = AudioRolloffMode.Linear;
        }
        else s.spatialBlend = 0;
        s.Play();
    }

    static readonly HashSet<string> Dry = new HashSet<string> { "heartbeat_cycle", "phone_notification", "phone_vibrate", "tv_news_jingle", "yawn_man", "cane_mumble", "faint_thud" };
    static string Pick(params string[] names) => names[Random.Range(0, names.Length)];
    string lastStep = "";
    string PickStep(params string[] names)
    {
        string choice = names[Random.Range(0, names.Length)];
        if (names.Length > 1 && choice == lastStep) choice = names[(System.Array.IndexOf(names, choice) + 1) % names.Length];
        lastStep = choice;
        return choice;
    }

    // ------------------------------------------------------------------ systems (found lazily: GameplaySetup builds them)

    MapExplorer player; CharacterController controller;
    DayDirector director; GameSession session; PulseSystem pulse; PowerSystem power; HandheldSystem handheld; CutsceneSystem cutscene; HealthSystem health;

    bool Resolve()
    {
        if (!player) { player = FindAnyObjectByType<MapExplorer>(); if (player) controller = player.GetComponent<CharacterController>(); }
        if (!director) director = FindAnyObjectByType<DayDirector>();
        if (!director) return false;
        if (!session) session = director.GetComponent<GameSession>();
        if (!pulse) pulse = director.GetComponent<PulseSystem>();
        if (!power) power = director.GetComponent<PowerSystem>();
        if (!handheld) handheld = director.GetComponent<HandheldSystem>();
        if (!cutscene) cutscene = director.GetComponent<CutsceneSystem>();
        if (!health) health = director.GetComponent<HealthSystem>();
        return player;
    }

    Voice bedLight, bedMedium, bedHeavy, windRise, ambience, siren, football, breath, nightRain, citySirens;
    AudioLowPassFilter sirenFilter, ambienceFilter;
    AudioReverbZone reverb;

    void Awake() { Instance = this; Build(); }

    /// <summary>Creates the loop voices. Called again if a script recompile in the editor wiped the fields while playing.</summary>
    void Build()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        pool.Clear(); poolNext = 0;
        bedLight = MakeVoice("storm_loop_light"); bedMedium = MakeVoice("storm_loop_medium"); bedHeavy = MakeVoice("storm_loop_heavy");
        windRise = MakeVoice("wind_rising_loop"); ambience = MakeVoice("ambience_loop");
        siren = MakeVoice("siren_alert_full"); football = MakeVoice("cutscene_football_20s"); breath = MakeVoice("breath_loop");
        sirenFilter = siren.source.gameObject.AddComponent<AudioLowPassFilter>(); sirenFilter.cutoffFrequency = 22000;
        ambience.rate = .1f; windRise.rate = .15f; breath.rate = .5f; siren.rate = .7f; football.rate = .5f;
        ambienceFilter = ambience.source.gameObject.AddComponent<AudioLowPassFilter>(); ambienceFilter.cutoffFrequency = 22000;
        // One reverb zone that covers the house; its preset follows the floor Cane is on (see Update).
        var reverbHost = new GameObject("Reverb"); reverbHost.transform.SetParent(transform, false);
        reverb = reverbHost.AddComponent<AudioReverbZone>(); reverb.minDistance = 300; reverb.maxDistance = 400; reverb.reverbPreset = AudioReverbPreset.Livingroom;
        // Cutscene extras: the night rain heard through the wall as Cane sleeps (the light storm bed, dull and quiet),
        // and the distant city sirens in the ending (needs city_sirens_distant in Resources/HurrycaneAudio).
        nightRain = MakeVoice("storm_loop_light"); nightRain.rate = .12f;
        var nightFilter = nightRain.source.gameObject.AddComponent<AudioLowPassFilter>(); nightFilter.cutoffFrequency = 650;
        citySirens = MakeVoice("city_sirens_distant"); citySirens.rate = .06f;
        // The storm sounds get a low-pass so the basement can muffle them.
        stormFilters.Clear();
        foreach (var voice in new[] { bedLight, bedMedium, bedHeavy, windRise })
        {
            var filter = voice.source.gameObject.AddComponent<AudioLowPassFilter>(); filter.cutoffFrequency = 22000;
            stormFilters.Add(filter);
        }
        // Soft limiter on the final mix, so loud moments never hard-clip into crackle.
        var listener = FindAnyObjectByType<AudioListener>();
        if (listener && !listener.GetComponent<MasterLimiter>()) listener.gameObject.AddComponent<MasterLimiter>();
    }

    readonly List<AudioLowPassFilter> stormFilters = new List<AudioLowPassFilter>();
    /// <summary>0 upstairs, 1 in the basement: the storm outside sounds dull and quieter.</summary>
    float muffle;
    /// <summary>Panic dulls the world: above about 135 BPM the outside sounds get muffled and quieter, so the heartbeat and breath stand out.</summary>
    float panicDull;
    float OutsideFactor => (1 - .55f * muffle) * (1 - .45f * panicDull);
    bool faintedWas;
    void OnDestroy() { if (Instance == this) Instance = null; AudioListener.pause = false; }

    void OnEnable()
    {
        MapExplorer.Used += OnUsed;
        BreakableOpening.HitLanded += OnHit;
        HandheldSystem.MessageReceived += OnMessage;
        DayDirector.PhaseChanged += OnPhase;
        GameSession.DayStarted += OnDayStarted;
    }
    void OnDisable()
    {
        MapExplorer.Used -= OnUsed;
        BreakableOpening.HitLanded -= OnHit;
        HandheldSystem.MessageReceived -= OnMessage;
        DayDirector.PhaseChanged -= OnPhase;
        GameSession.DayStarted -= OnDayStarted;
    }

    // ------------------------------------------------------------------ events

    static IInteractable Unwrap(IInteractable t)
    {
        if (t is InteractionProxy proxy)
        {
            var field = typeof(InteractionProxy).GetField("target", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && field.GetValue(proxy) is IInteractable inner) return inner;
        }
        return t;
    }

    readonly Dictionary<RadioSwitch, AudioSource> radios = new Dictionary<RadioSwitch, AudioSource>();

    void OnUsed(MapExplorer user, IInteractable raw)
    {
        var target = Unwrap(raw);
        var component = target as Component;
        Vector3 at = component ? component.transform.position : user.transform.position;
        switch (target)
        {
            case BreakableOpening opening:
                if (opening.State == OpeningState.Reinforced) Shot("plank_place", .9f, Random.Range(.95f, 1.05f), opening.Center);
                else if (opening.State == OpeningState.Repaired) Shot("tape_rip", .9f, 1, opening.Center);
                break;
            case CabinetOpenable cabinet:
                if (cabinet.IsRoomDoor) break; // no room-door sound chosen
                if (cabinet.IsDrawer) Shot(cabinet.IsOpen ? "drawer_open" : "drawer_close", .8f, Random.Range(.96f, 1.04f), at);
                else Shot(cabinet.IsOpen ? "cabinet_open_A" : Pick("cabinet_close_A", "cabinet_close_B"), .8f, Random.Range(.96f, 1.04f), at);
                break;
            case RadioSwitch radio:
                ToggleRadio(radio);
                break;
            case Switchable _:
                Shot("flashlight_click", .8f, .85f, at);
                break;
            default:
                Shot("item_pickup", .5f, Random.Range(.95f, 1.05f)); // pickups, stashes, plank stack, towels, loose items
                break;
        }
    }

    void ToggleRadio(RadioSwitch radio)
    {
        if (!radios.TryGetValue(radio, out var source) || !source)
        {
            var go = new GameObject("RadioStatic");
            go.transform.SetParent(radio.transform, false);
            source = go.AddComponent<AudioSource>();
            source.clip = Clip("radio_static_loop"); source.loop = true; source.spatialBlend = 1;
            source.minDistance = 1.5f; source.maxDistance = 14; source.rolloffMode = AudioRolloffMode.Linear;
            source.volume = .5f; // the clip is already quiet (-27 dB average)
            radios[radio] = source;
        }
        if (radio.IsOn) source.Play(); else source.Stop();
        Shot("flashlight_click", .6f, .9f, radio.transform.position);
    }

    void OnHit(BreakableOpening opening, OpeningState result)
    {
        var at = opening.Center;
        if (result == OpeningState.Cracked) Shot("door_break_heavy", .9f, Random.Range(.95f, 1.05f), at);          // boards or the brace give way
        else if (result == OpeningState.Broken)
        {
            if (opening.IsDoor) Shot("door_break_heavy", 1, 1, at);
            else Shot("glass_break", 1, Random.Range(.96f, 1.04f), at);
        }
    }

    void OnMessage(PhoneMessage message) { Shot("phone_vibrate", .7f); Invoke(nameof(Chime), .5f); }
    void Chime() => Shot("phone_notification", .6f);

    float thunderTimer = 6, gustTimer = 5, hammerTimer, nextBeat, openingClock, sirenBoost, sirenBoostTotal = 25;

    void OnPhase(DayPhase phase)
    {
        if (phase == DayPhase.Alert) { if (!vibratePlayed) Shot("phone_vibrate", .7f); Invoke(nameof(Chime), .5f); } // the opening already buzzed the phone
        if (phase == DayPhase.Disaster) { Shot("thunder_02", 1, 1); WindowWeather.Flash(); thunderTimer = Random.Range(5f, 9f); }
        if (phase == DayPhase.Preparation) thunderTimer = Random.Range(12f, 22f);
    }

    void OnDayStarted(int day)
    {
        // Mornings after Day 1 start with the sirens waking Cane; they fade over the first seconds.
        sirenBoost = day >= 2 ? sirenBoostTotal : 0;
    }

    // ------------------------------------------------------------------ per frame

    PowerSystem lastPower; bool flashlightWas; HandheldSystem.View viewWas;
    float stride, lastGroundedY, lastY, smoothVy, lastGroundTime;

    void Update()
    {
        AudioListener.pause = Time.timeScale == 0f;
        if (bedLight == null) { Instance = this; Build(); }
        if (!Resolve()) { ambience.target = .12f; Tick(Time.unscaledDeltaTime); return; }
        float dt = Time.deltaTime;
        var phase = director.Phase;
        bool playing = phase >= DayPhase.Preparation && phase <= DayPhase.AfterDisaster;
        int day = session ? Mathf.Max(1, session.CurrentDay) : 1;
        muffle = Mathf.MoveTowards(muffle, player.transform.position.y < -1.2f ? 1 : 0, dt * 1.5f);
        // Room reverb by floor: concrete basement, the living floor, the small rooms upstairs.
        if (reverb)
        {
            float py = player.transform.position.y;
            var wanted = py < -1.2f ? AudioReverbPreset.Hallway : py > 2.2f ? AudioReverbPreset.Room : AudioReverbPreset.Livingroom;
            if (reverb.reverbPreset != wanted) reverb.reverbPreset = wanted;
        }
        // Panic dulls the world (Day 2 onward; Day 1's pulse never leaves green).
        float panicTarget = playing && pulse && day >= 2 ? Mathf.InverseLerp(135f, 170f, pulse.Bpm) : 0;
        panicDull = Mathf.MoveTowards(panicDull, panicTarget, dt * .6f);

        Storm(phase, playing, day, dt);
        Cutscene(phase, dt);
        Pulse(playing, day, dt);
        Footsteps(playing, dt);
        Hammering(dt);
        Gadgets();
        Tick(dt);
    }

    void Tick(float dt)
    {
        foreach (var voice in new[] { bedLight, bedMedium, bedHeavy, windRise, ambience, siren, football, breath, nightRain, citySirens }) voice.Tick(dt);
    }

    float StormIntensity(DayPhase phase, int day, out float progress)
    {
        progress = 0;
        float len = 1;
        if (phase >= DayPhase.Preparation && phase <= DayPhase.AfterDisaster)
        {
            len = Mathf.Max(1, director.Today.Length(phase));
            progress = 1 - Mathf.Clamp01(director.TimeLeft / len);
        }
        float scale = day == 1 ? .55f : day == 2 ? .8f : 1f;
        switch (phase)
        {
            case DayPhase.Menu: return .15f;
            case DayPhase.Opening: return .18f;
            case DayPhase.Alert: return .25f;
            case DayPhase.Preparation: return Mathf.Lerp(.2f, .45f, progress) * scale;
            case DayPhase.Disaster: return Mathf.Lerp(.6f, 1f, progress) * scale;
            case DayPhase.AfterDisaster: return Mathf.Lerp(.45f, .12f, progress) * scale;
            case DayPhase.Night: return .08f;
            case DayPhase.Ending: return .1f;
            default: return .06f; // Ended, GameOver
        }
    }

    void Storm(DayPhase phase, bool playing, int day, float dt)
    {
        float progress;
        float intensity = StormIntensity(phase, day, out progress);
        float bed = .25f + .55f * intensity;
        float wLight = Mathf.Clamp01(1 - intensity / .5f);
        float wMedium = Mathf.Clamp01(1 - Mathf.Abs(intensity - .5f) / .4f);
        float wHeavy = Mathf.Clamp01((intensity - .5f) / .5f);
        float outside = OutsideFactor;
        // Near a window the storm is louder and clearer (more so at a broken one, less at a boarded one).
        float near = windowProximity;
        bedLight.target = bed * wLight * outside * (1 + .5f * near); bedMedium.target = bed * wMedium * outside * (1 + .5f * near); bedHeavy.target = bed * wHeavy * outside * (1 + .5f * near);
        windRise.target = Mathf.Min(1f, .5f * intensity * outside * (1 + 1.4f * near));
        float cutoff = Mathf.Min(Mathf.Lerp(22000f, 900f, muffle), Mathf.Lerp(22000f, 800f, panicDull));
        foreach (var filter in stormFilters) if (filter) filter.cutoffFrequency = Mathf.MoveTowards(filter.cutoffFrequency, cutoff, 20000 * dt + 1);
        if (ambienceFilter) ambienceFilter.cutoffFrequency = Mathf.MoveTowards(ambienceFilter.cutoffFrequency, Mathf.Lerp(22000f, 1800f, panicDull), 20000 * dt + 1);
        WindowAudio(intensity, dt);

        // The action music: quiet on Day 1, louder on Day 2, and loud on Day 3, and it swells in the storm itself.
        float musicBase = day <= 1 ? .16f : day == 2 ? .28f : .46f;
        float musicPhase = phase == DayPhase.Disaster ? 1.3f : phase == DayPhase.AfterDisaster ? .6f : phase == DayPhase.Night ? .2f : 1f;
        float musicPulse = day >= 3 && pulse && pulse.Bpm > 130 ? .12f : 0;
        ambience.target = phase == DayPhase.GameOver ? .08f : phase == DayPhase.Menu ? .14f : phase == DayPhase.Ending || phase == DayPhase.Ended ? .25f
            : playing ? Mathf.Clamp01(musicBase * musicPhase + musicPulse) : .16f;

        // Random wind gusts between the loops; more often as the storm builds.
        gustTimer -= dt * (1 + 2 * windowProximity); // gusts come more often by the windows
        if (gustTimer <= 0 && intensity > .12f)
        {
            Shot("wind_gust", (.25f + .5f * intensity) * outside, Random.Range(.9f, 1.15f), null, Random.Range(-.8f, .8f));
            gustTimer = Random.Range(6f, 15f) / (.5f + intensity);
        }
        else if (gustTimer <= 0) gustTimer = 4;

        // Thunder: distant in late Preparation, frequent in Disaster, rare after.
        thunderTimer -= dt;
        if (thunderTimer <= 0)
        {
            float gapScale = day >= 3 ? .75f : 1f; // more thunder on the last day
            if (phase == DayPhase.Disaster) { WindowWeather.Flash(); Shot(Pick("thunder_01", "thunder_03", "thunder_04", "thunder_05", "thunder_06"), Random.Range(.7f, 1f) * outside, Random.Range(.92f, 1.05f), null, Random.Range(-.5f, .5f)); thunderTimer = Random.Range(7f, 16f) * gapScale; }
            else if (phase == DayPhase.Preparation && day >= 2 && progress > .4f) { WindowWeather.Flash(.35f); Shot(Pick("thunder_01", "thunder_03"), .35f * outside, .9f, null, Random.Range(-.5f, .5f)); thunderTimer = Random.Range(20f, 35f); }
            else if (phase == DayPhase.AfterDisaster && progress < .5f) { WindowWeather.Flash(.6f); Shot(Pick("thunder_04", "thunder_06"), .4f * outside, .88f); thunderTimer = Random.Range(25f, 40f); }
            else thunderTimer = 5;
        }

        // Siren: flat, filtered when distant. The opening cutscene drives it separately (Cutscene()).
        if (!(cutscene && cutscene.Playing && cutscene.Kind == "opening"))
        {
            float target = 0, sirenCutoff = 22000;
            if (phase == DayPhase.Alert) target = .5f;
            else if (sirenBoost > 0) { target = .6f * (sirenBoost / sirenBoostTotal); sirenCutoff = 3500; sirenBoost -= dt; }
            if (phase == DayPhase.Preparation) { target = Mathf.Max(target, .1f * (1 - progress)); sirenCutoff = 1300; }
            siren.target = target;
            sirenFilter.cutoffFrequency = Mathf.MoveTowards(sirenFilter.cutoffFrequency, sirenCutoff, 12000 * dt);
        }
    }

    // ---- windows: how close Cane is to one (wind), and rain on every window's glass (3D loops)
    BreakableOpening[] openings = new BreakableOpening[0];
    float openingsRefresh, windowProximity;
    readonly Dictionary<BreakableOpening, AudioSource> rainSources = new Dictionary<BreakableOpening, AudioSource>();

    void WindowAudio(float intensity, float dt)
    {
        openingsRefresh -= dt;
        if (openingsRefresh <= 0) { openings = FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude, FindObjectsSortMode.None); openingsRefresh = 3; }
        var at = player.viewCamera.transform.position;
        float best = 0;
        foreach (var o in openings)
        {
            if (!o || !o.InPlay) continue;
            // Boarded openings hold the storm back; broken ones let it in.
            float exposure = o.State == OpeningState.Broken ? 1.4f : o.State == OpeningState.Cracked ? 1.1f : o.State == OpeningState.Reinforced ? .5f : o.State == OpeningState.Repaired ? .8f : 1f;
            best = Mathf.Max(best, Mathf.Clamp01(1 - o.DistanceTo(at) / 3.5f) * exposure);
            if (!o.IsDoor) UpdateRain(o, exposure, intensity, dt);
        }
        windowProximity = Mathf.MoveTowards(windowProximity, Mathf.Min(1.4f, best), dt * 2f);
    }

    void UpdateRain(BreakableOpening o, float exposure, float intensity, float dt)
    {
        if (!rainSources.TryGetValue(o, out var s) || !s)
        {
            var clip = Clip("rain_window_loop");
            if (!clip) return;
            var go = new GameObject("WindowRain_" + o.Id);
            go.transform.SetParent(transform, false);
            go.transform.position = o.Center;
            s = go.AddComponent<AudioSource>();
            s.clip = clip; s.loop = true; s.playOnAwake = false; s.volume = 0; s.spatialBlend = 1;
            s.minDistance = 1.2f; s.maxDistance = 7; s.rolloffMode = AudioRolloffMode.Linear; s.reverbZoneMix = .3f;
            s.time = Random.value * clip.length * .9f; // so neighbouring windows are not in step
            rainSources[o] = s;
        }
        float stormy = Mathf.InverseLerp(.15f, .7f, intensity);
        float target = .6f * stormy * exposure;
        s.volume = Mathf.MoveTowards(s.volume, target, .5f * dt);
        if (s.volume > .001f && !s.isPlaying) s.Play();
        else if (s.volume <= .001f && target <= .001f && s.isPlaying) s.Pause();
    }

    bool sirenWasHeard;

    void Cutscene(DayPhase phase, float dt)
    {
        bool opening = cutscene && cutscene.Playing && cutscene.Kind == "opening";
        if (opening)
        {
            openingClock += dt;
            // The news anchor is a mumble from the TV: the news plays from about 1.2 s until the match comes on at 14.6 s.
            if (!newsPlayed && openingClock >= 1.2f) { newsPlayed = true; PlayNews(); Shot("tv_news_jingle", .35f, 1, TvPosition); } // news flash sting under the anchor
            // The match is on the TV roughly 14.6 s to 24 s into the opening.
            football.target = openingClock > 14.6f && openingClock < 24f ? .14f : 0;
            // Eyes get heavy at about 18 s: he yawns, then drifts off.
            if (!yawnPlayed && openingClock >= 17.6f) { yawnPlayed = true; Shot("yawn_man", .6f); }
            // Night rain through the wall while he sleeps and the screen is black (about 21 s to 30 s).
            nightRain.target = openingClock > 21f && openingClock < 29.5f ? .2f : 0;
            // The alarm lights pulse, but the siren stays steady like in the game: it starts when the lights start and
            // runs for the 6.8 s of the wake-up, then fades.
            if (sirenStart < 0 && cutscene.Siren > .05f) sirenStart = openingClock;
            siren.target = sirenStart >= 0 && openingClock < sirenStart + 7.4f ? .5f : 0;
            sirenFilter.cutoffFrequency = 22000;
            if (!mumblePlayed && openingClock >= 22.2f) { mumblePlayed = true; Shot("cane_mumble", .8f); } // Cane mutters as he drifts off
            // He looks down at the phone at about 34 s: it buzzes in his hand.
            if (!vibratePlayed && sirenStart >= 0 && openingClock > sirenStart + 7f) { vibratePlayed = true; Shot("phone_vibrate", .8f); }
            sirenWasHeard = true;
        }
        else
        {
            openingClock = 0;
            football.target = 0; nightRain.target = 0;
            yawnPlayed = vibratePlayed = mumblePlayed = false; sirenStart = -1;
            if (newsSource && newsSource.isPlaying) newsSource.Stop(); // the cutscene was skipped
            newsPlayed = false;
            if (sirenWasHeard && phase != DayPhase.Opening) sirenWasHeard = false;
        }

        // Ending: distant city sirens fade in as Cane walks through the house and stay while he looks at the street.
        bool ending = cutscene && cutscene.Playing && cutscene.Kind == "ending";
        if (ending)
        {
            endingClock += dt;
            citySirens.target = endingClock > 5f && endingClock < 21f ? Mathf.Lerp(0f, .38f, Mathf.Clamp01((endingClock - 5f) / 8f)) : 0;
        }
        else { endingClock = 0; citySirens.target = 0; }
    }
    bool yawnPlayed, vibratePlayed, mumblePlayed; float endingClock, sirenStart = -1;

    // ---- TV news anchor (mumble). Clip: tv_news_mumble (forward, muffled). Try tv_news_mumble_alt for a babble version.
    AudioSource newsSource;
    bool newsPlayed;
    static readonly Vector3 TvPosition = new Vector3(-5.22f, 1.18f, -4.22f);
    const string NewsClip = "tv_news_mumble";

    void PlayNews()
    {
        if (!newsSource)
        {
            var go = new GameObject("TvNews");
            go.transform.SetParent(transform, false);
            newsSource = go.AddComponent<AudioSource>();
            newsSource.loop = false; newsSource.playOnAwake = false;
            newsSource.spatialBlend = .5f; newsSource.minDistance = 6; newsSource.maxDistance = 35; newsSource.rolloffMode = AudioRolloffMode.Linear;
        }
        var tv = GameObject.Find("Living_TV_Animimo_Visual");
        newsSource.transform.position = tv ? tv.transform.position : TvPosition;
        newsSource.clip = Clip(NewsClip); newsSource.volume = 1f; newsSource.pitch = 1;
        if (newsSource.clip) newsSource.Play();
    }

    void Pulse(bool playing, int day, float dt)
    {
        bool alive = playing && pulse && !(health && health.Fainted);
        if (!alive) { breath.target = 0; return; }
        float bpm = pulse.Bpm;
        // Heartbeat: one lub-dub cycle every 60 / BPM s. Audible from 85 BPM and clearly present by 120; sped up when the
        // interval is shorter than the clip's body. Day 1 stays gentle (the pulse never leaves green there).
        float level = Mathf.InverseLerp(85f, 150f, bpm);
        float volume = Mathf.Lerp(.45f, 1f, level) * (day == 1 ? .6f : 1f);
        nextBeat -= dt;
        if (nextBeat <= 0)
        {
            nextBeat = 60f / Mathf.Max(60f, bpm);
            if (bpm >= 85f) Shot("heartbeat_cycle", volume, Mathf.Clamp(.69f * bpm / 60f, 1f, 1.8f));
        }
        // Breathing joins at 90 BPM, faster and much louder as the pulse climbs (full volume by about 150 BPM).
        float t = Mathf.InverseLerp(90f, 150f, bpm);
        breath.target = bpm >= 90 ? Mathf.Lerp(.4f, 1f, t) * (day == 1 ? .6f : 1f) : 0;
        breath.source.pitch = Mathf.Lerp(.9f, 1.45f, t);
    }

    void Footsteps(bool playing, float dt)
    {
        if (!player) return;
        float y = player.transform.position.y;
        // Height change per second, smoothed, tells stairs from flat floor in both directions.
        float vy = dt > 0 ? (y - lastY) / dt : 0; lastY = y;
        smoothVy = Mathf.Lerp(smoothVy, vy, 1 - Mathf.Exp(-8f * dt));
        if (!playing || !controller || !player.enabled || Cursor.lockState != CursorLockMode.Locked) { stride = 0; return; }
        if (controller.isGrounded) lastGroundTime = Time.time;
        var velocity = controller.velocity;
        float speed = new Vector2(velocity.x, velocity.z).magnitude;
        // Going down a ramp the controller reports "not grounded" on most frames, so allow a short grace period.
        bool grounded = controller.isGrounded || Time.time - lastGroundTime < .3f;
        if (!grounded || speed < .4f) { stride = Mathf.Max(0, stride - dt); return; }
        stride += speed * dt;
        bool onStairs = Mathf.Abs(smoothVy) > .45f;
        float length = onStairs ? 1.0f : player.Sprinting ? 1.5f : 1.25f;
        if (stride < length) return;
        stride = 0;
        var flood = director ? director.GetComponent<FloodSystem>() : null;
        bool wet = (flood && flood.PlayerInWater) || player.puddleSpeedFactor < .99f;
        string clip;
        if (wet) clip = PickStep("step_water_01", "step_water_02", "step_water_03", "step_water_04");
        else if (onStairs) clip = PickStep("stair_step_01", "stair_step_02", "stair_step_03", "stair_step_04"); // up and down use the same steps
        else if (y < -1f) clip = PickStep("step_concrete_01", "step_concrete_02", "step_concrete_03", "step_concrete_04", "step_concrete_05", "step_concrete_06");
        else clip = PickStep("step_wood_01", "step_wood_02", "step_wood_03", "step_wood_04", "step_wood_05", "step_wood_06");
        Shot(clip, wet ? .65f : player.Sprinting ? .6f : .4f, Random.Range(.93f, 1.07f));
    }

    void Hammering(float dt)
    {
        var focus = player ? Unwrap(player.Focus) : null;
        if (player && player.HoldProgress > .02f && focus is BreakableOpening opening)
        {
            hammerTimer -= dt;
            if (hammerTimer <= 0)
            {
                Shot(Pick("hammer_hit_A", "hammer_hit_B", "hammer_hit_C"), .7f, Random.Range(.95f, 1.05f), opening.Center);
                hammerTimer = Random.Range(.38f, .55f);
            }
        }
        else hammerTimer = 0;
    }

    void Gadgets()
    {
        if (power)
        {
            bool on = power.FlashlightOn;
            if (power == lastPower && on != flashlightWas) Shot("flashlight_click", .7f);
            lastPower = power; flashlightWas = on;
        }
        if (health)
        {
            if (health.Fainted && !faintedWas) Shot("faint_thud", 1);
            faintedWas = health.Fainted;
        }
        if (handheld)
        {
            var view = handheld.Open;
            if (view != viewWas)
            {
                if (view == HandheldSystem.View.Handbook) Shot(Pick("book_open_01", "book_open_02"), .6f);
                else if (viewWas == HandheldSystem.View.Handbook) Shot(Pick("book_close_01", "book_close_02"), .6f);
                viewWas = view;
            }
        }
    }
}
