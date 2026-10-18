using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Shared look for the game UI ("emergency broadcast" style, Docs/Design/07): colours, Barlow Condensed fonts from
/// Resources/HurrycaneUI/Fonts, procedurally drawn rounded/ring sprites (no image assets needed) and small builders.
/// </summary>
public static class UiKit
{
    public static readonly Color PanelColor = new Color(.055f, .065f, .085f, .82f);
    public static readonly Color PanelLight = new Color(.13f, .145f, .175f, .95f);
    public static readonly Color TextColor = new Color(.96f, .96f, .94f);
    public static readonly Color Dim = new Color(.62f, .65f, .70f);
    public static readonly Color Amber = new Color(.95f, .66f, .23f);
    public static readonly Color Red = new Color(.90f, .28f, .30f);
    public static readonly Color Blue = new Color(.33f, .64f, .95f);
    public static readonly Color Green = new Color(.30f, .78f, .50f);

    static Font medium, semiBold, bold;
    static Sprite rounded, ring, disc;

    public static Font Medium => medium ? medium : medium = LoadFont("BarlowCondensed-Medium");
    public static Font SemiBold => semiBold ? semiBold : semiBold = LoadFont("BarlowCondensed-SemiBold");
    public static Font Bold => bold ? bold : bold = LoadFont("BarlowCondensed-Bold");
    /// <summary>9-sliced rounded rectangle.</summary>
    public static Sprite Rounded => rounded ? rounded : rounded = MakeRounded(64, 14);
    /// <summary>Thin ring, for radial hold progress.</summary>
    public static Sprite Ring => ring ? ring : ring = MakeRing(128, 9);
    public static Sprite Disc => disc ? disc : disc = MakeRing(32, 32);

    static Font LoadFont(string name)
    {
        var font = Resources.Load<Font>("HurrycaneUI/Fonts/" + name);
        return font ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public static Color PhaseColor(DayPhase phase) =>
        phase == DayPhase.Disaster ? Red : phase == DayPhase.AfterDisaster ? Blue : Amber;

    // ---- builders ----

    public static RectTransform Node(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; // UI
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    /// <summary>Anchors and pivots at the same point (e.g. (0.5,1) = top centre), then positions and sizes.</summary>
    public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Stretch(this RectTransform rt, float inset = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        return rt;
    }

    public static Image Panel(Transform parent, string name, Color color, bool rounded = true)
    {
        var image = Node(name, parent).gameObject.AddComponent<Image>();
        if (rounded) { image.sprite = Rounded; image.type = Image.Type.Sliced; }
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static Text Label(Transform parent, string name, Font font, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool wrap = false)
    {
        var text = Node(name, parent).gameObject.AddComponent<Text>();
        text.font = font; text.fontSize = size; text.color = color; text.alignment = align;
        text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = true;
        text.raycastTarget = false;
        text.gameObject.AddComponent<LocalizedLabel>(); // swaps in Turkish when chosen in Settings (see Loc)
        return text;
    }

    /// <summary>A rounded track with a fill child; set the value with SetFill.</summary>
    public static RectTransform Bar(Transform parent, string name, Color track, Color fill, out Image fillImage)
    {
        var back = Panel(parent, name, track);
        fillImage = Panel(back.transform, "Fill", fill);
        fillImage.rectTransform.Stretch();
        return back.rectTransform;
    }

    public static void SetFill(Image fill, float value)
    {
        var rt = fill.rectTransform;
        rt.anchorMax = new Vector2(Mathf.Clamp01(value), 1);
        fill.enabled = value > .001f;
    }

    public static Button Button(Transform parent, string name, string caption, UnityAction onClick, out Text label, int fontSize = 30)
    {
        var image = Panel(parent, name, Color.white);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = PanelLight;
        colors.highlightedColor = new Color(Amber.r * .55f, Amber.g * .55f, Amber.b * .55f, 1);
        colors.pressedColor = Amber;
        colors.selectedColor = PanelLight;
        colors.fadeDuration = .08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);
        label = Label(image.transform, "Label", SemiBold, fontSize, TextColor, TextAnchor.MiddleCenter, true);
        label.rectTransform.Stretch(10);
        label.text = caption;
        return button;
    }

    public static T Layout<T>(this Component component, int padding, float spacing, TextAnchor align) where T : HorizontalOrVerticalLayoutGroup
    {
        var group = component.gameObject.AddComponent<T>();
        group.padding = new RectOffset(padding, padding, padding, padding);
        group.spacing = spacing;
        group.childAlignment = align;
        group.childControlWidth = group.childControlHeight = true;
        group.childForceExpandWidth = typeof(T) == typeof(VerticalLayoutGroup);
        group.childForceExpandHeight = false;
        return group;
    }

    public static ContentSizeFitter Fit(this Component component, bool width, bool height)
    {
        var fitter = component.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = width ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = height ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        return fitter;
    }

    public static LayoutElement Size(this Component component, float width = -1, float height = -1, float flexibleWidth = -1)
    {
        var element = component.gameObject.GetComponent<LayoutElement>();
        if (!element) element = component.gameObject.AddComponent<LayoutElement>();
        if (width >= 0) { element.minWidth = width; element.preferredWidth = width; }
        if (height >= 0) { element.minHeight = height; element.preferredHeight = height; }
        if (flexibleWidth >= 0) element.flexibleWidth = flexibleWidth;
        return element;
    }

    // ---- procedural sprites ----

    static Sprite MakeRounded(int size, int radius)
    {
        return MakeSprite(size, radius + 2, (x, y) =>
        {
            float cx = Mathf.Clamp(x, radius, size - radius), cy = Mathf.Clamp(y, radius, size - radius);
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            return Mathf.Clamp01(radius - distance + .5f);
        });
    }

    static Sprite MakeRing(int size, float thickness)
    {
        float outer = size * .5f;
        return MakeSprite(size, 0, (x, y) =>
        {
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(outer, outer));
            float middle = outer - thickness * .5f;
            return Mathf.Clamp01(thickness * .5f - Mathf.Abs(distance - middle) + .5f);
        });
    }

    static Sprite MakeSprite(int size, int border, Func<float, float, float> alpha)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = new Color(1, 1, 1, alpha(x + .5f, y + .5f));
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }
}
