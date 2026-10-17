using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Animated weather seen through the windows, driven by the day's phase.</summary>
public sealed class WindowWeather : MonoBehaviour
{
    readonly List<GameObject> panes = new List<GameObject>();
    readonly List<Material> materials = new List<Material>();
    readonly List<Light> daylight = new List<Light>();
    readonly List<float> intensities = new List<float>();
    DayDirector director;
    GameSession session;
    int finalDay;
    float finalStorm;
    float strength;
    public float Strength => strength;
    public int WindowCount => panes.Count;

    void Start()
    {
        director = GetComponent<DayDirector>();
        session = GetComponent<GameSession>();
        foreach (var day in director.Config.days) finalDay = Mathf.Max(finalDay, day.day);
        var shader = Resources.Load<Shader>("StormWindow");
        if (!shader) { Debug.LogError("WindowWeather: StormWindow shader missing"); enabled = false; return; }
        foreach (var opening in FindObjectsByType<BreakableOpening>(FindObjectsInactive.Exclude))
        {
            if (opening.IsDoor) continue;
            var b = opening.GetComponent<Collider>().bounds;
            bool thinX = b.size.x < b.size.z;
            var inward = thinX ? new Vector3(Mathf.Sign(-6 - b.center.x), 0, 0)
                : new Vector3(0, 0, Mathf.Sign(-5 - b.center.z));
            var pane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pane.name = "Weather_" + opening.Id;
            Destroy(pane.GetComponent<Collider>());
            pane.transform.SetParent(transform, true);
            pane.transform.SetPositionAndRotation(b.center + inward * .035f, Quaternion.LookRotation(-inward));
            float width = thinX ? b.size.z : b.size.x;
            pane.transform.localScale = new Vector3(width * .985f, b.size.y * .985f, 1);
            var material = new Material(shader) { name = "Weather_" + opening.Id };
            material.SetFloat("_Aspect", width / b.size.y);
            material.SetFloat("_Seed", panes.Count * 3.17f);
            var renderer = pane.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            panes.Add(pane); materials.Add(material);
            pane.SetActive(false);
        }
        foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Include))
            if (light.name.Contains("Window")) { daylight.Add(light); intensities.Add(light.intensity); }
    }

    void LateUpdate()
    {
        if (!director) return;
        var phase = director.Phase;
        bool visible = phase >= DayPhase.Alert && phase <= DayPhase.Night || phase == DayPhase.GameOver;
        float target = phase == DayPhase.Disaster ? 1 : phase == DayPhase.AfterDisaster ? .55f
            : phase == DayPhase.Night ? .85f : visible ? .23f : 0;
        strength = Mathf.MoveTowards(strength, target, Time.deltaTime * .18f);
        bool finale = phase == DayPhase.Disaster && session && session.CurrentDay == finalDay;
        finalStorm = Mathf.MoveTowards(finalStorm, finale ? 1 : 0, Time.deltaTime * .25f);
        for (int i = 0; i < panes.Count; i++)
        {
            panes[i].SetActive(visible);
            materials[i].SetFloat("_Storm", strength);
            materials[i].SetFloat("_FinalStorm", finalStorm);
            // Alert screens and menus can pause gameplay; the sky keeps moving.
            materials[i].SetFloat("_WeatherTime", Time.unscaledTime);
        }
        // Only window fill lamps are dimmed; PowerSystem retains control of room lamps and blackout.
        for (int i = 0; i < daylight.Count; i++)
            if (daylight[i]) daylight[i].intensity = intensities[i] * Mathf.Lerp(1, .12f, strength) * Mathf.Lerp(1, .35f, finalStorm);
    }

    void OnDestroy()
    {
        foreach (var material in materials) if (material) Destroy(material);
        for (int i = 0; i < daylight.Count; i++) if (daylight[i]) daylight[i].intensity = intensities[i];
    }
}
