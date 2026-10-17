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
            // Lightning light: shines in through this window, down onto the floor.
            var bolt = new GameObject("Lightning_" + opening.Id).AddComponent<Light>();
            bolt.transform.SetParent(transform, true);
            bolt.transform.SetPositionAndRotation(b.center + inward * .15f, Quaternion.LookRotation(inward + Vector3.down * .45f));
            bolt.type = LightType.Spot; bolt.range = 8; bolt.spotAngle = 110; bolt.innerSpotAngle = 60;
            bolt.color = new Color(.88f, .9f, 1f); bolt.shadows = LightShadows.None; bolt.intensity = 0; bolt.enabled = false;
            lightning.Add(bolt);
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
        float flash = Lightning(phase);
        for (int i = 0; i < lightning.Count; i++)
        {
            lightning[i].enabled = flash > .01f;
            lightning[i].intensity = flash * 7;
        }
        for (int i = 0; i < panes.Count; i++)
        {
            panes[i].SetActive(visible);
            materials[i].SetFloat("_Flash", flash);
            materials[i].SetFloat("_Storm", strength);
            materials[i].SetFloat("_FinalStorm", finalStorm);
            // Alert screens and menus can pause gameplay; the sky keeps moving.
            materials[i].SetFloat("_WeatherTime", Time.unscaledTime);
        }
        // Only window fill lamps are dimmed; PowerSystem retains control of room lamps and blackout.
        for (int i = 0; i < daylight.Count; i++)
            if (daylight[i]) daylight[i].intensity = intensities[i] * Mathf.Lerp(1, .12f, strength) * Mathf.Lerp(1, .35f, finalStorm);
    }

    /// <summary>
    /// Lightning during the hurricane: every 5-12 s in Disaster Time (3-7 s in the final storm), now and then after it.
    /// Each strike is a quick double flicker lighting all the windows at once. Returns the flash brightness 0..1.
    /// </summary>
    float Lightning(DayPhase phase)
    {
        float now = Time.time;
        // With sound, AudioDirector calls Flash() with each thunder clap so the two line up; this timer is the fallback.
        if (!AudioDirector.Instance)
        {
            bool storm = phase == DayPhase.Disaster || phase == DayPhase.AfterDisaster;
            if (!storm) nextStrike = -1;
            else
            {
                if (nextStrike < 0) nextStrike = now + Random.Range(2f, 5f);
                if (now >= nextStrike)
                {
                    strikeStarted = now; strikePower = 1;
                    nextStrike = now + (phase == DayPhase.AfterDisaster ? Random.Range(12f, 25f) : finalStorm > .5f ? Random.Range(3f, 7f) : Random.Range(5f, 12f));
                }
            }
        }
        float t = now - strikeStarted;
        if (t < 0 || t > .7f) return 0;
        float first = Mathf.Exp(-t * 14), second = t > .16f ? Mathf.Exp(-(t - .16f) * 9) * .8f : 0;
        return Mathf.Clamp01(Mathf.Max(first, second)) * strikePower;
    }

    /// <summary>A lightning strike now (0..1 brightness; distant thunder flashes dimmer). Called by AudioDirector.</summary>
    public static void Flash(float power = 1)
    {
        if (!current) return;
        current.strikeStarted = Time.time;
        current.strikePower = Mathf.Clamp01(power);
    }

    static WindowWeather current;
    void Awake() => current = this;
    readonly List<Light> lightning = new List<Light>();
    float nextStrike = -1, strikeStarted = -10, strikePower = 1;

    void OnDestroy()
    {
        foreach (var material in materials) if (material) Destroy(material);
        for (int i = 0; i < daylight.Count; i++) if (daylight[i]) daylight[i].intensity = intensities[i];
    }
}
