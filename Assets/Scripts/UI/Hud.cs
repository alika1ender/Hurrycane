using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placeholder HUD (Docs/Design/07), IMGUI until the real UI: phase timer top centre, health bottom left, the task
/// source top right (Day 1 phone: one task + hint; later days: handbook list), toasts, and the alert / night overlays.
/// </summary>
[RequireComponent(typeof(MapExplorer))]
public sealed class Hud : MonoBehaviour
{
    const string AlertText = "EMERGENCY ALERT\n\nHurricane warning in effect for your area. Landfall expected this evening. " +
        "Board up or secure windows and doors. Gather water, food, medication and a flashlight. Charge your phone. Stay indoors.";

    MapExplorer explorer;
    readonly List<KeyValuePair<string, float>> toasts = new List<KeyValuePair<string, float>>();
    GUIStyle center, big, small;

    void Awake() => explorer = GetComponent<MapExplorer>();
    void OnEnable()
    {
        TaskEngine.TaskCompleted += OnTaskDone;
        DayDirector.PhaseChanged += OnPhase;
        BreakableOpening.WarningStarted += OnWarning;
    }
    void OnDisable()
    {
        TaskEngine.TaskCompleted -= OnTaskDone;
        DayDirector.PhaseChanged -= OnPhase;
        BreakableOpening.WarningStarted -= OnWarning;
    }

    void OnTaskDone(TaskState task) { if (task.definition.id != "d1_alert") Toast("Done: " + task.definition.text); }
    void OnPhase(DayPhase phase)
    {
        if (phase == DayPhase.Preparation) Toast("Preparation Time: get the house ready");
        else if (phase == DayPhase.Disaster) Toast("Disaster Time: the storm hits. Work takes longer.");
        else if (phase == DayPhase.AfterDisaster) Toast("After-Disaster Time: fix what's left");
    }
    void OnWarning(BreakableOpening opening, float seconds)
    {
        if (Vector3.Distance(opening.transform.position, explorer.transform.position) < 8)
            Toast((opening.IsDoor ? "The front door" : "A window") + " is creaking...");
    }
    void Toast(string text) => toasts.Add(new KeyValuePair<string, float>(text, Time.unscaledTime + 3));

    void OnGUI()
    {
        var session = GameSession.Instance;
        var director = session ? session.GetComponent<DayDirector>() : null;
        if (!director) return;
        GUI.depth = -10;
        if (center == null)
        {
            center = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = 18 };
            big = new GUIStyle(center) { fontSize = 26, fontStyle = FontStyle.Bold };
            small = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 14 };
        }
        float w = Screen.width, h = Screen.height;

        if (director.Phase == DayPhase.Alert)
        {
            Shade(.75f);
            GUI.Box(new Rect(w * .5f - 260, h * .5f - 150, 520, 300), "PHONE");
            GUI.Label(new Rect(w * .5f - 240, h * .5f - 120, 480, 220), "Broski: Cane did you see the news, quickly get prepared.\n\n" + AlertText, center);
            GUI.Label(new Rect(w * .5f - 240, h * .5f + 105, 480, 30), "Click or press Space to open the alert", center);
            return;
        }
        if (director.Phase == DayPhase.Night || director.Phase == DayPhase.Ended)
        {
            Shade(1);
            GUI.Label(new Rect(w * .5f - 300, h * .5f - 120, 600, 240), director.NightText, big);
            return;
        }

        // Timer, top centre. The last 10 seconds pulse.
        string label = director.Phase == DayPhase.Preparation ? "Preparation Time" : director.Phase == DayPhase.Disaster ? "Disaster Time" : "After-Disaster Time";
        float left = Mathf.Max(0, director.TimeLeft);
        var old = GUI.color;
        if (left <= 10) GUI.color = Color.Lerp(Color.white, new Color(1, .3f, .25f), .5f + .5f * Mathf.Sin(Time.unscaledTime * 10));
        GUI.Box(new Rect(w * .5f - 140, 12, 280, 60), "");
        GUI.Label(new Rect(w * .5f - 140, 14, 280, 24), "Day " + session.CurrentDay + " · " + label, center);
        GUI.Label(new Rect(w * .5f - 140, 36, 280, 34), Mathf.FloorToInt(left / 60) + ":" + Mathf.FloorToInt(left % 60).ToString("00"), big);
        GUI.color = old;

        // Health, bottom left (pulse goes next to it later).
        GUI.Box(new Rect(16, h - 52, 230, 36), "");
        GUI.Label(new Rect(26, h - 48, 80, 28), "Health", small);
        GUI.color = new Color(0, 0, 0, .6f); GUI.DrawTexture(new Rect(90, h - 42, 140, 14), Texture2D.whiteTexture);
        GUI.color = session.health < 25 ? new Color(.9f, .2f, .2f) : new Color(.3f, .85f, .4f);
        GUI.DrawTexture(new Rect(90, h - 42, 140 * session.health / 100f, 14), Texture2D.whiteTexture);
        GUI.color = old;

        // Task source, top right.
        var tasks = session.GetComponent<TaskEngine>();
        if (tasks) DrawTasks(tasks, director.Today.taskSource == "phone", w);

        // Toasts.
        toasts.RemoveAll(t => t.Value < Time.unscaledTime);
        for (int i = 0; i < toasts.Count; i++)
            GUI.Label(new Rect(w * .5f - 300, h * .68f + i * 28, 600, 26), toasts[i].Key, center);
    }

    void DrawTasks(TaskEngine tasks, bool phone, float w)
    {
        if (phone)
        {
            var current = tasks.Current;
            GUI.Box(new Rect(w - 336, 12, 320, 92), "PHONE");
            GUI.Label(new Rect(w - 326, 34, 300, 66), current == null ? "All done. Stay safe." : current.definition.text + "\n<i>" + current.definition.hint + "</i>", new GUIStyle(small) { richText = true });
            return;
        }
        var lines = new List<string>();
        foreach (var t in tasks.Today)
        {
            if (t.status == TaskStatus.Hidden) continue;
            string mark = t.status == TaskStatus.Done ? "[x] " : t.status == TaskStatus.Locked ? "[-] " : "[ ] ";
            lines.Add(mark + t.definition.text + (t.status == TaskStatus.Locked ? " (too late)" : t.definition.required ? "" : " (optional)"));
        }
        float height = 34 + lines.Count * 20;
        GUI.Box(new Rect(w - 376, 12, 360, height), "HANDBOOK");
        for (int i = 0; i < lines.Count; i++) GUI.Label(new Rect(w - 366, 34 + i * 20, 344, 20), lines[i], small);
    }

    static void Shade(float alpha)
    {
        var old = GUI.color;
        GUI.color = new Color(0, 0, 0, alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;
    }
}
