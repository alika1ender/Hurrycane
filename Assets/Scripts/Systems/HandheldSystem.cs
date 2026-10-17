using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public sealed class PhoneMessage { public int day; public string trigger, text; }

[Serializable]
public sealed class PhoneConfig
{
    public float drainPerSecond = .1f, openDrainPerSecond = 1, pulseDropPerText = 3;
    public PhoneMessage[] messages = new PhoneMessage[0];

    public static PhoneConfig Load()
    {
        var file = Resources.Load<TextAsset>("HurrycaneData/phone");
        return file ? JsonUtility.FromJson<PhoneConfig>(file.text) : new PhoneConfig();
    }
}

/// <summary>
/// The in-hand phone and handbook (Docs/Design/07). Tab raises the handbook if carried, otherwise the phone; while raised
/// Cane can walk but not use things. The phone battery drains slowly (fast while raised) and dies early on Day 2; Broski's
/// one-way texts arrive on triggers and calm the pulse a little. GameUI draws the screens; this owns the state and props.
/// </summary>
[RequireComponent(typeof(DayDirector))]
public sealed class HandheldSystem : MonoBehaviour
{
    public enum View { None, Phone, Handbook }
    public static event Action<PhoneMessage> MessageReceived;

    readonly List<PhoneMessage> thread = new List<PhoneMessage>();
    readonly HashSet<string> boardedToday = new HashSet<string>();
    GameSession session;
    DayDirector director;
    MapExplorer player;
    PhoneConfig config;
    Transform phoneProp, bookProp;
    float raise;

    public View Open { get; private set; }
    public IReadOnlyList<PhoneMessage> Thread => thread;
    public int Unread { get; private set; }
    public bool PhoneDead => session.phoneBattery <= 0;
    public bool HasHandbook => player && player.Inventory.Has(ItemKind.Handbook);
    /// <summary>What Tab opens: the handbook once carried, otherwise the phone.</summary>
    public View Preferred => HasHandbook ? View.Handbook : View.Phone;

    void Awake() { session = GetComponent<GameSession>(); director = GetComponent<DayDirector>(); config = PhoneConfig.Load(); }
    void OnEnable()
    {
        GameSession.DayStarted += OnDayStarted;
        DayDirector.PhaseChanged += OnPhase;
        BreakableOpening.StateChanged += OnOpeningChanged;
        FloodSystem.FloodStarted += OnFlood;
    }
    void OnDisable()
    {
        GameSession.DayStarted -= OnDayStarted;
        DayDirector.PhaseChanged -= OnPhase;
        BreakableOpening.StateChanged -= OnOpeningChanged;
        FloodSystem.FloodStarted -= OnFlood;
    }

    public void Bind(MapExplorer explorer)
    {
        player = explorer;
        phoneProp = MakeProp("PhoneProp", new Vector3(.07f, .145f, .009f), new Color(.05f, .05f, .06f));
        var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        DestroyImmediate(screen.GetComponent<Collider>());
        screen.transform.SetParent(phoneProp, false);
        screen.transform.localPosition = new Vector3(0, 0, -.6f);
        screen.transform.localRotation = Quaternion.identity;
        screen.transform.localScale = new Vector3(.9f, .92f, 1);
        var glow = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        glow.SetColor("_BaseColor", new Color(.1f, .14f, .2f)); glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(.25f, .35f, .5f));
        screen.GetComponent<Renderer>().sharedMaterial = glow;
        bookProp = MakeProp("HandbookProp", new Vector3(.25f, .33f, .035f), new Color(.72f, .13f, .11f));
    }

    Transform MakeProp(string name, Vector3 size, Color color)
    {
        var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        DestroyImmediate(prop.GetComponent<Collider>());
        prop.name = name;
        prop.transform.SetParent(player.viewCamera.transform, false);
        prop.transform.localScale = size;
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .4f);
        prop.GetComponent<Renderer>().sharedMaterial = material;
        prop.gameObject.SetActive(false);
        return prop.transform;
    }

    // ---- messages ----

    void OnDayStarted(int day)
    {
        Close();
        boardedToday.Clear();
        thread.Clear(); Unread = 0;
        // Day 1's first text is shown on the alert screen already, so it arrives silently.
        Deliver(day, "dayStart", silent: day == 1);
    }
    void OnPhase(DayPhase phase) { if (phase == DayPhase.Night) Deliver(session.CurrentDay, "night", false); }
    void OnFlood() => Deliver(session.CurrentDay, "flood", false);
    void OnOpeningChanged(BreakableOpening opening, OpeningState before, OpeningState after)
    {
        if (after != OpeningState.Reinforced || director.Phase < DayPhase.Preparation || director.Phase > DayPhase.AfterDisaster) return;
        if (boardedToday.Count == 0) Deliver(session.CurrentDay, "boarded", false);
        boardedToday.Add(opening.Id);
    }

    void Deliver(int day, string trigger, bool silent)
    {
        if (PhoneDead) return; // a dead phone gets nothing
        foreach (var message in config.messages.Where(m => m.day == day && m.trigger == trigger))
        {
            thread.Add(message);
            if (silent) continue;
            Unread++;
            MessageReceived?.Invoke(message);
        }
    }

    public float PulseDropPerText => config.pulseDropPerText;

    // ---- raising / lowering ----

    public void Toggle()
    {
        if (Open != View.None) { Close(); return; }
        Open = Preferred;
        if (Open == View.Phone) Unread = 0;
    }
    public void Close() { Open = View.None; }

    void Update()
    {
        if (!player) return;
        bool active = director.Phase >= DayPhase.Preparation && director.Phase <= DayPhase.AfterDisaster;
        if (active)
        {
            float drain = config.drainPerSecond + (Open == View.Phone ? config.openDrainPerSecond : 0);
            session.phoneBattery = Mathf.Max(0, session.phoneBattery - drain * Time.deltaTime);
        }
        var keyboard = Keyboard.current;
        bool canUse = active && player.enabled && Cursor.lockState == CursorLockMode.Locked;
        if (canUse && keyboard != null && keyboard.tabKey.wasPressedThisFrame) Toggle();
        if (!canUse && Open != View.None && !player.enabled) Close();
        player.interactLocked = Open != View.None;

        // Raise the prop into view (or lower it away).
        raise = Mathf.MoveTowards(raise, Open == View.None ? 0 : 1, Time.unscaledDeltaTime * 6);
        float e = raise * raise * (3 - 2 * raise);
        Place(phoneProp, Open == View.Phone || (Open == View.None && raise > 0 && !HasHandbook), new Vector3(.17f, -.32f, .42f), new Vector3(.13f, -.11f, .32f), e, Quaternion.Euler(-12, -14, 4));
        Place(bookProp, Open == View.Handbook || (Open == View.None && raise > 0 && HasHandbook), new Vector3(0, -.45f, .5f), new Vector3(0, -.2f, .42f), e, Quaternion.Euler(-48, 0, 0));
    }

    static void Place(Transform prop, bool show, Vector3 down, Vector3 up, float t, Quaternion rotation)
    {
        if (!prop) return;
        prop.gameObject.SetActive(show && t > 0);
        prop.localPosition = Vector3.Lerp(down, up, t);
        prop.localRotation = rotation;
    }
}
