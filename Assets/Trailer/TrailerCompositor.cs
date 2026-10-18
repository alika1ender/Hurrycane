#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor-only: cuts the shots filmed by TrailerRecorder into the trailer. Reads edit.json (clips in order: a filmed
/// shot, a picture from Resources, or black, each with optional title text, zoom, fades and a white flash) and
/// mix.wav (44.1 kHz stereo 16-bit, built by Tools/Trailer/trailer_mix.py; edit.json is kept there too), draws each frame with uGUI into a 1920x1080
/// RenderTexture and encodes H.264/AAC with Unity's MediaEncoder. Runs in Play mode (it needs the UI to render).
/// </summary>
public sealed class TrailerCompositor : MonoBehaviour
{
    [Serializable] public sealed class Clip
    {
        public string src = "black", text = "", sub = "", style = "", color = "white";
        public float from, dur = 1, zoomFrom = 1, zoomTo = 1, panX, panY, fadeIn, fadeOut, flash, darken, textY, exposure = 1;
        public bool still;       // no automatic push-in
        public bool blackBackdrop; // keep black behind a portrait picture (one with its own transparent background)
        public bool hold, steady; // steady: titles shown at full strength for the whole clip (titles held across cuts)
    }
    [Serializable] public sealed class Edit { public Clip[] clips = new Clip[0]; public string output = "trailer.mp4"; public int[] stills = new int[0]; }

    const int W = TrailerRecorder.Width, H = TrailerRecorder.Height, Fps = TrailerRecorder.Fps, Rate = 44100;

    public string status = "idle";
    public bool busy;

    Camera view;
    RenderTexture rt;
    Texture2D frameTex, outTex;
    RawImage picture;
    Material grade;
    Image darken, fade, flash, bar;
    Text title, sub, small;
    RectTransform titleRect;
    readonly Dictionary<string, Texture2D> images = new Dictionary<string, Texture2D>();
    readonly Dictionary<string, int> counts = new Dictionary<string, int>();
    // Background behind a portrait picture: the picture's own edge colour, so it doesn't sit in a visible box.
    readonly Dictionary<Clip, Color> backdrops = new Dictionary<Clip, Color>();

    public static TrailerCompositor Get()
    {
        var existing = FindAnyObjectByType<TrailerCompositor>();
        return existing ? existing : new GameObject("TrailerCompositor").AddComponent<TrailerCompositor>();
    }

    public void Compose(string editPath) { if (!busy) StartCoroutine(Run(editPath)); }

    IEnumerator Run(string editPath)
    {
        busy = true;
        status = "starting";
        var edit = JsonUtility.FromJson<Edit>(File.ReadAllText(editPath));
        string folder = Path.GetDirectoryName(editPath);
        float[] audio = ReadWav(Path.Combine(folder, "mix.wav"));
        Build();

        int total = 0; foreach (var c in edit.clips) total += Mathf.RoundToInt(c.dur * Fps);
        var stills = new HashSet<int>(edit.stills);
        string output = Path.Combine(folder, edit.output);
        if (File.Exists(output)) File.Delete(output);
        var video = new VideoTrackAttributes { frameRate = new MediaRational(Fps), width = W, height = H, includeAlpha = false, bitRateMode = UnityEditor.VideoBitrateMode.High };
        var sound = new AudioTrackAttributes { sampleRate = new MediaRational(Rate), channelCount = 2, language = "en" };
        int perFrame = Rate / Fps * 2;
        int done = 0;
        foreach (var clip in edit.clips)
        {
            if (!clip.src.StartsWith("image:") || clip.blackBackdrop) continue;
            int last = Mathf.RoundToInt(clip.dur * Fps) - 1;
            DrawFrame(clip, last, last + 1, folder);
            fade.color = flash.color = Color.clear;
            if (!(picture.texture && picture.texture.width < picture.texture.height)) continue;
            Canvas.ForceUpdateCanvases(); view.Render();
            var before = RenderTexture.active; RenderTexture.active = rt;
            outTex.ReadPixels(new Rect(0, 0, W, H), 0, 0, false); outTex.Apply(false);
            RenderTexture.active = before;
            var r = picture.rectTransform.rect;
            backdrops[clip] = outTex.GetPixel(Mathf.RoundToInt(W / 2f - r.width / 2f + 4), H / 2);
        }
        using (var encoder = new MediaEncoder(output, video, sound))
        using (var samples = new NativeArray<float>(perFrame, Allocator.Persistent))
        {
            foreach (var clip in edit.clips)
            {
                int frames = Mathf.RoundToInt(clip.dur * Fps);
                for (int i = 0; i < frames; i++)
                {
                    DrawFrame(clip, i, frames, folder);
                    Canvas.ForceUpdateCanvases();
                    view.Render();
                    var previous = RenderTexture.active;
                    RenderTexture.active = rt;
                    outTex.ReadPixels(new Rect(0, 0, W, H), 0, 0, false);
                    outTex.Apply(false);
                    RenderTexture.active = previous;
                    encoder.AddFrame(outTex);
                    var slice = samples;
                    for (int k = 0; k < perFrame; k++) { int at = done * perFrame + k; slice[k] = at < audio.Length ? audio[at] : 0; }
                    encoder.AddSamples(slice);
                    if (stills.Contains(done)) File.WriteAllBytes(Path.Combine(folder, "still_" + done.ToString("0000") + ".jpg"), outTex.EncodeToJPG(90));
                    done++;
                    if (done % 6 == 0) { status = "frame " + done + "/" + total; yield return null; }
                }
            }
        }
        status = "done " + output;
        busy = false;
    }

    // ------------------------------------------------------------------ one frame

    void DrawFrame(Clip clip, int i, int frames, string folder)
    {
        float s = i / (float)Fps, t = frames > 1 ? i / (float)(frames - 1) : 0;
        // Background: a filmed frame, a picture, or black.
        Texture tex = null;
        bool fit = false;
        if (clip.src.StartsWith("shot:"))
        {
            string name = clip.src.Substring(5);
            int index = Mathf.RoundToInt(clip.from * Fps) + (clip.hold ? 0 : i);
            index = Mathf.Clamp(index, 0, Count(folder, name) - 1);
            frameTex.LoadImage(File.ReadAllBytes(Path.Combine(folder, "shots", name, index.ToString("0000") + ".jpg")));
            tex = frameTex;
        }
        else if (clip.src.StartsWith("image:"))
        {
            string path = clip.src.Substring(6);
            if (!images.TryGetValue(path, out var image)) images[path] = image = Resources.Load<Texture2D>(path);
            tex = image;
            fit = image && image.width < image.height; // portrait (the phone): fit inside, centred
        }
        view.backgroundColor = backdrops.TryGetValue(clip, out var backdrop) ? backdrop : Color.black;
        picture.texture = tex;
        picture.color = tex ? Color.white : Color.black;
        float zoom = Mathf.Lerp(clip.zoomFrom, clip.zoomTo, Ease(t));
        // Every filmed shot slowly pushes in unless it says otherwise: keeps the picture moving.
        if (clip.src.StartsWith("shot:") && !clip.still && clip.zoomFrom == 1 && clip.zoomTo == 1) zoom = Mathf.Lerp(1, 1.08f, t);
        if (grade) grade.SetFloat("_Exposure", clip.exposure > 0 ? clip.exposure : 1);
        if (fit && tex)
        {
            float h = H * .92f * zoom, w = h * tex.width / tex.height;
            picture.rectTransform.anchorMin = picture.rectTransform.anchorMax = new Vector2(.5f, .5f);
            picture.rectTransform.sizeDelta = new Vector2(w, h);
            picture.rectTransform.anchoredPosition = new Vector2(0, clip.panY * H * (1 - Ease(Mathf.Clamp01(s / .6f))));
            picture.uvRect = new Rect(0, 0, 1, 1);
        }
        else
        {
            picture.rectTransform.anchorMin = Vector2.zero; picture.rectTransform.anchorMax = Vector2.one;
            picture.rectTransform.sizeDelta = Vector2.zero; picture.rectTransform.anchoredPosition = Vector2.zero;
            float size = 1 / Mathf.Max(1, zoom);
            picture.uvRect = new Rect((1 - size) * (.5f + clip.panX * t), (1 - size) * (.5f + clip.panY * t), size, size);
        }
        darken.color = new Color(0, 0, 0, clip.darken);

        // Fades to black and the white flash at the start of the clip.
        float black = 0;
        if (clip.fadeIn > 0) black = Mathf.Max(black, 1 - Mathf.Clamp01(s / clip.fadeIn));
        if (clip.fadeOut > 0) black = Mathf.Max(black, 1 - Mathf.Clamp01((clip.dur - s - 1f / Fps) / clip.fadeOut));
        fade.color = new Color(0, 0, 0, black);
        flash.color = new Color(1, 1, 1, clip.flash > 0 ? Mathf.Clamp01(1 - s / clip.flash) * .9f : 0);

        Titles(clip, s);
    }

    void Titles(Clip clip, float s)
    {
        title.text = clip.text; sub.text = clip.sub; small.text = "";
        bar.enabled = false;
        var color = Pick(clip.color);
        float inA = clip.steady ? 1 : Mathf.Clamp01((s - .12f) / .3f), outA = clip.steady ? 1 : Mathf.Clamp01((clip.dur - s) / .25f);
        float alpha = Mathf.Min(inA, outA);
        switch (clip.style)
        {
            case "card":     // one huge word on black, punched in
                Place(titleRect, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1800, 400), TextAnchor.MiddleCenter);
                title.fontSize = 230; title.font = UiKit.Bold;
                title.color = Alpha(color, Mathf.Clamp01(s / .05f) * outA);
                titleRect.localScale = Vector3.one * Mathf.Lerp(1.5f, 1, Ease(Mathf.Clamp01(s / .16f)));
                titleRect.anchoredPosition += Shake(s, 18);
                sub.text = "";
                break;
            case "lower":    // lower-third caption with an amber bar
                float baseY = clip.textY > 0 ? clip.textY : 205;
                Place(titleRect, new Vector2(0, 0), new Vector2(150, baseY), new Vector2(1600, 130), TextAnchor.MiddleLeft);
                title.fontSize = 96; title.font = UiKit.Bold; title.color = Alpha(color, alpha);
                titleRect.localScale = Vector3.one;
                titleRect.anchoredPosition += new Vector2((1 - Ease(inA)) * -60, 0);
                bar.enabled = alpha > 0; bar.color = Alpha(UiKit.Amber, alpha);
                bar.rectTransform.anchoredPosition = new Vector2(118, baseY - 30);
                bar.rectTransform.sizeDelta = new Vector2(12, 190 * Ease(inA));
                Place(sub.rectTransform, new Vector2(0, 0), new Vector2(152, baseY - 77), new Vector2(1600, 60), TextAnchor.MiddleLeft, sub);
                sub.color = Alpha(UiKit.TextColor, Mathf.Min(Mathf.Clamp01((s - .35f) / .3f), outA) * .92f);
                break;
            case "logo":     // HURRY CANE, then the tagline and the call to action
                Place(titleRect, new Vector2(.5f, .5f), new Vector2(0, 90), new Vector2(1800, 320), TextAnchor.MiddleCenter);
                title.fontSize = 290; title.font = UiKit.Bold;
                title.text = "HURRY<color=#F2A83B>CANE</color>";
                title.color = Alpha(Color.white, Mathf.Clamp01(s / .2f) * outA);
                titleRect.localScale = Vector3.one * Mathf.Lerp(1.12f, 1, Ease(Mathf.Clamp01(s / 1.2f)));
                Place(sub.rectTransform, new Vector2(.5f, .5f), new Vector2(0, -120), new Vector2(1800, 80), TextAnchor.MiddleCenter, sub);
                sub.fontSize = 64; sub.color = Alpha(UiKit.TextColor, Mathf.Min(Mathf.Clamp01((s - 1.1f) / .5f), outA));
                small.text = "PREPARE.   SURVIVE.   LEARN.";
                small.color = Alpha(UiKit.Amber, Mathf.Min(Mathf.Clamp01((s - 2.2f) / .5f), outA));
                break;
            default:         // "big": centred headline with an optional line under it
                Place(titleRect, new Vector2(.5f, .5f), new Vector2(0, 30), new Vector2(1800, 220), TextAnchor.MiddleCenter);
                title.fontSize = 150; title.font = UiKit.Bold;
                // Slams in: big, fast, with a short shake (held titles across cuts stay still).
                float slam = clip.steady ? 1 : Mathf.Clamp01((s - .04f) / .14f);
                title.color = Alpha(color, Mathf.Min(slam, outA));
                titleRect.localScale = Vector3.one * Mathf.Lerp(1.4f, 1, Ease(slam));
                if (!clip.steady) titleRect.anchoredPosition += Shake(s - .04f, 14);
                Place(sub.rectTransform, new Vector2(.5f, .5f), new Vector2(0, -95), new Vector2(1800, 70), TextAnchor.MiddleCenter, sub);
                sub.fontSize = 54; sub.color = Alpha(UiKit.TextColor, clip.steady ? 1 : Mathf.Min(Mathf.Clamp01((s - .4f) / .3f), outA));
                break;
        }
    }

    static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, TextAnchor align, Text text = null)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        if (anchor == Vector2.zero) rect.pivot = new Vector2(0, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var label = text ? text : rect.GetComponent<Text>();
        if (label) label.alignment = align;
    }

    static Vector2 Shake(float s, float amount)
    {
        float k = 1 - Ease(Mathf.Clamp01(s / .3f));
        return s < 0 ? Vector2.zero : new Vector2(Mathf.Sin(s * 93), Mathf.Cos(s * 71)) * amount * k;
    }

    static float Ease(float t) { t = Mathf.Clamp01(t); return 1 - (1 - t) * (1 - t) * (1 - t); }
    static Color Alpha(Color c, float a) { c.a = a; return c; }
    static Color Pick(string name) => name == "amber" ? UiKit.Amber : name == "red" ? new Color(.93f, .22f, .22f) : name == "blue" ? new Color(.45f, .72f, 1f) : Color.white;

    int Count(string folder, string name)
    {
        if (!counts.TryGetValue(name, out int n)) counts[name] = n = Directory.GetFiles(Path.Combine(folder, "shots", name), "*.jpg").Length;
        return n;
    }

    // ------------------------------------------------------------------ setup

    void Build()
    {
        rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        frameTex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        outTex = new Texture2D(W, H, TextureFormat.RGBA32, false);

        view = new GameObject("TrailerView").AddComponent<Camera>();
        view.transform.SetParent(transform, false);
        view.transform.position = new Vector3(0, -500, 0);
        view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = Color.black;
        view.cullingMask = 1 << 31; view.targetTexture = rt; view.enabled = false;
        view.orthographic = true; view.nearClipPlane = .01f; view.farClipPlane = 10;

        var canvasGo = new GameObject("TrailerCanvas");
        canvasGo.layer = 31;
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = view; canvas.planeDistance = 1;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(W, H);

        picture = Child<RawImage>(canvasGo.transform, "Picture");
        var shader = Shader.Find("Hidden/TrailerGrade");
        if (shader) picture.material = grade = new Material(shader);
        darken = Full<Image>(canvasGo.transform, "Darken");
        title = Child<Text>(canvasGo.transform, "Title");
        titleRect = title.rectTransform;
        sub = Child<Text>(canvasGo.transform, "Sub");
        small = Child<Text>(canvasGo.transform, "Small");
        Place(small.rectTransform, new Vector2(.5f, .5f), new Vector2(0, -215), new Vector2(1800, 70), TextAnchor.MiddleCenter, small);
        bar = Child<Image>(canvasGo.transform, "Bar");
        bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = Vector2.zero; bar.rectTransform.pivot = new Vector2(0, .5f);
        bar.rectTransform.anchoredPosition = new Vector2(118, 175);
        foreach (var t in new[] { title, sub, small })
        {
            t.font = UiKit.Bold; t.supportRichText = true; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = t.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(0, 0, 0, .85f); outline.effectDistance = new Vector2(3, -3);
            var shadow = t.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .55f); shadow.effectDistance = new Vector2(0, -8);
        }
        sub.font = UiKit.SemiBold; small.font = UiKit.SemiBold; small.fontSize = 46;
        flash = Full<Image>(canvasGo.transform, "Flash");
        fade = Full<Image>(canvasGo.transform, "Fade");
    }

    static T Child<T>(Transform parent, string name) where T : Graphic
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 31;
        go.transform.SetParent(parent, false);
        var g = go.AddComponent<T>(); g.raycastTarget = false;
        return g;
    }
    static T Full<T>(Transform parent, string name) where T : Graphic
    {
        var g = Child<T>(parent, name);
        g.rectTransform.anchorMin = Vector2.zero; g.rectTransform.anchorMax = Vector2.one; g.rectTransform.sizeDelta = Vector2.zero;
        g.color = Color.clear;
        return g;
    }

    /// <summary>Interleaved stereo floats from a 16-bit PCM WAV.</summary>
    static float[] ReadWav(string path)
    {
        if (!File.Exists(path)) return new float[0];
        var bytes = File.ReadAllBytes(path);
        int pos = 12;
        while (pos + 8 <= bytes.Length)
        {
            string id = System.Text.Encoding.ASCII.GetString(bytes, pos, 4);
            int size = BitConverter.ToInt32(bytes, pos + 4);
            if (id == "data")
            {
                int n = Mathf.Min(size, bytes.Length - pos - 8) / 2;
                var data = new float[n];
                for (int i = 0; i < n; i++) data[i] = BitConverter.ToInt16(bytes, pos + 8 + i * 2) / 32768f;
                return data;
            }
            pos += 8 + size + (size & 1);
        }
        return new float[0];
    }
}
#endif
