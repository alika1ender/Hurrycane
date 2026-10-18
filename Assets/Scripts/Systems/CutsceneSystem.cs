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
    public CaptionLine[] opening = new CaptionLine[0], ending = new CaptionLine[0], failed = new CaptionLine[0];
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
    TVBroadcast tv;
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
    /// <summary>0..1 red siren wash.</summary>
    public float Siren { get; private set; }
    public float SkipProgress => QuickSkip ? 0 : Mathf.Clamp01(skipHeld / SkipHold);
    public bool QuickSkip => Kind == "opening" && PlayerPrefs.GetInt(SeenKey, 0) == 1;

    void Awake() => config = CutsceneConfig.Load();
    public void Bind(MapExplorer explorer)
    {
        player = explorer;
        tv = FindAnyObjectByType<TVBroadcast>();
    }

    // ------------------------------------------------------------------ public entry points

    /// <summary>Static view of the living room behind the main menu.</summary>
    public void MenuShot() => Shot(new Vector3(-4.8f, 1.55f, -0.9f), new Vector3(-0.6f, 1.15f, -3.9f));

    public void PlayOpening(Action done) => Play("opening", OpeningRoutine(), config.opening, done);
    public void PlayEnding(Action done) => Play("ending", EndingRoutine(), config.ending, done);
    /// <summary>The losing ending (score below failedScoreBelow): a stagger through the wrecked, dark house, then black.</summary>
    public void PlayFailed(Action done) => Play("failed", FailedRoutine(), config.failed, done);

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
            SetTv(TVFrame.StormWatch);
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
        SetTv(TVFrame.Anchor);
        Fade = 1;
        Shot(seat, tvCenter);
        yield return Tween(1.2f, t => Fade = 1 - t);
        SetTv(TVFrame.StormWatch);
        yield return Wait(6.3f);                                      // forecast through 7.5 s
        SetTv(TVFrame.Prepare);
        yield return Wait(3.5f);                                      // preparation advice through 11 s
        yield return Tween(1.2f, t => Shot(seat, Vector3.Lerp(tvCenter, tvCenter + new Vector3(1.5f, -.4f, 2.2f), Smooth(t))));   // waves it off
        yield return Wait(1.6f);
        yield return Tween(.8f, t => Shot(seat, Vector3.Lerp(tvCenter + new Vector3(1.5f, -.4f, 2.2f), tvCenter, Smooth(t))));
        SetTv(TVFrame.Match);                                         // ~14.6 s: the match
        yield return Wait(4f);
        // Eyes get heavy: two slow blinks, then the head drops and they close.
        yield return Blink(.9f); yield return Wait(1.2f); yield return Blink(1.3f);
        yield return Tween(2.4f, t => { Eyelids = Smooth(t); Shot(seat, tvCenter + Vector3.down * .9f * t); });
        Fade = 1; Eyelids = 0;                                        // ~24.3 s
        SetTv(TVFrame.StormWatch);
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
        BuildDevastation(bark, leaves, wall, roof, wood, mud);
        // Morning sun from the south-east, so the street is lit (no shadows: cheap, and no light through walls).
        sun = new GameObject("MorningSun").AddComponent<Light>();
        sun.transform.SetParent(street.transform, false);
        sun.type = LightType.Directional; sun.color = new Color(1f, .93f, .82f); sun.intensity = 1.1f; sun.shadows = LightShadows.None;
        sun.transform.rotation = Quaternion.Euler(32, 25, 0); // shining south, onto the faces seen from the house
    }

    /// <summary>
    /// Fills the view from the living room window (camera near x -2.3, looking south) with storm damage: snapped, leaning
    /// and uprooted trees, a closer row of half-collapsed houses, debris piles, an overturned car and a leaning pole.
    /// </summary>
    void BuildDevastation(Material bark, Material leaves, Material wall, Material roof, Material wood, Material mud)
    {
        var dark = Mat(new Color(.07f, .07f, .08f), .1f);
        var deadLeaves = Mat(new Color(.25f, .26f, .12f), .05f);
        var rubble = Mat(new Color(.45f, .42f, .38f), .1f);
        var paint = Mat(new Color(.55f, .18f, .14f), .5f);
        var random = new System.Random(1803);
        float R(float min, float max) => min + (float)random.NextDouble() * (max - min);

        // Trees: (x, z, kind) 0 snapped, 1 leaning, 2 uprooted. Spread across the window's view, near to far.
        var trees = new (float x, float z, int kind)[]
        {
            (-6.5f, 7.5f, 0), (2.8f, 7f, 1), (-9.5f, 10f, 1), (5.5f, 10.5f, 2), (-4f, 12.5f, 2), (0.5f, 13f, 0),
            (7.5f, 13.5f, 1), (-11f, 14f, 0), (-1.5f, 19f, 1), (4f, 19.5f, 0), (-7.5f, 20f, 1), (9.5f, 21f, 0),
        };
        foreach (var (x, z, kind) in trees)
        {
            var tree = Group("BrokenTree", new Vector3(x, 0, z), R(0, 360));
            float girth = R(.32f, .5f);
            if (kind == 0) // snapped: a jagged stump, the crown lying beside it
            {
                float stump = R(1.2f, 2.4f);
                Part(tree, PrimitiveType.Cylinder, bark, new Vector3(0, stump * .5f, 0), new Vector3(girth, stump * .5f, girth), Vector3.zero);
                Part(tree, PrimitiveType.Cube, bark, new Vector3(.05f, stump + .12f, 0), new Vector3(girth * .5f, .45f, girth * .4f), new Vector3(0, 0, 18));
                float length = R(3.5f, 5f);
                Part(tree, PrimitiveType.Cylinder, bark, new Vector3(length * .5f + .3f, girth * .4f, .2f), new Vector3(girth * .8f, length * .5f, girth * .8f), new Vector3(0, 0, 86));
                Part(tree, PrimitiveType.Sphere, R(0, 1) > .5f ? leaves : deadLeaves, new Vector3(length + .4f, .8f, .3f), new Vector3(2.4f, 1.4f, 2f), Vector3.zero);
                Part(tree, PrimitiveType.Cylinder, bark, new Vector3(length * .6f, .7f, -.5f), new Vector3(.09f, .9f, .09f), new Vector3(40, 0, 60));
            }
            else if (kind == 1) // leaning hard, stripped bare, a few branches left
            {
                float height = R(4.5f, 6.5f), lean = R(22, 38);
                var trunk = Part(tree, PrimitiveType.Cylinder, bark, Vector3.zero, new Vector3(girth, height * .5f, girth), new Vector3(0, 0, lean));
                trunk.transform.localPosition = trunk.transform.localRotation * new Vector3(0, height * .5f, 0);
                var top = trunk.transform.localRotation * new Vector3(0, height, 0);
                for (int b = 0; b < 4; b++)
                    Part(tree, PrimitiveType.Cylinder, bark, top * R(.55f, .95f), new Vector3(.08f, R(.5f, 1f), .08f), new Vector3(R(-60, 60), R(0, 360), R(-70, 70)));
                if (R(0, 1) > .4f) Part(tree, PrimitiveType.Sphere, deadLeaves, top + new Vector3(0, .2f, 0), new Vector3(1.6f, 1f, 1.4f), Vector3.zero);
            }
            else // uprooted: trunk on the ground, the root plate torn up with it
            {
                float length = R(4.5f, 6f);
                Part(tree, PrimitiveType.Cylinder, bark, new Vector3(length * .5f, girth * .5f, 0), new Vector3(girth, length * .5f, girth), new Vector3(0, 0, 88));
                Part(tree, PrimitiveType.Sphere, mud, new Vector3(-.2f, 1f, 0), new Vector3(.6f, 2.2f, 2.4f), Vector3.zero);
                Part(tree, PrimitiveType.Sphere, leaves, new Vector3(length + .6f, 1f, 0), new Vector3(2.8f, 1.8f, 2.6f), Vector3.zero);
                Part(tree, PrimitiveType.Cylinder, bark, new Vector3(length * .7f, 1f, .6f), new Vector3(.1f, 1f, .1f), new Vector3(-35, 0, 30));
            }
        }

        // A closer row of houses: walls half down, roofs caved in, rubble in front.
        for (int i = 0; i < 4; i++)
        {
            var house = Group("RuinedHouse", new Vector3(-11 + i * 7f, 0, 16.5f), R(-6, 6));
            float w = 5.6f, h = 3.4f, d = 5f;
            Part(house, PrimitiveType.Cube, wall, new Vector3(0, h * .5f, d * .5f), new Vector3(w, h, .25f), Vector3.zero);                               // back wall
            Part(house, PrimitiveType.Cube, wall, new Vector3(-w * .5f, h * .35f, 0), new Vector3(.25f, h * .7f, d), Vector3.zero);                       // left wall, lower
            Part(house, PrimitiveType.Cube, wall, new Vector3(w * .5f + .2f, h * .3f, .3f), new Vector3(.25f, h * .6f, d * .8f), new Vector3(0, 0, -14));  // right wall, buckled
            Part(house, PrimitiveType.Cube, wall, new Vector3(-w * .3f, h * .45f, -d * .5f), new Vector3(w * .4f, h * .9f, .25f), Vector3.zero);           // front, left piece
            Part(house, PrimitiveType.Cube, wall, new Vector3(w * .32f, h * .22f, -d * .5f), new Vector3(w * .3f, h * .44f, .25f), new Vector3(0, 0, 6));  // front, broken stub
            Part(house, PrimitiveType.Cube, dark, new Vector3(-w * .3f, h * .55f, -d * .5f - .14f), new Vector3(.9f, .8f, .04f), Vector3.zero);              // empty window
            Part(house, PrimitiveType.Cube, roof, new Vector3(0, h * .55f, d * .1f), new Vector3(w * .95f, .22f, d * 1.05f), new Vector3(i % 2 == 0 ? 28 : 34, 0, i % 2 == 0 ? 8 : -10)); // caved-in roof
            if (i % 2 == 1) Part(house, PrimitiveType.Cube, roof, new Vector3(w * .35f, h + .2f, d * .3f), new Vector3(w * .4f, .2f, d * .5f), new Vector3(0, 0, -22)); // a hanging piece
            for (int r = 0; r < 9; r++)
                Part(house, PrimitiveType.Cube, r % 3 == 0 ? wood : rubble, new Vector3(R(-w * .5f, w * .5f), R(.1f, .5f), -d * .5f - R(.4f, 2f)),
                    new Vector3(R(.3f, 1.2f), R(.15f, .5f), R(.3f, .9f)), new Vector3(R(-25, 25), R(0, 180), R(-25, 25)));
        }

        // An overturned car in the street.
        var car = Group("OverturnedCar", new Vector3(-3.2f, 0, 10.8f), 74);
        Part(car, PrimitiveType.Cube, paint, new Vector3(0, .55f, 0), new Vector3(4.2f, .8f, 1.8f), new Vector3(0, 0, 180));
        Part(car, PrimitiveType.Cube, dark, new Vector3(.2f, .12f, 0), new Vector3(2.2f, .3f, 1.6f), Vector3.zero);
        foreach (var wx in new[] { -1.3f, 1.3f }) foreach (var wz in new[] { -.8f, .8f })
            Part(car, PrimitiveType.Cylinder, dark, new Vector3(wx, 1.05f, wz), new Vector3(.6f, .1f, .6f), new Vector3(90, 0, 0));

        // A leaning power pole with a drooping line.
        var pole = Group("LeaningPole", new Vector3(6.5f, 0, 9f), 0);
        Part(pole, PrimitiveType.Cylinder, bark, new Vector3(-1.2f, 3.2f, 0), new Vector3(.22f, 3.4f, .22f), new Vector3(0, 0, 24));
        Part(pole, PrimitiveType.Cube, bark, new Vector3(-2.6f, 6.2f, 0), new Vector3(1.6f, .12f, .12f), new Vector3(0, 0, 24));
        Part(pole, PrimitiveType.Cube, dark, new Vector3(-6f, 3.4f, 0), new Vector3(7f, .03f, .03f), new Vector3(0, 0, -28));

        // More boards, branches and roof sheets strewn across the yard and street.
        for (int i = 0; i < 26; i++)
            Block(PrimitiveType.Cube, i % 4 == 0 ? roof : i % 4 == 1 ? bark : wood, new Vector3(R(-12, 10), R(.03f, .2f), R(5, 15)),
                new Vector3(R(.6f, 2f), R(.04f, .12f), R(.12f, .5f)), new Vector3(R(-15, 15), R(0, 360), R(-15, 15)));
    }

    Transform Group(string name, Vector3 position, float yaw)
    {
        var group = new GameObject(name).transform;
        group.SetParent(street.transform, false);
        group.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        return group;
    }

    GameObject Part(Transform parent, PrimitiveType type, Material material, Vector3 localPosition, Vector3 scale, Vector3 euler)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    void SetMorning(bool on)
    {
        if (player && on)
        {
            player.viewCamera.clearFlags = CameraClearFlags.SolidColor;
            player.viewCamera.backgroundColor = new Color(.72f, .76f, .80f); // pale overcast sky
            // Morning haze: the wrecked trees and houses outside stay readable, the far street fades out.
            SaveFog();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = player.viewCamera.backgroundColor;
            RenderSettings.fogStartDistance = 4f;    // the rooms stay clear
            RenderSettings.fogEndDistance = 34f;     // nearby damage clear, the far row hazy
        }
    }

    (bool on, FogMode mode, Color color, float start, float end, float density)? savedFog;
    void SaveFog()
    {
        if (savedFog == null)
            savedFog = (RenderSettings.fog, RenderSettings.fogMode, RenderSettings.fogColor, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance, RenderSettings.fogDensity);
    }

    void OnDestroy()
    {
        // Leaving Play mode must not leave the scene foggy in the editor.
        if (savedFog is { } fog)
        {
            RenderSettings.fog = fog.on; RenderSettings.fogMode = fog.mode; RenderSettings.fogColor = fog.color;
            RenderSettings.fogStartDistance = fog.start; RenderSettings.fogEndDistance = fog.end; RenderSettings.fogDensity = fog.density;
        }
    }

    // ------------------------------------------------------------------ failed ending (score below failedScoreBelow, Docs/Design/08)

    IEnumerator FailedRoutine()
    {
        var director = GetComponent<DayDirector>();
        var door = FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude).FirstOrDefault(o => o.IsDoor);
        BuildStreet();
        SetGloom();
        FloodOutside(door);
        var flood = GetComponent<FloodSystem>();
        if (flood && director) flood.ShowDepth(director.Config.flood.maxDepth); // the basement stays under water
        Fade = 1;

        // 1. The living room's broken window: dark storm, rain blowing in.
        var windowEye = new Vector3(-2.3f, 1.5f, -1.5f); var windowLook = new Vector3(-2.3f, 1.25f, 2f);
        float clock = 0;
        yield return Tween(1.4f, t => { Fade = 1 - t; clock += Time.deltaTime; Unsteady(windowEye, windowLook, clock, 0); });
        yield return Tween(2.8f, t => { clock += Time.deltaTime; Unsteady(windowEye + new Vector3(0, 0, .15f * t), windowLook, clock, 0); });
        yield return Tween(.6f, t => { Fade = t; clock += Time.deltaTime; });

        // 2. The top of the basement stairs: the shelter is under water.
        var stairEye = new Vector3(-9.0f, 1.55f, -8.2f); var stairLook = new Vector3(-9.7f, -2.2f, -2.5f);
        yield return Tween(.6f, t => { Fade = 1 - t; clock += Time.deltaTime; Unsteady(stairEye, stairLook, clock, 0); });
        yield return Tween(3f, t => { clock += Time.deltaTime; Unsteady(stairEye + new Vector3(0, -.1f * t, .25f * t), stairLook, clock, 0); });
        yield return Tween(.6f, t => { Fade = t; clock += Time.deltaTime; });

        // 3. A stagger to the front door, with a stumble halfway.
        Vector3 outward = door ? Outward(door) : Vector3.forward;
        Vector3 doorCentre = door ? door.Center : new Vector3(-2.3f, 1.1f, 0);
        float floorY = door ? door.GetComponent<Collider>().bounds.min.y : 0;
        var stand = new Vector3(doorCentre.x, floorY + 1.5f, doorCentre.z) - outward * 1.7f;
        var outside = new Vector3(doorCentre.x, floorY + .9f, doorCentre.z) + outward * 8;
        var start = new Vector3(-5.0f, 1.5f, -1.9f);
        yield return Tween(.6f, t => { Fade = 1 - t; clock += Time.deltaTime; Unsteady(start, stand, clock, 0); });
        yield return Tween(5f, t =>
        {
            clock += Time.deltaTime;
            float stumble = Mathf.Exp(-Mathf.Pow((t - .55f) * 9, 2)); // a sharp lurch around 55%
            var eye = Vector3.Lerp(start, stand, Smooth(t)) + Vector3.down * .35f * stumble;
            Unsteady(eye, Vector3.Lerp(stand, outside, Smooth(t)) + Vector3.down * .8f * stumble, clock, 14 * stumble);
        });

        // 4. The front door is smashed in and the flood is seeping under it. (The door is part of the hall mesh, so it
        // can't swing open; the broken-door look from OpeningVisuals shows it gave way.)
        if (door && door.State != OpeningState.Broken) door.SetState(OpeningState.Broken);
        var doorFace = new Vector3(doorCentre.x, floorY + 1.1f, doorCentre.z);
        yield return Tween(4.2f, t => { clock += Time.deltaTime; Unsteady(stand + outward * .45f * Smooth(t), Vector3.Lerp(doorFace, doorFace + Vector3.down * .9f, Smooth(t)), clock, 0); });
        var lastLook = doorFace + Vector3.down * .9f; // where the eyes were last; the fall below tips away from here

        // 5. His legs give way: the view tips and sinks, eyes close, black.
        yield return Tween(3.4f, t =>
        {
            clock += Time.deltaTime;
            float fall = Smooth(t);
            Unsteady(stand + outward * .45f + Vector3.down * 1.05f * fall, lastLook + Vector3.down * 2.4f * fall, clock, 28 * fall);
            Eyelids = Mathf.Clamp01(t < .5f ? Mathf.Sin(t * Mathf.PI * 4) * .5f * t * 2 : (t - .5f) * 2.2f);
        });
        Eyelids = 1; Fade = 1;
        yield return Wait(.9f);
    }

    /// <summary>Handheld, unsteady first-person framing: small drifting sway plus an optional roll in degrees.</summary>
    void Unsteady(Vector3 eye, Vector3 target, float time, float roll)
    {
        var sway = new Vector3(Mathf.Sin(time * 1.3f) * .03f, Mathf.Sin(time * 2.1f) * .02f, 0);
        var drift = new Vector3(Mathf.Sin(time * .9f) * .12f, Mathf.Sin(time * 1.7f) * .05f, 0);
        Shot(eye + sway, target + drift);
        var e = player.viewCamera.transform.localEulerAngles;
        player.viewCamera.transform.localRotation = Quaternion.Euler(e.x, 0, roll + Mathf.Sin(time * 1.1f) * 1.5f);
    }

    /// <summary>Power still out, a cold dark haze inside and out: the opposite of the survival ending's morning.</summary>
    void SetGloom()
    {
        var power = GetComponent<PowerSystem>();
        if (power) power.SetPower(false);
        var gloom = new Color(.09f, .1f, .12f);
        player.viewCamera.clearFlags = CameraClearFlags.SolidColor;
        player.viewCamera.backgroundColor = gloom;
        SaveFog();
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = gloom;
        RenderSettings.fogDensity = .075f; // rooms readable, the street swallowed after ~15 m
        if (sun) sun.enabled = false;      // BuildStreet's morning sun
    }

    /// <summary>
    /// The street outside is under water, and it is seeping in under the front door: a muddy sheet across the street and
    /// a glossy wet spread on the entry hall floor.
    /// </summary>
    void FloodOutside(BreakableOpening door)
    {
        var outward = door ? Outward(door) : Vector3.forward;
        var c = door ? door.Center : new Vector3(-2.3f, 1.1f, 0);
        float floorY = door ? door.GetComponent<Collider>().bounds.min.y : 0;
        var muddy = Mat(new Color(.2f, .18f, .13f), .92f);
        var street = Block(PrimitiveType.Cube, muddy, new Vector3(c.x, floorY - .12f, c.z) + outward * 20.4f, new Vector3(60, .05f, 40), Vector3.zero);
        street.transform.rotation = Quaternion.LookRotation(outward);
        street.name = "FailedFloodWater";
        var seep = Block(PrimitiveType.Cube, muddy, new Vector3(c.x, floorY + .012f, c.z) - outward * .8f, new Vector3(1.5f, .006f, 1.5f), Vector3.zero);
        seep.transform.rotation = Quaternion.LookRotation(outward);
        seep.name = "FailedSeepWater";
    }

    /// <summary>Points out of the house through an opening (the house footprint centre is about (-6, -5)).</summary>
    static Vector3 Outward(BreakableOpening opening)
    {
        var b = opening.GetComponent<Collider>().bounds;
        bool thinX = b.size.x < b.size.z;
        return thinX ? new Vector3(-Mathf.Sign(-6 - b.center.x), 0, 0) : new Vector3(0, 0, -Mathf.Sign(-5 - b.center.z));
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

    void SetTv(TVFrame frame)
    {
        if (tv) tv.Show(frame);
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
