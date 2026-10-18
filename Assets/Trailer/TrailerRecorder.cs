#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Editor-only trailer tool (not in builds). In Play mode it films scripted shots of the real game: every frame is
/// rendered at 1920x1080 into a RenderTexture and saved as a JPG under <see cref="Folder"/>/shots/&lt;name&gt;/.
/// Time.captureFramerate keeps the game clock at exactly 30 fps however slowly frames are saved.
/// TrailerCompositor then cuts the shots together with titles and sound into the final MP4.
/// Usage (Play mode): TrailerRecorder.Folder = "..."; TrailerRecorder.Get().Record("house", "board", ...).
/// </summary>
public sealed class TrailerRecorder : MonoBehaviour
{
    public const int Width = 1920, Height = 1080, Fps = 30;
    public static string Folder;

    public string status = "idle";
    public bool busy;

    GameSession session;
    DayDirector director;
    MapExplorer player;
    Camera cam;
    Canvas hud;
    RenderTexture rt;
    Texture2D grab;
    string shotDir;
    int frame;
    float savedFov;
    bool calmPulse = true;
    // The pose is applied again in LateUpdate so nothing in Update (cutscenes, the player rig) can move the camera.
    bool posed;
    Vector3 poseEye, poseBody;
    Quaternion poseRot, poseBodyRot;
    Light torch;

    const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public static TrailerRecorder Get()
    {
        var existing = FindAnyObjectByType<TrailerRecorder>();
        return existing ? existing : new GameObject("TrailerRecorder").AddComponent<TrailerRecorder>();
    }

    public void Record(params string[] shots) { if (!busy) StartCoroutine(RecordAll(shots)); }

    IEnumerator RecordAll(string[] shots)
    {
        busy = true;
        Setup();
        foreach (var name in shots)
        {
            status = "recording " + name;
            shotDir = Path.Combine(Folder, "shots", name);
            if (Directory.Exists(shotDir)) Directory.Delete(shotDir, true);
            Directory.CreateDirectory(shotDir);
            frame = 0;
            calmPulse = true;
            posed = false;
            if (torch) torch.enabled = false;
            Hud(false);
            IEnumerator routine = null;
            try { routine = Shot(name); }
            catch (Exception e) { Debug.LogException(e); }
            if (routine != null) yield return routine;
        }
        Teardown();
        status = "done " + string.Join(",", shots);
        busy = false;
    }

    // ------------------------------------------------------------------ setup

    void Setup()
    {
        Application.runInBackground = true;
        session = GameSession.Instance;
        director = session.GetComponent<DayDirector>();
        player = FindAnyObjectByType<MapExplorer>();
        cam = player.viewCamera;
        hud = FindAnyObjectByType<GameUI>().GetComponent<Canvas>();
        rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        grab = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        savedFov = cam.fieldOfView;
        Time.captureFramerate = Fps;
        AudioListener.volume = 0;
    }

    void Teardown()
    {
        Hud(false);
        cam.targetTexture = null;
        cam.fieldOfView = savedFov;
        Time.captureFramerate = 0;
        AudioListener.volume = 1;
        player.enabled = true;
        director.debugTimeScale = 1;
        rt.Release();
    }

    /// <summary>Starts a day in a known state: intro skipped, clock frozen (no random storm hits), player under script control.</summary>
    void Day(int day, DayPhase phase = DayPhase.Preparation)
    {
        session.StartDay(day);
        var cutscene = session.GetComponent<CutsceneSystem>();
        if (cutscene) cutscene.Finish();
        director.SkipIntro();
        Set(director, "ShowingControls", false);
        if (phase != DayPhase.Preparation) Call(director, "EnterPhase", phase);
        director.debugTimeScale = 0;
        player.enabled = false;
        var ui = FindAnyObjectByType<GameUI>();
        if (ui) typeof(GameUI).GetField("bannerUntil", Any)?.SetValue(ui, 0f); // no "PREPARATION TIME" banner
        cam.targetTexture = rt;
        cam.fieldOfView = 62;
        foreach (var o in FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude)) if (o.InPlay) o.SetState(OpeningState.Unreinforced);
        var puddles = session.GetComponent<PuddleSystem>();
        if (puddles) foreach (var p in puddles.Puddles.ToArray()) if (p) Destroy(p.gameObject);
    }

    static BreakableOpening Opening(string id) => FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude).FirstOrDefault(o => o.Id == id);

    void Hud(bool on)
    {
        if (!hud) return;
        hud.renderMode = on ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
        if (on) { hud.worldCamera = cam; hud.planeDistance = .3f; }
    }

    static void Storm(float strength, float final)
    {
        var weather = FindAnyObjectByType<WindowWeather>();
        if (!weather) return;
        typeof(WindowWeather).GetField("strength", Any).SetValue(weather, strength);
        typeof(WindowWeather).GetField("finalStorm", Any).SetValue(weather, final);
    }

    static void Set(object target, string property, object value)
    {
        var p = target.GetType().GetProperty(property, Any);
        p?.GetSetMethod(true)?.Invoke(target, new[] { value });
    }
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Any).Invoke(target, args);

    /// <summary>Camera at an eye point looking at a target. The body goes under the eye unless given.</summary>
    void Pose(Vector3 eye, Vector3 target, Vector3? body = null, float shake = 0)
    {
        var look = target - eye;
        var flat = new Vector3(look.x, 0, look.z);
        if (flat.sqrMagnitude < 1e-6f) flat = Vector3.forward;
        poseBody = body ?? eye - Vector3.up * 1.68f;
        poseBodyRot = Quaternion.LookRotation(flat);
        var rot = Quaternion.LookRotation(look);
        if (shake > 0)
        {
            float t = Time.time * 9;
            rot *= Quaternion.Euler((Mathf.PerlinNoise(t, 1) - .5f) * shake, (Mathf.PerlinNoise(2, t) - .5f) * shake, (Mathf.PerlinNoise(t, 3) - .5f) * shake * .5f);
        }
        poseEye = eye; poseRot = rot; posed = true;
        ApplyPose();
    }

    void ApplyPose()
    {
        player.transform.SetPositionAndRotation(poseBody, poseBodyRot);
        cam.transform.SetPositionAndRotation(poseEye, poseRot);
    }

    void LateUpdate() { if (busy && posed) ApplyPose(); }

    /// <summary>A flashlight beam on the camera (the game's own needs the item in hand).</summary>
    void Torch(bool on)
    {
        if (!torch)
        {
            torch = new GameObject("TrailerTorch").AddComponent<Light>();
            torch.type = LightType.Spot; torch.range = 16; torch.spotAngle = 58; torch.innerSpotAngle = 28;
            torch.intensity = 7; torch.color = new Color(1, .95f, .85f); torch.shadows = LightShadows.Soft;
        }
        torch.transform.SetParent(cam.transform, false);
        torch.transform.localPosition = new Vector3(.18f, -.12f, .1f);
        torch.transform.localRotation = Quaternion.Euler(1.5f, -2, 0);
        torch.enabled = on;
    }

    static float Smooth(float t) => t * t * (3 - 2 * t);

    /// <summary>Films <paramref name="seconds"/> of game time, calling step(t 0..1, seconds in) before each frame.</summary>
    IEnumerator Film(float seconds, Action<float, float> step)
    {
        int count = Mathf.RoundToInt(seconds * Fps);
        for (int i = 0; i < count; i++)
        {
            if (calmPulse) session.pulse = 72;
            step?.Invoke(count > 1 ? i / (float)(count - 1) : 0, i / (float)Fps);
            yield return new WaitForEndOfFrame();
            Capture();
        }
    }

    void Capture()
    {
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        grab.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
        RenderTexture.active = previous;
        File.WriteAllBytes(Path.Combine(shotDir, frame.ToString("0000") + ".jpg"), grab.EncodeToJPG(93));
        frame++;
    }

    // ------------------------------------------------------------------ shots

    IEnumerator Shot(string name)
    {
        switch (name)
        {
            case "house": return House();
            case "kitchen": return Kitchen();
            case "stormwin": return StormWindow();
            case "board": return Board();
            case "glow": return Glow();
            case "tools": return Tools();
            case "shatter": return Shatter();
            case "door": return DoorBreak();
            case "blackout": return Blackout();
            case "flood": return Flood();
            case "zap": return Zap();
            case "heart": return Heart();
            case "ending": return Ending();
        }
        Debug.LogError("Unknown trailer shot " + name);
        return null;
    }

    // Day 1, calm morning: the living room, slow dolly.
    IEnumerator House()
    {
        Day(1);
        yield return Film(3.8f, (t, s) => Pose(Vector3.Lerp(new Vector3(-4.8f, 1.55f, -0.9f), new Vector3(-4.1f, 1.5f, -1.5f), Smooth(t)),
            Vector3.Lerp(new Vector3(-0.6f, 1.15f, -3.9f), new Vector3(-0.8f, 1.15f, -5.4f), Smooth(t))));
    }

    IEnumerator Kitchen()
    {
        Day(1);
        yield return Film(3.2f, (t, s) => Pose(Vector3.Lerp(new Vector3(-2.2f, 1.55f, -4.4f), new Vector3(-2.0f, 1.5f, -5.5f), Smooth(t)), new Vector3(-1.4f, 1.3f, -9.6f)));
    }

    // Day 3 storm through the living room windows, lightning.
    IEnumerator StormWindow()
    {
        Day(3, DayPhase.Disaster);
        yield return Film(3.4f, (t, s) =>
        {
            Storm(1, 1);
            if (Mathf.Abs(s - .5f) < .01f) WindowWeather.Flash(1);
            if (Mathf.Abs(s - 2.2f) < .01f) WindowWeather.Flash(.8f);
            Pose(Vector3.Lerp(new Vector3(-3.4f, 1.5f, -2.8f), new Vector3(-3.0f, 1.5f, -2.5f), t), new Vector3(-0.6f, 1.55f, -0.9f));
        });
    }

    // Day 1: boarding up the living room windows.
    IEnumerator Board()
    {
        Day(1);
        var w1 = Opening("W01"); var w2 = Opening("W02");
        bool done1 = false, done2 = false;
        yield return Film(4.8f, (t, s) =>
        {
            if (!done1 && s >= 1.1f) { done1 = true; w1.SetState(OpeningState.Reinforced); }
            if (!done2 && s >= 3.4f) { done2 = true; w2.SetState(OpeningState.Reinforced); }
            float pan = Smooth(Mathf.InverseLerp(1.9f, 2.9f, s));
            var eye = Vector3.Lerp(new Vector3(-2.3f, 1.62f, -2.3f), new Vector3(-1.25f, 1.78f, -2.5f), pan);
            Pose(eye, Vector3.Lerp(new Vector3(-2.3f, 1.5f, 0), new Vector3(0, 1.55f, -2.6f), pan), null, s > 1.1f && s < 1.4f || s > 3.4f && s < 3.7f ? 1.2f : 0);
        });
    }

    /// <summary>A clean task list a couple of frames into the day: listed tasks done, the rest open, windows unboarded, no toasts.</summary>
    IEnumerator SkipTasks(params string[] ids)
    {
        yield return null; yield return null;
        foreach (var o in FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude)) if (o.InPlay) o.SetState(OpeningState.Unreinforced);
        var tasks = session.GetComponent<TaskEngine>();
        foreach (var task in tasks.Today) task.status = ids.Contains(task.definition.id) ? TaskStatus.Done : TaskStatus.Open;
        typeof(TaskEngine).GetField("assigned", Any).SetValue(tasks, null);
        var ui = FindAnyObjectByType<GameUI>();
        var toasts = (List<KeyValuePair<CanvasGroup, float>>)typeof(GameUI).GetField("toasts", Any).GetValue(ui);
        foreach (var toast in toasts) if (toast.Key) Destroy(toast.Key.gameObject);
        toasts.Clear();
        yield return null; yield return null;
    }

    // Day 1 with the HUD: the task card and the windows glowing through the wall.
    IEnumerator Glow()
    {
        Day(1);
        yield return SkipTasks("d1_handbook", "d1_alert", "d1_flashlight", "d1_tools");
        Hud(true);
        yield return Film(3.6f, (t, s) => Pose(Vector3.Lerp(new Vector3(-4.9f, 1.6f, -4.4f), new Vector3(-4.4f, 1.6f, -3.8f), Smooth(t)), new Vector3(-1.4f, 1.35f, -0.6f)));
    }

    // Day 1: the toolkit and planks glowing on the basement workbench.
    IEnumerator Tools()
    {
        Day(1);
        yield return SkipTasks("d1_handbook", "d1_alert", "d1_flashlight");
        yield return Film(2.8f, (t, s) => Pose(Vector3.Lerp(new Vector3(-3.0f, -1.5f, -2.9f), new Vector3(-3.3f, -1.6f, -2.3f), Smooth(t)), new Vector3(-3.45f, -2.35f, -0.5f)));
    }

    // Day 3: a window shakes, then shatters.
    IEnumerator Shatter()
    {
        Day(3, DayPhase.Disaster);
        var w2 = Opening("W02");
        w2.Strike(OpeningState.Broken);
        float brokeAt = -1;
        yield return Film(5.2f, (t, s) =>
        {
            Storm(1, 1);
            if (brokeAt < 0 && w2.State == OpeningState.Broken) { brokeAt = s; WindowWeather.Flash(1); }
            float kick = brokeAt < 0 ? 0 : Mathf.Exp(-(s - brokeAt) * 4) * 5;
            Pose(new Vector3(-2.9f, 1.5f, -2.3f) + (brokeAt < 0 ? Vector3.zero : Vector3.left * .25f * Smooth(Mathf.Clamp01((s - brokeAt) * 2))), new Vector3(0, 1.6f, -2.6f), null, kick);
        });
    }

    // Day 3: the front door gives in.
    IEnumerator DoorBreak()
    {
        Day(3, DayPhase.Disaster);
        var door = Opening("Door");
        door.Strike(OpeningState.Broken);
        float brokeAt = -1;
        yield return Film(5f, (t, s) =>
        {
            Storm(1, 1);
            if (brokeAt < 0 && door.State == OpeningState.Broken) brokeAt = s;
            float kick = brokeAt < 0 ? .3f : Mathf.Exp(-(s - brokeAt) * 4) * 6;
            Pose(Vector3.Lerp(new Vector3(-7.2f, 1.55f, -3.2f), new Vector3(-7.1f, 1.55f, -2.7f), t), new Vector3(-7f, 1.25f, 0), null, kick);
        });
    }

    // Day 2 blackout: the flashlight beam sweeping the dark living room.
    IEnumerator Blackout()
    {
        Day(2, DayPhase.Disaster);
        Torch(true);
        yield return Film(4.6f, (t, s) =>
        {
            Storm(1, 0);
            var look = Vector3.Lerp(new Vector3(-1.0f, 1.0f, -9.6f), new Vector3(-6.5f, 1.0f, -9.0f), Smooth(t));
            Pose(Vector3.Lerp(new Vector3(-2.6f, 1.6f, -5.2f), new Vector3(-3.0f, 1.6f, -5.6f), t), look, null, .7f);
        });
        Torch(false);
    }

    void Depth(float depth)
    {
        var flood = session.GetComponent<FloodSystem>();
        typeof(FloodSystem).GetMethod("SetDepth", Any).Invoke(flood, new object[] { depth });
    }

    // Day 3: the basement floods, and the water is live.
    IEnumerator Flood()
    {
        Day(3, DayPhase.Disaster);
        var flood = session.GetComponent<FloodSystem>();
        flood.Breaker.SetOff(false);
        yield return Film(4.6f, (t, s) =>
        {
            Storm(1, 1);
            Depth(Mathf.Lerp(.12f, .3f, t));
            Pose(Vector3.Lerp(new Vector3(-10.1f, -1.0f, -4.4f), new Vector3(-9.9f, -1.75f, -3.3f), Smooth(t)), new Vector3(-6.6f, -2.95f, -0.8f));
        });
    }

    IEnumerator Zap()
    {
        Day(3, DayPhase.Disaster);
        var flood = session.GetComponent<FloodSystem>();
        flood.Breaker.SetOff(false);
        Depth(.3f);
        var body = new Vector3(-8.6f, -2.8f, -2.2f);
        yield return Film(2.6f, (t, s) =>
        {
            Storm(1, 1);
            foreach (float at in new[] { .25f, .85f, 1.45f, 2.05f }) if (Mathf.Abs(s - at) < .01f) Call(flood, "Zap");
            float since = Mathf.Repeat(s - .25f, .6f);
            Pose(Vector3.Lerp(new Vector3(-6.8f, -1.9f, -3.6f), new Vector3(-7.1f, -2.0f, -3.3f), t), new Vector3(-8.6f, -2.85f, -2.2f), body, s > .25f && since < .3f ? 1.5f : 0);
        });
    }

    // Day 3 with the HUD: the heart hits 180 and Cane freezes.
    IEnumerator Heart()
    {
        Day(3, DayPhase.Disaster);
        calmPulse = false;
        yield return Film(3.9f, (t, s) =>
        {
            Storm(1, 1);
            session.pulse = Mathf.Lerp(150, 180, Mathf.Clamp01(s / .5f));
            if (Mathf.Abs(s - 1.6f) < .01f) WindowWeather.Flash(1);
            Pose(new Vector3(-4.4f, 1.6f, -4.3f), new Vector3(-2.3f, 1.45f, -.3f), null, .8f + s * .4f);
        });
        session.pulse = 72;
    }

    // The ending cutscene as it plays (camera driven by CutsceneSystem).
    IEnumerator Ending()
    {
        Day(3, DayPhase.AfterDisaster);
        calmPulse = true;
        // Windows left unboarded so the view of the ruined street is clear.
        Call(director, "DebugNormalEnding");
        cam.targetTexture = rt;
        yield return Film(23.5f, null);
    }
}
#endif
