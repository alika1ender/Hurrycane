using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable] public sealed class CaptionLine { public float at, duration; public string speaker, text; }

[Serializable]
public sealed class CutsceneConfig
{
    public CaptionLine[] opening = new CaptionLine[0], ending = new CaptionLine[0];
    public static CutsceneConfig Load()
    {
        var file = Resources.Load<TextAsset>("HurrycaneData/cutscenes");
        return file ? JsonUtility.FromJson<CutsceneConfig>(file.text) : new CutsceneConfig();
    }
}

/// <summary>
/// First-person cutscenes made from camera moves, captions and screen effects (no character model):
/// the opening on the sofa (Docs/Design/01) and the ending walk to the living room window (08), plus the main menu shot.
/// Hold Space to skip (a single press once the opening has been seen). GameUI draws captions, eyelids, sirens and fades.
/// Captions live in Resources/HurrycaneData/cutscenes.json.
/// </summary>
[RequireComponent(typeof(DayDirector))]
public sealed class CutsceneSystem : MonoBehaviour
{
    const string SeenKey = "Hurrycane.SeenOpening";
    const float SkipHold = 1f;

    MapExplorer player;
    CutsceneConfig config;
    Action onDone;
    Coroutine running, captions;
    Vector3 homePosition; Quaternion homeRotation;
    Renderer tv; int tvSlot = -1; Material tvNews, tvMatch;
    GameObject street;
    Light sun;
    float skipHeld;

    public bool Playing { get; private set; }
    public string Kind { get; private set; } = "";
    public string Speaker { get; private set; } = "";
    public string Caption { get; private set; } = "";
    /// <summary>0 clear .. 1 black.</summary>
    public float Fade { get; private set; }
    /// <summary>0 eyes open .. 1 closed.</summary>
    public float Eyelids { get; private set; }
    /// <summary>0..1 red/blue siren wash.</summary>
    public float Siren { get; private set; }
    public float SkipProgress => QuickSkip ? 0 : Mathf.Clamp01(skipHeld / SkipHold);
    public bool QuickSkip => Kind == "opening" && PlayerPrefs.GetInt(SeenKey, 0) == 1;

    void Awake() => config = CutsceneConfig.Load();
    public void Bind(MapExplorer explorer)
    {
        player = explorer;
        tv = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude).FirstOrDefault(r => r.name == "Living_TV_Animimo_Visual");
        if (tv) tvSlot = Array.FindIndex(tv.sharedMaterials, m => m && m.name.Contains("TVStorm"));
        if (tvSlot >= 0)
        {
            tvNews = tv.sharedMaterials[tvSlot];
            tvMatch = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "TV_Match" };
            tvMatch.SetColor("_BaseColor", new Color(.1f, .35f, .12f));
            tvMatch.EnableKeyword("_EMISSION"); tvMatch.SetColor("_EmissionColor", new Color(.2f, .6f, .22f));
        }
    }

    // ------------------------------------------------------------------ public entry points

    /// <summary>Static view of the living room behind the main menu.</summary>
    public void MenuShot() => Shot(new Vector3(-4.8f, 1.55f, -0.9f), new Vector3(-0.6f, 1.15f, -3.9f));

    public void PlayOpening(Action done) => Play("opening", OpeningRoutine(), config.opening, done);
    public void PlayEnding(Action done) => Play("ending", EndingRoutine(), config.ending, done);

    void Play(string kind, IEnumerator routine, CaptionLine[] lines, Action done)
    {
        Stop();
        Kind = kind; onDone = done; Playing = true; skipHeld = 0;
        homePosition = player.transform.position; homeRotation = player.transform.rotation;
        player.enabled = false;
        running = StartCoroutine(Run(routine));
        captions = StartCoroutine(Captions(lines));
    }

    IEnumerator Run(IEnumerator routine)
    {
        yield return routine;
        Finish();
    }

    void Update()
    {
        if (!Playing) return;
        var keyboard = Keyboard.current;
        bool held = keyboard != null && keyboard.spaceKey.isPressed;
        if (QuickSkip) { if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) Finish(); return; }
        skipHeld = held ? skipHeld + Time.unscaledDeltaTime : 0;
        if (skipHeld >= SkipHold) Finish();
    }

    /// <summary>Ends the cutscene now (skip or natural end) and hands control back to the day.</summary>
    public void Finish()
    {
        if (!Playing) return;
        if (Kind == "opening")
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            SetTv(tvNews);
            // Gameplay starts where the day's save put Cane, not on the sofa.
            player.transform.SetPositionAndRotation(homePosition, homeRotation);
            player.viewCamera.transform.localRotation = Quaternion.identity;
        }
        Stop();
        var done = onDone; onDone = null;
        done?.Invoke();
    }

    void Stop()
    {
        if (running != null) StopCoroutine(running);
        if (captions != null) StopCoroutine(captions);
        running = captions = null;
        Playing = false; Caption = Speaker = ""; Eyelids = 0; Siren = 0; Fade = 0;
    }

    // ------------------------------------------------------------------ opening (Docs/Design/01)

    IEnumerator OpeningRoutine()
    {
        var seat = new Vector3(-1.05f, 1.2f, -2.72f);
        var tvCenter = new Vector3(-5.22f, 1.18f, -4.22f);
        SetTv(tvNews);
        Fade = 1;
        Shot(seat, tvCenter);
        yield return Tween(1.2f, t => Fade = 1 - t);
        yield return Wait(9.8f);                                      // the news
        yield return Tween(1.2f, t => Shot(seat, Vector3.Lerp(tvCenter, tvCenter + new Vector3(1.5f, -.4f, 2.2f), Smooth(t))));   // waves it off
        yield return Wait(1.6f);
        yield return Tween(.8f, t => Shot(seat, Vector3.Lerp(tvCenter + new Vector3(1.5f, -.4f, 2.2f), tvCenter, Smooth(t))));
        SetTv(tvMatch);                                               // ~14.6 s: the match
        yield return Wait(4f);
        // Eyes get heavy: two slow blinks, then the head drops and they close.
        yield return Blink(.9f); yield return Wait(1.2f); yield return Blink(1.3f);
        yield return Tween(2.4f, t => { Eyelids = Smooth(t); Shot(seat, tvCenter + Vector3.down * .9f * t); });
        Fade = 1; Eyelids = 0;                                        // ~24.3 s
        SetTv(tvNews);
        yield return Wait(3f);                                        // "The next morning"
        Shot(seat, tvCenter);
        Eyelids = 1; Fade = 0;
        // Sirens wake him; the TV now shows the warning.
        yield return Tween(3.6f, t => { Siren = .5f + .5f * Mathf.Sin(t * 3.6f * 6f); Eyelids = 1 - Smooth(Mathf.Clamp01(t * 1.6f)); });
        yield return Tween(3.2f, t => Siren = Mathf.Lerp(.6f, .15f, t) * (.5f + .5f * Mathf.Sin(t * 3.2f * 6f)));
        Siren = 0;
        yield return Tween(2.6f, t => Shot(seat, Vector3.Lerp(tvCenter, seat + new Vector3(-.3f, -.9f, .5f), Smooth(t)))); // looks down at the phone
        yield return Wait(.4f);
    }

    // ------------------------------------------------------------------ ending (Docs/Design/08)

    IEnumerator EndingRoutine()
    {
        BuildStreet();
        SetMorning(true);
        var power = GetComponent<PowerSystem>();
        if (power) power.SetDaylight();
        var window = FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude).FirstOrDefault(o => o.Id == "W01");
        if (window) window.SetGlassClear(true);
        Fade = 1;
        // At the foot of the basement stairs, looking up.
        Shot(new Vector3(-9.7f, -1.55f, -1.7f), new Vector3(-9.7f, 0.6f, -8.6f));
        yield return Tween(1.2f, t => Fade = 1 - t);
        yield return Wait(2.6f);
        yield return Tween(.7f, t => Fade = t);
        // Up top, then through the house to the living room window.
        var path = new[] { new Vector3(-8.0f, 1.6f, -8.7f), new Vector3(-7.8f, 1.6f, -4.0f), new Vector3(-7.6f, 1.6f, -1.8f), new Vector3(-5.0f, 1.6f, -1.7f), new Vector3(-2.3f, 1.55f, -1.15f) };
        Shot(path[0], path[1]);
        yield return Tween(.7f, t => Fade = 1 - t);
        float length = 0; for (int i = 1; i < path.Length; i++) length += Vector3.Distance(path[i - 1], path[i]);
        yield return Tween(8.6f, t =>
        {
            float d = Smooth(t) * length;
            for (int i = 1; i < path.Length; i++)
            {
                float leg = Vector3.Distance(path[i - 1], path[i]);
                if (d <= leg || i == path.Length - 1)
                {
                    var p = Vector3.Lerp(path[i - 1], path[i], Mathf.Clamp01(d / leg));
                    var ahead = i + 1 < path.Length ? Vector3.Lerp(path[i], path[i + 1], Mathf.Clamp01(d / leg)) : new Vector3(-2.3f, 1.4f, 2f);
                    Shot(p, Vector3.Lerp(ahead, new Vector3(-2.3f, 1.4f, 2f), t * t));
                    break;
                }
                d -= leg;
            }
        });
        // At the window: the street outside.
        yield return Tween(5.2f, t => Shot(path[path.Length - 1] + new Vector3(0, 0, .25f * Smooth(t)), new Vector3(-2.3f + Mathf.Sin(t * 2.2f) * .8f, 1.3f, 4f)));
        yield return Wait(2.6f);
        yield return Tween(1.2f, t => Fade = t);
    }

    /// <summary>Placeholder ruined street south of the house (seen through W01): mud, standing water, a fallen tree, debris
    /// and roofless houses. Swap for real art.</summary>
    void BuildStreet()
    {
        if (street) return;
        street = new GameObject("EndingStreet");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(street, gameObject.scene);
        var mud = Mat(new Color(.30f, .27f, .21f), .1f); var water = Mat(new Color(.18f, .22f, .24f), .9f);
        var bark = Mat(new Color(.27f, .19f, .13f), .1f); var leaves = Mat(new Color(.16f, .26f, .13f), .05f);
        var wall = Mat(new Color(.62f, .58f, .52f), .1f); var roof = Mat(new Color(.30f, .24f, .22f), .1f); var wood = Mat(new Color(.48f, .36f, .24f), .1f);
        Block(PrimitiveType.Cube, mud, new Vector3(-6, -.06f, 20), new Vector3(60, .1f, 40), Vector3.zero);
        Block(PrimitiveType.Cube, water, new Vector3(-3, -.005f, 8), new Vector3(7, .02f, 3.5f), new Vector3(0, 12, 0));
        Block(PrimitiveType.Cube, water, new Vector3(3, -.005f, 14), new Vector3(9, .02f, 4), new Vector3(0, -8, 0));
        // Fallen tree across the yard.
        Block(PrimitiveType.Cylinder, bark, new Vector3(-1.5f, .35f, 6.5f), new Vector3(.55f, 3.4f, .55f), new Vector3(0, 30, 84));
        foreach (var o in new[] { new Vector3(1.6f, .9f, 8.3f), new Vector3(2.4f, 1.3f, 9.0f), new Vector3(1.0f, 1.4f, 9.4f) })
            Block(PrimitiveType.Sphere, leaves, o, new Vector3(2.2f, 1.6f, 2.0f), Vector3.zero);
        // Broken fence and scattered boards.
        for (int i = 0; i < 9; i++) Block(PrimitiveType.Cube, wood, new Vector3(-9 + i * 1.6f, .45f, 11.5f), new Vector3(.12f, .9f, .12f), new Vector3(i % 3 == 0 ? 35 : 0, 0, i % 2 == 0 ? -20 : 8));
        for (int i = 0; i < 7; i++) Block(PrimitiveType.Cube, wood, new Vector3(-6 + i * 1.9f, .05f, 5 + (i % 3) * 1.7f), new Vector3(1.4f, .05f, .2f), new Vector3(0, i * 47, i % 2 * 9));
        // Houses across the street, roofs torn off.
        for (int i = 0; i < 4; i++)
        {
            var x = -14 + i * 7.5f;
            Block(PrimitiveType.Cube, wall, new Vector3(x, 2, 24), new Vector3(6, 4, 6), Vector3.zero);
            Block(PrimitiveType.Cube, roof, new Vector3(x + (i % 2 == 0 ? 1.5f : -1), 4.3f, 23), new Vector3(4, .25f, 6.2f), new Vector3(i % 2 == 0 ? 0 : 6, 0, i % 2 == 0 ? 24 : -32));
        }
        // Morning sun from the south-east, so the street is lit (no shadows: cheap, and no light through walls).
        sun = new GameObject("MorningSun").AddComponent<Light>();
        sun.transform.SetParent(street.transform, false);
        sun.type = LightType.Directional; sun.color = new Color(1f, .93f, .82f); sun.intensity = 1.1f; sun.shadows = LightShadows.None;
        sun.transform.rotation = Quaternion.Euler(32, 25, 0); // shining south, onto the faces seen from the house
    }

    void SetMorning(bool on)
    {
        if (player && on)
        {
            player.viewCamera.clearFlags = CameraClearFlags.SolidColor;
            player.viewCamera.backgroundColor = new Color(.72f, .76f, .80f); // pale overcast sky
        }
    }

    GameObject Block(PrimitiveType type, Material material, Vector3 position, Vector3 scale, Vector3 euler)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(street.transform, false);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    static Material Mat(Color color, float smoothness)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    // ------------------------------------------------------------------ helpers

    void SetTv(Material screen)
    {
        if (!tv || tvSlot < 0 || !screen) return;
        var slots = tv.sharedMaterials; slots[tvSlot] = screen; tv.sharedMaterials = slots;
    }

    /// <summary>Puts the first-person camera at an eye point looking at a target (moves the player; it's disabled meanwhile).</summary>
    void Shot(Vector3 eye, Vector3 target)
    {
        var look = target - eye;
        var flat = new Vector3(look.x, 0, look.z);
        if (flat.sqrMagnitude < 1e-6f) flat = player.transform.forward;
        player.transform.SetPositionAndRotation(eye - Vector3.up * player.viewCamera.transform.localPosition.y, Quaternion.LookRotation(flat));
        float pitch = -Mathf.Atan2(look.y, flat.magnitude) * Mathf.Rad2Deg;
        player.viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }

    IEnumerator Captions(CaptionLine[] lines)
    {
        float start = Time.time;
        foreach (var line in lines.OrderBy(l => l.at))
        {
            while (Time.time - start < line.at) yield return null;
            Speaker = line.speaker ?? ""; Caption = line.text;
            float end = line.at + line.duration;
            while (Time.time - start < end) yield return null;
            Caption = Speaker = "";
        }
    }

    static IEnumerator Wait(float seconds) { for (float t = 0; t < seconds; t += Time.deltaTime) yield return null; }

    static IEnumerator Tween(float seconds, Action<float> step)
    {
        for (float t = 0; t < seconds; t += Time.deltaTime) { step(t / seconds); yield return null; }
        step(1);
    }

    IEnumerator Blink(float seconds) => Tween(seconds, t => Eyelids = Mathf.Sin(t * Mathf.PI) * .92f);
    static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
}
