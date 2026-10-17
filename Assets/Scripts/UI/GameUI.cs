using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// The game's screen UI (Docs/Design/07), built in code with uGUI so the scene file stays untouched:
/// phase timer, health, crosshair + interaction prompt with hold ring, carried items, phone/handbook task panel,
/// toasts, phase banner, damage flash, the alert phone, night card and pause/restart menu.
/// </summary>
public sealed class GameUI : MonoBehaviour
{
    const string AlertBody = "Hurricane warning in effect for your area. Landfall expected this evening. Board up or secure windows and doors. " +
        "Gather water, food, medication and a flashlight. Charge your phone. Stay indoors.";

    MapExplorer explorer;
    PauseMenu pause;
    RectTransform hud;

    // HUD pieces
    Image timerPanel, timerFill, healthFill, damageFlash, holdRing, crosshair, heart;
    Text pulseValue, pulseZone, vitalHints, ringCaption;
    CanvasGroup faintGroup;
    GameObject gameOverOverlay, scoreOverlay;
    RectTransform gameOverCards, scoreRows;
    Text scoreTitle, scoreSub, scoreTotal, scoreRank, scoreLine, scoreFact;
    float heartPhase;
    GameObject phoneOverlay, phoneDeadScreen, handbookOverlay;
    RectTransform phoneThread, handbookList, groundMap, upperMap;
    Text phoneStatus, phoneTask, phoneHint, handbookSubtitle;
    string overlaySignature;
    float overlayRefresh;
    DayPhase shownPhase = (DayPhase)(-1);
    Text phaseLabel, timeLabel, healthValue, promptKey, promptText, promptMissing;
    RectTransform prompt, inventoryRow, taskPanel, toastColumn;
    CanvasGroup bannerGroup, nightGroup;
    Text bannerTitle, bannerSub, nightTitle, nightBody, nightFooter, alertFooter;
    GameObject alertOverlay, pauseOverlay;
    RectTransform pauseCards;
    Text pauseSubtitle;

    string inventorySignature, taskSignature;
    float bannerUntil, lastHealth = -1;
    readonly List<KeyValuePair<CanvasGroup, float>> toasts = new List<KeyValuePair<CanvasGroup, float>>();
    bool pauseWasOpen;

    public void Bind(MapExplorer player)
    {
        explorer = player;
        pause = player.GetComponent<PauseMenu>();
        MapExplorer.DrawDebugHud = false;
        Build();
    }

    void OnEnable()
    {
        TaskEngine.TaskCompleted += OnTaskDone;
        DayDirector.PhaseChanged += OnPhase;
        BreakableOpening.WarningStarted += OnWarning;
        BreakableOpening.HitLanded += OnHit;
        FloodSystem.FloodStarted += OnFlood;
        HandheldSystem.MessageReceived += OnText;
    }
    void OnDisable()
    {
        TaskEngine.TaskCompleted -= OnTaskDone;
        DayDirector.PhaseChanged -= OnPhase;
        BreakableOpening.WarningStarted -= OnWarning;
        BreakableOpening.HitLanded -= OnHit;
        FloodSystem.FloodStarted -= OnFlood;
        HandheldSystem.MessageReceived -= OnText;
    }
    void OnText(PhoneMessage message) => Toast("Broski:  " + message.text, UiKit.Amber);
    void OnFlood() => Toast("Water is pouring down the basement stairs!", UiKit.Red);
    void OnDestroy() => MapExplorer.DrawDebugHud = true;

    // ------------------------------------------------------------------ build

    void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();
        if (!FindAnyObjectByType<EventSystem>())
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        hud = UiKit.Node("HUD", transform).Stretch();
        BuildTimer();
        BuildHealth();
        BuildCrosshair();
        BuildInventory();
        taskPanel = UiKit.Node("Tasks", hud).Place(new Vector2(1, 1), new Vector2(-28, -24), new Vector2(400, 0));
        var hint = UiKit.Label(hud, "Controls", UiKit.Medium, 20, new Color(1, 1, 1, .45f), TextAnchor.LowerRight);
        hint.rectTransform.Place(new Vector2(1, 0), new Vector2(-28, 22), new Vector2(600, 30));
        hint.text = "E  use / hold      G  drop      Tab  phone / handbook      F  flashlight      Q  breathe      H  first aid      Esc  pause";

        toastColumn = UiKit.Node("Toasts", transform).Place(new Vector2(.5f, .5f), new Vector2(0, -175), new Vector2(900, 0));
        toastColumn.pivot = new Vector2(.5f, 1);
        toastColumn.Layout<VerticalLayoutGroup>(0, 8, TextAnchor.UpperCenter).childForceExpandWidth = false;

        BuildBanner();
        damageFlash = UiKit.Panel(transform, "DamageFlash", new Color(.75f, .05f, .05f, 0), false);
        damageFlash.rectTransform.Stretch();
        ringCaption = UiKit.Label(hud, "RingCaption", UiKit.SemiBold, 26, UiKit.TextColor, TextAnchor.MiddleCenter);
        ringCaption.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(0, 56), new Vector2(500, 34));
        ringCaption.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1, -1);
        BuildNight();
        var faint = UiKit.Panel(transform, "Faint", Color.black, false);
        faint.rectTransform.Stretch();
        faintGroup = faint.gameObject.AddComponent<CanvasGroup>(); faintGroup.alpha = 0; faintGroup.blocksRaycasts = false;
        var faintText = UiKit.Label(faint.transform, "Text", UiKit.Bold, 72, UiKit.TextColor, TextAnchor.MiddleCenter);
        faintText.rectTransform.Stretch(); faintText.text = "YOU PASSED OUT\n<size=32><color=#9AA0AA>Waking up by the bed... the clock kept running.</color></size>";
        BuildGameOver();
        BuildScore();
        BuildPhone();
        BuildHandbook();
        BuildAlert();
        BuildPause();
    }

    void BuildPhone()
    {
        var frame = UiKit.Panel(transform, "PhoneOverlay", new Color(.02f, .02f, .03f, 1));
        frame.rectTransform.Place(new Vector2(1, .5f), new Vector2(-120, 0), new Vector2(440, 740));
        phoneOverlay = frame.gameObject;
        var screen = UiKit.Panel(frame.transform, "Screen", new Color(.09f, .1f, .13f, 1));
        screen.rectTransform.Stretch(12);
        var column = UiKit.Node("Column", screen.transform).Stretch(16);
        column.Layout<VerticalLayoutGroup>(0, 12, TextAnchor.UpperCenter);
        phoneStatus = UiKit.Label(column, "Status", UiKit.SemiBold, 22, UiKit.Dim, TextAnchor.MiddleCenter);
        UiKit.Label(column, "From", UiKit.SemiBold, 22, UiKit.Amber).text = "BROSKI";
        phoneThread = UiKit.Node("Thread", column);
        phoneThread.Layout<VerticalLayoutGroup>(0, 8, TextAnchor.UpperLeft);
        var spacer = UiKit.Node("Spacer", column); spacer.Size(-1, -1).flexibleHeight = 1;
        var card = UiKit.Panel(column, "TaskCard", UiKit.PanelLight);
        card.Layout<VerticalLayoutGroup>(14, 4, TextAnchor.UpperLeft);
        UiKit.Label(card.transform, "Kicker", UiKit.SemiBold, 20, UiKit.Amber).text = "NEXT TASK";
        phoneTask = UiKit.Label(card.transform, "Task", UiKit.Bold, 28, UiKit.TextColor, TextAnchor.UpperLeft, true);
        phoneHint = UiKit.Label(card.transform, "Hint", UiKit.Medium, 22, UiKit.Dim, TextAnchor.UpperLeft, true);
        UiKit.Label(column, "Close", UiKit.Medium, 20, UiKit.Dim, TextAnchor.MiddleCenter).text = "Tab  put the phone away";
        var dead = UiKit.Panel(screen.transform, "Dead", Color.black);
        dead.rectTransform.Stretch();
        phoneDeadScreen = dead.gameObject;
        var deadText = UiKit.Label(dead.transform, "Text", UiKit.Bold, 36, UiKit.Dim, TextAnchor.MiddleCenter, true);
        deadText.rectTransform.Stretch(30);
        deadText.text = "BATTERY DEAD\n<size=24>Use the emergency handbook.\nRed book, living room coffee table.</size>";
        phoneOverlay.SetActive(false);
    }

    static readonly Color Paper = new Color(.93f, .89f, .80f), Ink = new Color(.16f, .15f, .14f), InkDim = new Color(.42f, .39f, .35f), BookRed = new Color(.62f, .12f, .10f);

    void BuildHandbook()
    {
        var book = UiKit.Panel(transform, "HandbookOverlay", BookRed);
        book.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(0, -10), new Vector2(1320, 760));
        handbookOverlay = book.gameObject;
        var left = UiKit.Panel(book.transform, "LeftPage", Paper);
        left.rectTransform.Place(new Vector2(0, .5f), new Vector2(14, 0), new Vector2(640, 732));
        var right = UiKit.Panel(book.transform, "RightPage", Paper);
        right.rectTransform.Place(new Vector2(1, .5f), new Vector2(-14, 0), new Vector2(640, 732));

        var lcol = UiKit.Node("Column", left.transform).Stretch(30);
        lcol.Layout<VerticalLayoutGroup>(0, 8, TextAnchor.UpperLeft);
        UiKit.Label(lcol, "Title", UiKit.Bold, 44, BookRed).text = "EMERGENCY HANDBOOK";
        handbookSubtitle = UiKit.Label(lcol, "Sub", UiKit.SemiBold, 24, InkDim);
        handbookList = UiKit.Node("List", lcol);
        handbookList.Layout<VerticalLayoutGroup>(0, 6, TextAnchor.UpperLeft);

        var rcol = UiKit.Node("Column", right.transform).Stretch(30);
        var mapTitle = UiKit.Label(rcol, "Title", UiKit.Bold, 44, BookRed);
        mapTitle.rectTransform.Place(new Vector2(0, 1), Vector2.zero, new Vector2(580, 50));
        mapTitle.text = "FLOOR PLAN";
        groundMap = MapFrame(rcol, "GROUND FLOOR", new Vector2(0, -80));
        upperMap = MapFrame(rcol, "UPPER FLOOR", new Vector2(300, -80));
        var legend = UiKit.Label(rcol, "Legend", UiKit.SemiBold, 22, Ink, TextAnchor.UpperLeft, true);
        legend.rectTransform.Place(new Vector2(0, 1), new Vector2(0, -400), new Vector2(580, 120));
        legend.text = "<color=#2E8B57>BOARDED</color>    <color=#D48A1E>CRACKED</color>    <color=#C0392B>BROKEN</color>    <color=#2F6DB5>TAPED</color>    <color=#8C8579>NOT SECURED</color>\nWhite dot = you. North is up. D = front door, numbers = windows.";
        var close = UiKit.Label(rcol, "Close", UiKit.Medium, 22, InkDim, TextAnchor.LowerRight);
        close.rectTransform.Place(new Vector2(1, 0), Vector2.zero, new Vector2(400, 30));
        close.text = "Tab  close the handbook";
        handbookOverlay.SetActive(false);
    }

    RectTransform MapFrame(Transform parent, string label, Vector2 position)
    {
        var caption = UiKit.Label(parent, label, UiKit.SemiBold, 22, InkDim);
        caption.rectTransform.Place(new Vector2(0, 1), position, new Vector2(280, 28));
        caption.text = label;
        var map = UiKit.Panel(parent, label + "Map", new Color(.80f, .75f, .64f));
        map.rectTransform.Place(new Vector2(0, 1), position + new Vector2(0, -32), new Vector2(280, 233));
        return map.rectTransform;
    }

    void BuildGameOver()
    {
        var shade = UiKit.Panel(transform, "GameOver", new Color(.08f, 0, 0, .92f), false);
        shade.rectTransform.Stretch(); shade.raycastTarget = true;
        gameOverOverlay = shade.gameObject;
        var panel = UiKit.Panel(shade.transform, "Panel", UiKit.PanelColor);
        panel.rectTransform.Place(new Vector2(.5f, .5f), Vector2.zero, new Vector2(680, 0));
        panel.Layout<VerticalLayoutGroup>(32, 14, TextAnchor.UpperCenter);
        panel.Fit(false, true);
        UiKit.Label(panel.transform, "Title", UiKit.Bold, 64, UiKit.Red, TextAnchor.MiddleCenter).text = "YOU DIDN'T MAKE IT";
        UiKit.Label(panel.transform, "Sub", UiKit.Medium, 26, UiKit.Dim, TextAnchor.MiddleCenter, true).text = "Day 3. Your health ran out in the storm. Try again from the start of a day.";
        gameOverCards = UiKit.Node("Cards", panel.transform);
        gameOverCards.Layout<VerticalLayoutGroup>(0, 10, TextAnchor.UpperCenter);
        UiKit.Button(panel.transform, "NewGame", "NEW GAME", GameSession.NewGame, out _, 28).Size(-1, 56);
        gameOverOverlay.SetActive(false);
    }

    void BuildScore()
    {
        var shade = UiKit.Panel(transform, "Score", new Color(0, 0, 0, .94f), false);
        shade.rectTransform.Stretch(); shade.raycastTarget = true;
        scoreOverlay = shade.gameObject;
        var panel = UiKit.Panel(shade.transform, "Panel", UiKit.PanelColor);
        panel.rectTransform.Place(new Vector2(.5f, .5f), Vector2.zero, new Vector2(760, 0));
        panel.Layout<VerticalLayoutGroup>(36, 10, TextAnchor.UpperCenter);
        panel.Fit(false, true);
        scoreTitle = UiKit.Label(panel.transform, "Title", UiKit.Bold, 64, UiKit.TextColor, TextAnchor.MiddleCenter);
        scoreSub = UiKit.Label(panel.transform, "Sub", UiKit.Medium, 26, UiKit.Dim, TextAnchor.MiddleCenter, true);
        scoreRows = UiKit.Node("Rows", panel.transform);
        scoreRows.Layout<VerticalLayoutGroup>(0, 6, TextAnchor.UpperCenter);
        var totalRow = UiKit.Node("Total", panel.transform);
        totalRow.Layout<HorizontalLayoutGroup>(0, 16, TextAnchor.MiddleCenter);
        scoreRank = UiKit.Label(totalRow, "Rank", UiKit.Bold, 56, UiKit.Amber, TextAnchor.MiddleLeft);
        scoreRank.Size(-1, -1, 1);
        scoreTotal = UiKit.Label(totalRow, "Points", UiKit.Bold, 56, UiKit.TextColor, TextAnchor.MiddleRight);
        scoreLine = UiKit.Label(panel.transform, "Line", UiKit.SemiBold, 30, UiKit.TextColor, TextAnchor.MiddleCenter, true);
        scoreFact = UiKit.Label(panel.transform, "Fact", UiKit.Medium, 24, UiKit.Dim, TextAnchor.MiddleCenter, true);
        UiKit.Button(panel.transform, "PlayAgain", "PLAY AGAIN", GameSession.NewGame, out _, 30).Size(-1, 60);
        scoreOverlay.SetActive(false);
    }

    void BuildTimer()
    {
        timerPanel = UiKit.Panel(hud, "Timer", UiKit.PanelColor);
        timerPanel.rectTransform.Place(new Vector2(.5f, 1), new Vector2(0, -22), new Vector2(380, 112));
        phaseLabel = UiKit.Label(timerPanel.transform, "Phase", UiKit.SemiBold, 24, UiKit.Amber, TextAnchor.MiddleCenter);
        phaseLabel.rectTransform.Place(new Vector2(.5f, 1), new Vector2(0, -10), new Vector2(360, 30));
        timeLabel = UiKit.Label(timerPanel.transform, "Time", UiKit.Bold, 62, UiKit.TextColor, TextAnchor.MiddleCenter);
        timeLabel.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(0, -6), new Vector2(360, 64));
        UiKit.Bar(timerPanel.transform, "Progress", new Color(1, 1, 1, .12f), UiKit.Amber, out timerFill)
            .Place(new Vector2(.5f, 0), new Vector2(0, 12), new Vector2(330, 6));
    }

    void BuildHealth()
    {
        var panel = UiKit.Panel(hud, "Vitals", UiKit.PanelColor);
        panel.rectTransform.Place(new Vector2(0, 0), new Vector2(28, 28), new Vector2(360, 140));
        // Pulse row (top): beating heart dot, BPM number, zone word. Colour is never the only signal (07).
        heart = UiKit.Node("Heart", panel.transform).Place(new Vector2(0, 1), new Vector2(26, -24), new Vector2(22, 22)).gameObject.AddComponent<Image>();
        heart.sprite = UiKit.Disc; heart.raycastTarget = false;
        heart.rectTransform.pivot = new Vector2(.5f, .5f); heart.rectTransform.anchoredPosition = new Vector2(31, -35);
        pulseValue = UiKit.Label(panel.transform, "Bpm", UiKit.Bold, 40, UiKit.TextColor);
        pulseValue.rectTransform.Place(new Vector2(0, 1), new Vector2(54, -12), new Vector2(200, 46));
        pulseZone = UiKit.Label(panel.transform, "Zone", UiKit.SemiBold, 22, UiKit.Green, TextAnchor.MiddleRight);
        pulseZone.rectTransform.Place(new Vector2(1, 1), new Vector2(-18, -20), new Vector2(160, 30));
        // Health row (bottom).
        var title = UiKit.Label(panel.transform, "Label", UiKit.SemiBold, 20, UiKit.Dim);
        title.rectTransform.Place(new Vector2(0, 0), new Vector2(20, 44), new Vector2(200, 26));
        title.text = "HEALTH";
        UiKit.Bar(panel.transform, "Bar", new Color(1, 1, 1, .12f), UiKit.Green, out healthFill)
            .Place(new Vector2(0, 0), new Vector2(20, 22), new Vector2(250, 14));
        healthValue = UiKit.Label(panel.transform, "Value", UiKit.Bold, 40, UiKit.TextColor, TextAnchor.MiddleRight);
        healthValue.rectTransform.Place(new Vector2(1, 0), new Vector2(-16, 10), new Vector2(80, 50));
        // Contextual key hints just above the panel.
        vitalHints = UiKit.Label(hud, "VitalHints", UiKit.SemiBold, 22, UiKit.TextColor);
        vitalHints.rectTransform.Place(new Vector2(0, 0), new Vector2(30, 176), new Vector2(600, 30));
        vitalHints.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1, -1);
    }

    void BuildCrosshair()
    {
        crosshair = UiKit.Node("Crosshair", hud).Place(new Vector2(.5f, .5f), Vector2.zero, new Vector2(8, 8)).gameObject.AddComponent<Image>();
        crosshair.sprite = UiKit.Disc; crosshair.raycastTarget = false;
        holdRing = UiKit.Node("HoldRing", hud).Place(new Vector2(.5f, .5f), Vector2.zero, new Vector2(64, 64)).gameObject.AddComponent<Image>();
        holdRing.sprite = UiKit.Ring; holdRing.type = Image.Type.Filled; holdRing.fillMethod = Image.FillMethod.Radial360;
        holdRing.fillOrigin = (int)Image.Origin360.Top; holdRing.fillClockwise = true; holdRing.color = UiKit.Amber; holdRing.raycastTarget = false;

        var panel = UiKit.Panel(hud, "Prompt", UiKit.PanelColor);
        prompt = panel.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(0, -64), new Vector2(0, 52));
        prompt.pivot = new Vector2(.5f, 1);
        panel.Layout<HorizontalLayoutGroup>(9, 12, TextAnchor.MiddleLeft).padding = new RectOffset(9, 18, 8, 8);
        panel.Fit(true, true);
        var cap = UiKit.Panel(prompt, "Key", UiKit.TextColor);
        cap.Size(36, 36);
        promptKey = UiKit.Label(cap.transform, "Letter", UiKit.Bold, 24, new Color(.08f, .08f, .1f), TextAnchor.MiddleCenter);
        promptKey.rectTransform.Stretch(); promptKey.text = "E";
        promptText = UiKit.Label(prompt, "Text", UiKit.SemiBold, 28, UiKit.TextColor);
        promptMissing = UiKit.Label(hud, "Missing", UiKit.Medium, 24, UiKit.Amber, TextAnchor.UpperCenter);
        promptMissing.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(0, -124), new Vector2(700, 30));
        promptMissing.rectTransform.pivot = new Vector2(.5f, 1);
    }

    void BuildInventory()
    {
        inventoryRow = UiKit.Node("Inventory", hud).Place(new Vector2(.5f, 0), new Vector2(0, 28), new Vector2(0, 46));
        inventoryRow.Layout<HorizontalLayoutGroup>(0, 8, TextAnchor.MiddleCenter);
        inventoryRow.Fit(true, false);
    }

    void BuildBanner()
    {
        var root = UiKit.Node("PhaseBanner", transform).Place(new Vector2(.5f, .5f), new Vector2(0, 230), new Vector2(1000, 150));
        bannerGroup = root.gameObject.AddComponent<CanvasGroup>(); bannerGroup.alpha = 0; bannerGroup.blocksRaycasts = false;
        bannerTitle = UiKit.Label(root, "Title", UiKit.Bold, 84, UiKit.Amber, TextAnchor.MiddleCenter);
        bannerTitle.rectTransform.Place(new Vector2(.5f, 1), Vector2.zero, new Vector2(1000, 92));
        bannerTitle.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
        bannerSub = UiKit.Label(root, "Sub", UiKit.Medium, 30, UiKit.TextColor, TextAnchor.MiddleCenter);
        bannerSub.rectTransform.Place(new Vector2(.5f, 0), new Vector2(0, 10), new Vector2(1000, 40));
        bannerSub.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1, -1);
    }

    void BuildNight()
    {
        var shade = UiKit.Panel(transform, "Night", Color.black, false);
        shade.rectTransform.Stretch();
        nightGroup = shade.gameObject.AddComponent<CanvasGroup>(); nightGroup.alpha = 0; nightGroup.blocksRaycasts = false;
        nightTitle = UiKit.Label(shade.transform, "Title", UiKit.Bold, 96, UiKit.TextColor, TextAnchor.MiddleCenter);
        nightTitle.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(0, 70), new Vector2(1400, 110));
        nightBody = UiKit.Label(shade.transform, "Body", UiKit.Medium, 36, UiKit.Dim, TextAnchor.UpperCenter, true);
        nightBody.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(0, -20), new Vector2(1100, 160));
        nightBody.rectTransform.pivot = new Vector2(.5f, 1);
        nightFooter = UiKit.Label(shade.transform, "Footer", UiKit.Medium, 26, new Color(1, 1, 1, .4f), TextAnchor.MiddleCenter);
        nightFooter.rectTransform.Place(new Vector2(.5f, 0), new Vector2(0, 80), new Vector2(1000, 40));
    }

    void BuildAlert()
    {
        var shade = UiKit.Panel(transform, "Alert", new Color(0, 0, 0, .88f), false);
        shade.rectTransform.Stretch();
        alertOverlay = shade.gameObject;
        var phone = UiKit.Panel(shade.transform, "Phone", new Color(.02f, .02f, .03f, 1));
        phone.rectTransform.Place(new Vector2(.5f, .5f), Vector2.zero, new Vector2(470, 760));
        var screen = UiKit.Panel(phone.transform, "Screen", new Color(.09f, .1f, .13f, 1));
        screen.rectTransform.Stretch(14);
        var column = UiKit.Node("Column", screen.transform).Stretch(18);
        column.Layout<VerticalLayoutGroup>(0, 16, TextAnchor.UpperCenter);

        var clock = UiKit.Label(column, "Clock", UiKit.SemiBold, 22, UiKit.Dim, TextAnchor.MiddleCenter);
        clock.text = "7:42 AM"; clock.Size(-1, 30);

        var note = UiKit.Panel(column, "Broski", UiKit.PanelLight);
        note.Layout<VerticalLayoutGroup>(14, 4, TextAnchor.UpperLeft);
        var from = UiKit.Label(note.transform, "From", UiKit.SemiBold, 22, UiKit.Amber); from.text = "BROSKI";
        var said = UiKit.Label(note.transform, "Text", UiKit.Medium, 26, UiKit.TextColor, TextAnchor.UpperLeft, true);
        said.text = "Cane did you see the news, quickly get prepared.";

        var alert = UiKit.Panel(column, "Alert", new Color(.16f, .05f, .06f, 1));
        alert.Layout<VerticalLayoutGroup>(0, 0, TextAnchor.UpperLeft);
        var header = UiKit.Panel(alert.transform, "Header", UiKit.Red);
        header.Size(-1, 54);
        var headerText = UiKit.Label(header.transform, "Text", UiKit.Bold, 32, Color.white, TextAnchor.MiddleCenter);
        headerText.rectTransform.Stretch(); headerText.text = "EMERGENCY ALERT";
        var bodyBox = UiKit.Node("BodyBox", alert.transform);
        bodyBox.Layout<VerticalLayoutGroup>(18, 0, TextAnchor.UpperLeft);
        var body = UiKit.Label(bodyBox, "Body", UiKit.Medium, 27, UiKit.TextColor, TextAnchor.UpperLeft, true);
        body.text = AlertBody;

        alertFooter = UiKit.Label(column, "Footer", UiKit.SemiBold, 28, UiKit.TextColor, TextAnchor.MiddleCenter, true);
        alertFooter.text = "Tap to open the alert\n<size=20><color=#9AA0AA>Click or press Space</color></size>";
        alertFooter.Size(-1, 80);
    }

    void BuildPause()
    {
        var shade = UiKit.Panel(transform, "Pause", new Color(0, 0, 0, .72f), false);
        shade.rectTransform.Stretch();
        shade.raycastTarget = true;
        pauseOverlay = shade.gameObject;
        var panel = UiKit.Panel(shade.transform, "Panel", UiKit.PanelColor);
        panel.rectTransform.Place(new Vector2(.5f, .5f), Vector2.zero, new Vector2(660, 0));
        panel.Layout<VerticalLayoutGroup>(32, 14, TextAnchor.UpperCenter);
        panel.Fit(false, true);
        var title = UiKit.Label(panel.transform, "Title", UiKit.Bold, 64, UiKit.TextColor, TextAnchor.MiddleCenter);
        title.text = "PAUSED";
        pauseSubtitle = UiKit.Label(panel.transform, "Subtitle", UiKit.Medium, 26, UiKit.Dim, TextAnchor.MiddleCenter);
        UiKit.Button(panel.transform, "Resume", "RESUME", PauseMenu.Resume, out _, 32).Size(-1, 64);
        var restartTitle = UiKit.Label(panel.transform, "RestartTitle", UiKit.SemiBold, 22, UiKit.Dim, TextAnchor.MiddleLeft);
        restartTitle.text = "RESTART FROM THE START OF A DAY";
        pauseCards = UiKit.Node("Cards", panel.transform);
        pauseCards.Layout<VerticalLayoutGroup>(0, 10, TextAnchor.UpperCenter);
        var help = UiKit.Label(panel.transform, "Help", UiKit.Medium, 22, UiKit.Dim, TextAnchor.MiddleCenter, true);
        help.text = "WASD move  ·  Shift run  ·  E use (hold for some)  ·  G drop armful  ·  Tab phone / handbook  ·  F flashlight  ·  Q breathe  ·  H first aid";
        pauseOverlay.SetActive(false);
    }

    // ------------------------------------------------------------------ update

    void Update()
    {
        var session = GameSession.Instance;
        var director = session ? session.GetComponent<DayDirector>() : null;
        if (!explorer || !director) { hud.gameObject.SetActive(false); return; }
        var phase = director.Phase;

        bool alert = phase == DayPhase.Alert;
        bool night = phase == DayPhase.Night;
        bool ended = phase == DayPhase.Ended, gameOver = phase == DayPhase.GameOver;
        alertOverlay.SetActive(alert);
        hud.gameObject.SetActive(!alert && !night && !ended && !gameOver);
        UpdateNight(night, director);
        if (phase != shownPhase)
        {
            shownPhase = phase;
            gameOverOverlay.SetActive(gameOver);
            scoreOverlay.SetActive(ended);
            if (gameOver) FillCards(gameOverCards);
            if (ended) FillScore(director);
        }
        var health = session.GetComponent<HealthSystem>();
        faintGroup.alpha = Mathf.MoveTowards(faintGroup.alpha, health && health.Fainted ? 1 : 0, Time.unscaledDeltaTime * .7f);
        if (alert) alertFooter.color = Color.Lerp(UiKit.TextColor, UiKit.Amber, .5f + .5f * Mathf.Sin(Time.unscaledTime * 4));

        bool paused = pause && pause.Open;
        if (paused != pauseWasOpen) { pauseWasOpen = paused; pauseOverlay.SetActive(paused); if (paused) FillPauseCards(session, director); }

        if (!alert && !night && !ended && !gameOver)
        {
            UpdateTimer(director, session);
            UpdateHealth(session);
            UpdatePulse(session);
            UpdatePrompt();
            UpdateInventory();
            UpdateTasks(session, director);
        }
        UpdateHandheld(session, director, !alert && !night && !ended && !gameOver);
        UpdateToasts();
        bannerGroup.alpha = Mathf.Clamp01((bannerUntil - Time.unscaledTime) / .6f);
        var flash = damageFlash.color; flash.a = Mathf.MoveTowards(flash.a, 0, Time.unscaledDeltaTime * .8f); damageFlash.color = flash;
    }

    void UpdateTimer(DayDirector director, GameSession session)
    {
        var color = UiKit.PhaseColor(director.Phase);
        string name = director.Phase == DayPhase.Preparation ? "PREPARATION TIME" : director.Phase == DayPhase.Disaster ? "DISASTER TIME" : "AFTER-DISASTER TIME";
        phaseLabel.text = "DAY " + session.CurrentDay + "  ·  " + name;
        phaseLabel.color = color;
        float left = Mathf.Max(0, director.TimeLeft), length = Mathf.Max(1, director.Today.Length(director.Phase));
        timeLabel.text = Mathf.FloorToInt(left / 60).ToString("00") + ":" + Mathf.FloorToInt(left % 60).ToString("00");
        timeLabel.color = left <= 10 ? Color.Lerp(UiKit.TextColor, UiKit.Red, .5f + .5f * Mathf.Sin(Time.unscaledTime * 9)) : UiKit.TextColor;
        timerFill.color = color;
        UiKit.SetFill(timerFill, left / length);
    }

    void UpdateHealth(GameSession session)
    {
        float health = session.health;
        if (lastHealth >= 0 && health < lastHealth - .5f) { var c = damageFlash.color; c.a = .45f; damageFlash.color = c; }
        lastHealth = health;
        UiKit.SetFill(healthFill, health / 100f);
        healthFill.color = health > 50 ? UiKit.Green : health > 25 ? UiKit.Amber : UiKit.Red;
        healthValue.text = Mathf.RoundToInt(health).ToString();
    }

    void UpdatePrompt()
    {
        var focus = explorer.enabled && Cursor.lockState == CursorLockMode.Locked ? explorer.Focus : null;
        crosshair.color = focus != null ? UiKit.Amber : new Color(1, 1, 1, .6f);
        crosshair.rectTransform.sizeDelta = Vector2.one * (focus != null ? 10 : 6);
        prompt.gameObject.SetActive(focus != null);
        string missing = focus?.Unavailable(explorer);
        promptMissing.text = string.IsNullOrEmpty(missing) ? "" : missing;
        UpdateRing();
        if (focus == null) return;
        bool hold = focus.HoldSeconds > 0;
        promptText.text = (hold ? "<color=#9AA0AA>HOLD</color>  " : "") + focus.Prompt;
        promptText.color = string.IsNullOrEmpty(missing) ? UiKit.TextColor : UiKit.Dim;
    }

    void UpdateHandheld(GameSession session, DayDirector director, bool playing)
    {
        var hand = session.GetComponent<HandheldSystem>();
        var view = playing && hand ? hand.Open : HandheldSystem.View.None;
        phoneOverlay.SetActive(view == HandheldSystem.View.Phone);
        handbookOverlay.SetActive(view == HandheldSystem.View.Handbook);
        if (view == HandheldSystem.View.None) { overlaySignature = null; return; }
        if (Time.unscaledTime < overlayRefresh && overlaySignature != null) return;
        overlayRefresh = Time.unscaledTime + .25f;
        var engine = session.GetComponent<TaskEngine>();
        if (view == HandheldSystem.View.Phone) FillPhone(session, hand, engine);
        else FillHandbook(session, engine);
    }

    void FillPhone(GameSession session, HandheldSystem hand, TaskEngine engine)
    {
        phoneDeadScreen.SetActive(hand.PhoneDead);
        phoneStatus.text = "7:42 AM        " + Mathf.CeilToInt(session.phoneBattery) + "%";
        var current = engine ? engine.Current : null;
        phoneTask.text = current == null ? "All set for now." : current.definition.text;
        phoneHint.text = current == null ? "Stay away from the windows." : current.definition.hint;
        string signature = "T" + hand.Thread.Count;
        if (signature == overlaySignature) return;
        overlaySignature = signature;
        foreach (Transform child in phoneThread) Destroy(child.gameObject);
        foreach (var message in hand.Thread.Skip(Mathf.Max(0, hand.Thread.Count - 5)))
        {
            var bubble = UiKit.Panel(phoneThread, "Bubble", UiKit.PanelLight);
            bubble.Layout<VerticalLayoutGroup>(12, 0, TextAnchor.UpperLeft);
            UiKit.Label(bubble.transform, "Text", UiKit.Medium, 24, UiKit.TextColor, TextAnchor.UpperLeft, true).text = message.text;
        }
    }

    void FillHandbook(GameSession session, TaskEngine engine)
    {
        handbookSubtitle.text = "DAY " + session.CurrentDay + "  ·  CHECKLIST";
        string signature = engine ? string.Join(",", engine.Today.Select(t => t.definition.id + (int)t.status)) : "";
        if (signature != overlaySignature)
        {
            overlaySignature = signature;
            foreach (Transform child in handbookList) Destroy(child.gameObject);
            if (engine)
                foreach (var task in engine.Today)
                {
                    if (task.status == TaskStatus.Hidden) continue;
                    var row = UiKit.Node("Row", handbookList);
                    row.Layout<HorizontalLayoutGroup>(0, 12, TextAnchor.UpperLeft);
                    var box = UiKit.Panel(row, "Box", task.status == TaskStatus.Done ? new Color(.18f, .55f, .34f) : task.status == TaskStatus.Locked ? new Color(.75f, .22f, .17f) : new Color(.55f, .51f, .44f, .5f));
                    box.Size(20, 20);
                    var text = UiKit.Label(row, "Text", UiKit.Medium, 26, task.status == TaskStatus.Open ? Ink : InkDim, TextAnchor.UpperLeft, true);
                    text.text = task.definition.text + (task.status == TaskStatus.Locked ? "  <color=#C0392B>too late</color>" : !task.definition.required ? "  <color=#8C8579>optional</color>" : "")
                        + (task.status == TaskStatus.Open ? "\n<size=20><color=#6B655C>" + task.definition.hint + "</color></size>" : "");
                    text.Size(-1, -1, 1);
                }
        }
        // The floor plan is live: redraw on every refresh.
        foreach (Transform child in groundMap) Destroy(child.gameObject);
        foreach (Transform child in upperMap) Destroy(child.gameObject);
        foreach (var opening in FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude))
        {
            var c = opening.Center;
            var map = c.y >= 3 ? upperMap : groundMap;
            var marker = UiKit.Panel(map, opening.Id, StateColor(opening.State));
            marker.rectTransform.Place(Vector2.zero, MapPoint(c, map) - new Vector2(13, 13), new Vector2(26, 26));
            var label = UiKit.Label(marker.transform, "Id", UiKit.Bold, 15, Color.white, TextAnchor.MiddleCenter);
            label.rectTransform.Stretch();
            label.text = opening.IsDoor ? "D" : opening.Id.Substring(1);
        }
        var feet = explorer.transform.position;
        if (feet.y > -1) // not in the basement
        {
            var map = feet.y >= 2.5f ? upperMap : groundMap;
            var you = UiKit.Node("You", map).gameObject.AddComponent<Image>();
            you.sprite = UiKit.Disc; you.color = Color.white; you.raycastTarget = false;
            you.rectTransform.Place(Vector2.zero, MapPoint(feet, map) - new Vector2(9, 9), new Vector2(18, 18));
            you.gameObject.AddComponent<Outline>().effectColor = Ink;
        }
    }

    /// <summary>House footprint x -12..0, z -10..0 mapped onto a map panel: north (z -10) up, east (x 0) right.</summary>
    static Vector2 MapPoint(Vector3 world, RectTransform map)
    {
        float u = Mathf.Clamp01((world.x + 12) / 12f), v = Mathf.Clamp01(-world.z / 10f);
        return new Vector2(u * map.sizeDelta.x, v * map.sizeDelta.y);
    }

    static Color StateColor(OpeningState state)
    {
        switch (state)
        {
            case OpeningState.Reinforced: return new Color(.18f, .55f, .34f);
            case OpeningState.Cracked: return new Color(.83f, .54f, .12f);
            case OpeningState.Broken: return new Color(.75f, .22f, .17f);
            case OpeningState.Repaired: return new Color(.18f, .43f, .71f);
            case OpeningState.Unreinforced: return new Color(.55f, .52f, .47f);
            default: return new Color(.55f, .52f, .47f, .25f); // not in play yet
        }
    }

    /// <summary>One ring around the crosshair for every hold: E actions (amber), first aid (green), breathing (blue).</summary>
    void UpdateRing()
    {
        var session = GameSession.Instance;
        var pulse = session ? session.GetComponent<PulseSystem>() : null;
        var health = session ? session.GetComponent<HealthSystem>() : null;
        float value = explorer.HoldProgress; Color color = UiKit.Amber; string caption = "";
        if (health && health.FirstAidProgress > 0) { value = health.FirstAidProgress; color = UiKit.Green; caption = "Using first aid..."; }
        if (pulse && pulse.Breathing) { value = pulse.BreathProgress; color = UiKit.Blue; caption = value < 1 ? "Breathe in... slowly" : "Breathing out. Your heart slows."; }
        holdRing.fillAmount = value;
        holdRing.color = color;
        holdRing.enabled = value > 0;
        ringCaption.text = caption;
    }

    void UpdatePulse(GameSession session)
    {
        var pulse = session.GetComponent<PulseSystem>();
        var health = session.GetComponent<HealthSystem>();
        if (!pulse) { heart.enabled = false; pulseValue.text = ""; pulseZone.text = ""; return; }
        float bpm = pulse.Bpm;
        var zone = pulse.Zone;
        var color = zone == PulseZone.Calm ? UiKit.Green : zone == PulseZone.Tense ? UiKit.Amber : UiKit.Red;
        // Beat at the real BPM: a quick swell then a slow settle.
        heartPhase = (heartPhase + Time.deltaTime * bpm / 60f) % 1f;
        float beat = Mathf.Exp(-heartPhase * 9f);
        heart.enabled = true;
        heart.color = color;
        heart.rectTransform.localScale = Vector3.one * (1 + .45f * beat);
        pulseValue.text = Mathf.RoundToInt(bpm) + " <size=22><color=#9AA0AA>BPM</color></size>";
        pulseZone.text = zone.ToString().ToUpperInvariant();
        pulseZone.color = color;

        var hints = new List<string>();
        if (zone != PulseZone.Calm && !pulse.Breathing)
            hints.Add(pulse.BreathCooldown > 0 ? "<color=#9AA0AA>Q  breathe in " + Mathf.CeilToInt(pulse.BreathCooldown) + "s</color>" : "Q  hold to breathe");
        var power = session.GetComponent<PowerSystem>();
        if (power && power.PowerOut && power.HasFlashlight && !power.FlashlightOn) hints.Add("F  flashlight");
        var flood = session.GetComponent<FloodSystem>();
        if (flood && flood.PlayerInWater) hints.Add("<color=#E5484D>You're wading in flood water</color>");
        if (health && health.CanUseFirstAid && session.health < 70)
            hints.Add("H  hold for first aid  <color=#9AA0AA>(" + explorer.Inventory.Uses(ItemKind.FirstAid) + ")</color>");
        vitalHints.text = string.Join("      ", hints);
    }

    void FillScore(DayDirector director)
    {
        var lines = director.NightText.Split('\n');
        scoreTitle.text = lines[0].ToUpperInvariant();
        scoreSub.text = string.Join(" ", lines.Skip(1).Where(l => l.Length > 0));
        foreach (Transform child in scoreRows) Destroy(child.gameObject);
        var r = director.Score;
        if (r == null) return;
        ScoreRow("Health remaining", r.health, 300, Mathf.RoundToInt(GameSession.Instance.health) + "%");
        ScoreRow("Openings still intact", r.openings, 250, r.intact + " / " + r.openingCount);
        ScoreRow("Reinforcements made", r.reinforcements, 150, r.reinforcedCount + " / 15");
        ScoreRow("Supplies left", r.supplies, 100, r.supplyCount.ToString());
        ScoreRow("Tasks completed", r.tasks, 100, r.tasksDone + " / " + r.tasksRequired);
        ScoreRow("Damage repaired", r.repairs, 100, r.hits == 0 ? "no damage" : (r.hits - r.damagedNow) + " / " + r.hits);
        scoreRank.text = r.rank;
        scoreRank.color = r.total >= 900 ? UiKit.Green : r.total >= 700 ? UiKit.Amber : r.total >= 450 ? UiKit.Blue : UiKit.Red;
        scoreTotal.text = r.total + " <size=30><color=#9AA0AA>/ 1000</color></size>";
        scoreLine.text = r.line;
        scoreFact.text = "Did you know?  " + r.fact;
    }

    void ScoreRow(string label, int points, int max, string detail)
    {
        var row = UiKit.Panel(scoreRows, "Row", new Color(1, 1, 1, .05f));
        row.Layout<HorizontalLayoutGroup>(0, 12, TextAnchor.MiddleLeft).padding = new RectOffset(16, 16, 6, 6);
        var name = UiKit.Label(row.transform, "Label", UiKit.SemiBold, 28, UiKit.TextColor); name.text = label; name.Size(-1, -1, 1);
        UiKit.Label(row.transform, "Detail", UiKit.Medium, 24, UiKit.Dim, TextAnchor.MiddleRight).text = detail;
        var value = UiKit.Label(row.transform, "Points", UiKit.Bold, 30, UiKit.TextColor, TextAnchor.MiddleRight);
        value.text = points + " <size=20><color=#9AA0AA>/ " + max + "</color></size>";
        value.Size(130);
    }

    void UpdateInventory()
    {
        var power = GameSession.Instance ? GameSession.Instance.GetComponent<PowerSystem>() : null;
        var inventory = explorer.Inventory;
        var chips = new List<string>();
        foreach (var group in inventory.Pockets.GroupBy(p => p.Kind))
        {
            int uses = group.Sum(p => p.UsesLeft);
            string extra = uses > 0 ? "  <color=#9AA0AA>" + uses + " uses</color>" : "";
            if (group.Key == ItemKind.Flashlight && power) extra = "  <color=" + (power.FlashlightOn ? "#F2A93B" : "#9AA0AA") + ">" + (power.FlashlightOn ? "ON " : "") + Mathf.CeilToInt(power.Charge) + "%</color>";
            chips.Add(Pickup.DisplayName(group.Key) + (group.Count() > 1 ? "  ×" + group.Count() : "") + extra);
        }
        string arms = "";
        if (inventory.Armful.Count > 0)
            arms = "ARMS  " + string.Join(", ", inventory.Armful.GroupBy(p => p.Kind).Select(g => Pickup.DisplayName(g.Key) + " " + g.Count()))
                + "  <color=#9AA0AA>" + inventory.Armful.Count + "/" + PlayerInventory.Capacity(inventory.ArmfulGroup.Value) + "</color>";
        string signature = string.Join("|", chips) + "#" + arms;
        if (signature == inventorySignature) return;
        inventorySignature = signature;
        foreach (Transform child in inventoryRow) Destroy(child.gameObject);
        foreach (var chip in chips) Chip(chip, UiKit.PanelColor, UiKit.TextColor);
        if (arms != "") Chip(arms, new Color(UiKit.Amber.r * .35f, UiKit.Amber.g * .35f, UiKit.Amber.b * .35f, .92f), UiKit.Amber);
    }

    void Chip(string text, Color background, Color color)
    {
        var chip = UiKit.Panel(inventoryRow, "Chip", background);
        chip.Layout<HorizontalLayoutGroup>(0, 0, TextAnchor.MiddleCenter).padding = new RectOffset(16, 16, 6, 6);
        var label = UiKit.Label(chip.transform, "Text", UiKit.SemiBold, 24, color, TextAnchor.MiddleCenter);
        label.text = text;
    }

    void UpdateTasks(GameSession session, DayDirector director)
    {
        var engine = session.GetComponent<TaskEngine>();
        var hand = session.GetComponent<HandheldSystem>();
        if (!engine) return;
        bool hasBook = hand && hand.HasHandbook;
        // Day 1 (and Days 2-3 until the handbook is found) the phone guides one task at a time.
        bool phone = director.Today.taskSource == "phone" || !hasBook;
        bool dead = hand && hand.PhoneDead;
        int unread = hand ? hand.Unread : 0;
        var current = engine.Current;
        string signature = (phone ? "P" + (current == null ? "-" : current.definition.id) + Mathf.RoundToInt(session.phoneBattery) + dead
            : "H" + string.Join(",", engine.Today.Select(t => t.definition.id + (int)t.status))) + "u" + unread;
        if (signature == taskSignature) return;
        taskSignature = signature;
        foreach (Transform child in taskPanel) Destroy(child.gameObject);
        var panel = UiKit.Panel(taskPanel, "Panel", UiKit.PanelColor);
        var rt = panel.rectTransform.Place(new Vector2(1, 1), Vector2.zero, new Vector2(phone ? 400 : 460, 0));
        panel.Layout<VerticalLayoutGroup>(20, 8, TextAnchor.UpperLeft);
        panel.Fit(false, true);

        var header = UiKit.Node("Header", rt);
        header.Layout<HorizontalLayoutGroup>(0, 8, TextAnchor.MiddleLeft);
        var title = UiKit.Label(header, "Title", UiKit.SemiBold, 22, UiKit.Dim);
        title.text = phone ? (dead ? "PHONE  <color=#E5484D>DEAD</color>" : "PHONE") : "EMERGENCY HANDBOOK";
        title.Size(-1, -1, 1);
        if (phone && !dead) UiKit.Label(header, "Battery", UiKit.SemiBold, 22, session.phoneBattery < 15 ? UiKit.Red : UiKit.Dim, TextAnchor.MiddleRight).text = Mathf.RoundToInt(session.phoneBattery) + "%";
        if (phone)
        {
            if (current == null)
                UiKit.Label(rt, "Done", UiKit.Bold, 30, UiKit.Green, TextAnchor.UpperLeft, true).text = "All set for now. Stay safe.";
            else
            {
                UiKit.Label(rt, "Kicker", UiKit.SemiBold, 20, UiKit.Amber).text = "NEXT TASK";
                UiKit.Label(rt, "Task", UiKit.Bold, 32, UiKit.TextColor, TextAnchor.UpperLeft, true).text = current.definition.text;
                UiKit.Label(rt, "Hint", UiKit.Medium, 24, UiKit.Dim, TextAnchor.UpperLeft, true).text = current.definition.hint;
            }
        }
        else
            foreach (var task in engine.Today)
            {
                if (task.status == TaskStatus.Hidden) continue;
                var row = UiKit.Node("Row", rt);
                row.Layout<HorizontalLayoutGroup>(0, 12, TextAnchor.UpperLeft);
                var box = UiKit.Panel(row, "Box", task.status == TaskStatus.Done ? UiKit.Green : task.status == TaskStatus.Locked ? new Color(UiKit.Red.r, UiKit.Red.g, UiKit.Red.b, .5f) : new Color(1, 1, 1, .16f));
                box.Size(20, 20);
                bool closed = task.status != TaskStatus.Open;
                var text = UiKit.Label(row, "Text", UiKit.Medium, 24, closed ? UiKit.Dim : UiKit.TextColor, TextAnchor.UpperLeft, true);
                text.text = task.definition.text + (task.status == TaskStatus.Locked ? "  <color=#E5484D>too late</color>" : !task.definition.required ? "  <color=#9AA0AA>optional</color>" : "");
                text.Size(-1, -1, 1);
            }
        string footer = hasBook ? "Tab  open the handbook (floor plan)" : dead ? "" : "Tab  open the phone";
        if (unread > 0 && !dead) footer = "<color=#F2A93B>" + unread + " new message" + (unread > 1 ? "s" : "") + "</color>   " + footer;
        if (footer != "") UiKit.Label(rt, "Footer", UiKit.SemiBold, 20, UiKit.Dim, TextAnchor.UpperLeft, true).text = footer;
    }

    void UpdateNight(bool night, DayDirector director)
    {
        float target = night ? 1 : 0;
        nightGroup.alpha = Mathf.MoveTowards(nightGroup.alpha, target, Time.unscaledDeltaTime * (night ? 1.2f : 2.5f));
        nightGroup.blocksRaycasts = night;
        if (!night) return;
        var lines = director.NightText.Split('\n');
        nightTitle.text = lines[0].ToUpperInvariant();
        nightBody.text = string.Join("\n", lines.Skip(1).Where(l => l.Length > 0));
        nightFooter.text = director.Phase == DayPhase.Ended ? "" : "Morning is coming...";
    }

    void FillPauseCards(GameSession session, DayDirector director)
    {
        pauseSubtitle.text = session ? "DAY " + session.CurrentDay + "  ·  " + director.Phase.ToString().ToUpperInvariant() : "";
        FillCards(pauseCards);
    }

    /// <summary>One Restart button per saved day (pause menu and game over).</summary>
    void FillCards(RectTransform cards)
    {
        foreach (Transform child in cards) Destroy(child.gameObject);
        var saves = SaveSystem.All();
        if (saves.Count == 0) UiKit.Label(cards, "None", UiKit.Medium, 24, UiKit.Dim, TextAnchor.MiddleCenter).text = "No saved days yet.";
        foreach (var save in saves)
        {
            int day = save.day;
            UiKit.Button(cards, "Day" + day,
                "<size=34>DAY " + day + "</size>\n<size=22><color=#9AA0AA>Health " + Mathf.RoundToInt(save.health) + "   ·   Boarded " + save.reinforced + "/15   ·   Water " + save.water + "   ·   Food " + save.food + "</color></size>",
                () => GameSession.RestartFromDay(day), out _).Size(-1, 92);
        }
    }

    // ------------------------------------------------------------------ toasts & banner

    void OnTaskDone(TaskState task)
    {
        if (task.definition.id.EndsWith("_alert")) return;
        Toast("DONE   " + task.definition.text, UiKit.Green);
    }

    void OnPhase(DayPhase phase)
    {
        string title = null, sub = null;
        if (phase == DayPhase.Preparation) { title = "PREPARATION TIME"; sub = "Get the house ready before the storm."; }
        else if (phase == DayPhase.Disaster) { title = "DISASTER TIME"; sub = "The storm hits. Everything takes longer."; }
        else if (phase == DayPhase.AfterDisaster) { title = "AFTER-DISASTER TIME"; sub = "Fix what's left before nightfall."; }
        if (title == null) return;
        // Read today's data, not PowerSystem: this can run before PowerSystem has handled the new day.
        var session = GameSession.Instance;
        var director = session ? session.GetComponent<DayDirector>() : null;
        if (phase == DayPhase.Preparation && director && director.Today.powerOut) sub += "  The power is out.";
        bannerTitle.text = title; bannerTitle.color = UiKit.PhaseColor(phase); bannerSub.text = sub;
        bannerUntil = Time.unscaledTime + 3.2f;
    }

    void OnWarning(BreakableOpening opening, float seconds)
    {
        if (explorer && opening.DistanceTo(explorer.viewCamera.transform.position) < 9)
            Toast((opening.IsDoor ? "The front door" : "A window") + " is creaking...", UiKit.Amber);
    }

    void OnHit(BreakableOpening opening, OpeningState result)
    {
        if (!explorer || opening.DistanceTo(explorer.viewCamera.transform.position) > 9) return;
        Toast(result == OpeningState.Broken ? (opening.IsDoor ? "The front door gave way!" : "Glass shatters!") : "The boards held, but the glass cracked.", result == OpeningState.Broken ? UiKit.Red : UiKit.Amber);
    }

    void Toast(string text, Color accent)
    {
        var panel = UiKit.Panel(toastColumn, "Toast", UiKit.PanelColor);
        panel.Layout<HorizontalLayoutGroup>(0, 12, TextAnchor.MiddleCenter).padding = new RectOffset(14, 22, 10, 10);
        var bar = UiKit.Panel(panel.transform, "Accent", accent); bar.Size(6, 30);
        UiKit.Label(panel.transform, "Text", UiKit.SemiBold, 28, UiKit.TextColor).text = text;
        var group = panel.gameObject.AddComponent<CanvasGroup>();
        toasts.Add(new KeyValuePair<CanvasGroup, float>(group, Time.unscaledTime + 3.2f));
        while (toasts.Count > 3) { Destroy(toasts[0].Key.gameObject); toasts.RemoveAt(0); }
    }

    void UpdateToasts()
    {
        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            float left = toasts[i].Value - Time.unscaledTime;
            if (left <= 0) { Destroy(toasts[i].Key.gameObject); toasts.RemoveAt(i); continue; }
            toasts[i].Key.alpha = Mathf.Clamp01(left / .5f);
        }
    }
}
