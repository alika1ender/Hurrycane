using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The world signs are meshes whose letters are modelled (a separate submesh), so Loc cannot translate them. In Turkish the
/// letter submeshes are painted over with the board's own material and real text is drawn on top; in English everything is
/// restored. Created automatically after a scene loads (same pattern as GameplaySetup); the layout of each sign is in Signs.
/// Positions are in the sign's own frame: x across (metres), y up, measured from the centre of the board's face.
/// </summary>
public sealed class LocalizedSign : MonoBehaviour
{
    sealed class Line
    {
        public string english; public float x, y, size; public Color color;
        public Line(string english, float x, float y, float size, Color color) { this.english = english; this.x = x; this.y = y; this.size = size; this.color = color; }
    }

    sealed class Layout
    {
        public string visual;          // object name of the sign mesh
        public int[] letterSlots;      // material slots that are letters; they are painted over with the cover slot
        public int coverSlot;          // material slot of the board face behind the letters
        public Vector3 faceNormal;     // world direction the readable face points
        public float faceOffset;       // how far the text floats in front of the board's face
        public Line[] lines;
    }

    static readonly Color Cream = new Color(.93f, .89f, .78f);
    static readonly Color Yellow = new Color(.95f, .72f, .13f);
    static readonly Color Ink = new Color(.12f, .14f, .15f);
    static readonly Color Teal = new Color(.13f, .33f, .33f);

    // Positions are measured from the board's centre, x to the viewer's right, y up.
    static readonly Layout[] Signs =
    {
        new Layout { visual = "Shelter_Instructions_Visual", letterSlots = new[] { 1 }, faceNormal = Vector3.back, faceOffset = .035f,
            lines = new[] {
                new Line("STORM SHELTER", 0f, .31f, .135f, Cream),
                new Line("BRING SUPPLIES HERE", 0f, .013f, .056f, Cream),
                new Line("WATER / FOOD / MEDICAL", 0f, -.045f, .056f, Cream),
                new Line("STAY AWAY FROM WINDOWS", 0f, -.11f, .056f, Cream) } },
        new Layout { visual = "Basement_ResourceSign_Visual", letterSlots = new[] { 1, 2 }, faceNormal = Vector3.back, faceOffset = .025f,
            lines = new[] {
                new Line("SUPPLY ROOM", 0f, .07f, .117f, Cream),
                new Line("WATER / FOOD / MEDICAL", 0f, -.1f, .048f, Yellow) } },
        new Layout { visual = "Basement_EntrySign_Visual", letterSlots = new[] { 1, 2 }, faceNormal = Vector3.back, faceOffset = .04f,
            lines = new[] {
                new Line("BASEMENT", 0f, -.105f, .16f, Cream),
                new Line("SUPPLY STORAGE  /  DOWN", 0f, -.295f, .052f, Yellow) } },
        new Layout { visual = "Hall_ShelterDirection_Visual", letterSlots = new[] { 1 }, faceNormal = Vector3.left, faceOffset = .03f,
            lines = new[] {
                new Line("SHELTER", 0f, .016f, .068f, Cream),
                new Line("BASEMENT >", 0f, -.051f, .068f, Cream) } },
        new Layout { visual = "Hall_EmergencyBoard_Visual", letterSlots = new[] { 3, 4 }, coverSlot = 2, faceNormal = Vector3.left, faceOffset = .03f,
            lines = new[] {
                new Line("STORM KIT", 0f, .186f, .067f, Teal),
                new Line("WATER + FOOD", 0f, .026f, .045f, Ink),
                new Line("RADIO + LIGHT", 0f, -.0125f, .045f, Ink),
                new Line("FIRST AID", 0f, -.05f, .045f, Ink) } },
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        foreach (var layout in Signs)
        {
            var go = GameObject.Find(layout.visual);
            if (go && !go.GetComponent<LocalizedSign>()) go.AddComponent<LocalizedSign>().Setup(layout);
        }
    }

    Layout layout;
    MeshRenderer meshRenderer;
    Material[] original, hidden;
    readonly List<TextMesh> texts = new List<TextMesh>();
    Transform textRoot;
    int shownVersion = -1;
    PowerSystem power;

    void Setup(Layout l)
    {
        layout = l;
        meshRenderer = GetComponent<MeshRenderer>();
        if (!meshRenderer) { Destroy(this); return; }
        original = meshRenderer.sharedMaterials;
        hidden = (Material[])original.Clone();
        foreach (int slot in l.letterSlots) if (slot < hidden.Length) hidden[slot] = original[Mathf.Min(l.coverSlot, original.Length - 1)];
    }


    void BuildText()
    {
        textRoot = new GameObject("TurkishText").transform;
        textRoot.SetParent(transform, false);
        var bounds = meshRenderer.bounds;
        var normal = layout.faceNormal;
        // A world-space frame for the text: facing out of the board, up stays up.
        textRoot.rotation = Quaternion.LookRotation(-normal, Vector3.up);
        textRoot.position = bounds.center + normal * (Mathf.Abs(Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z)))) + layout.faceOffset);
        foreach (var line in layout.lines)
        {
            var go = new GameObject(line.english);
            go.transform.SetParent(textRoot, false);
            go.transform.localPosition = new Vector3(line.x, line.y, 0);
            var text = go.AddComponent<TextMesh>();
            text.font = UiKit.SemiBold;
            text.fontSize = 100;
            text.characterSize = line.size / 10f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = line.color;
            go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            texts.Add(text);
        }
    }

    void Apply()
    {
        shownVersion = Loc.Version;
        bool tr = Loc.IsTurkish;
        if (tr && textRoot == null) BuildText(); // measures the bounds before the letters are removed
        meshRenderer.sharedMaterials = tr ? hidden : original; // (static batching ignores mesh swaps, materials still work)
        if (textRoot) textRoot.gameObject.SetActive(tr);
        if (!tr) return;
        for (int i = 0; i < texts.Count; i++) texts[i].text = Loc.T(layout.lines[i].english);
    }

    void LateUpdate()
    {
        if (shownVersion != Loc.Version) Apply();
        if (!Loc.IsTurkish) return;
        // The text is unlit, so dim it with the room: the signs would otherwise glow in the blackout.
        if (!power) power = FindAnyObjectByType<PowerSystem>();
        float light = power && power.PowerOut ? .22f : .8f;
        for (int i = 0; i < texts.Count; i++) { var c = layout.lines[i].color; texts[i].color = new Color(c.r * light, c.g * light, c.b * light, 1); }
    }
}
