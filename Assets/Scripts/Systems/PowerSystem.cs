using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The blackout (Docs/Design/01, 05): on days with "powerOut" the room lights go off and the ambient light drops; only
/// grey daylight through the windows remains, and the windowless basement is dark. The kitchen flashlight (F) lights
/// the way, drains, and swaps in a battery from the pockets when empty. Darkness raises the resting pulse.
/// </summary>
[RequireComponent(typeof(DayDirector))]
public sealed class PowerSystem : MonoBehaviour
{
    GameSession session;
    DayDirector director;
    MapExplorer player;
    PowerSettings settings;
    Light flashlight;
    readonly List<Light> roomLights = new List<Light>();
    Color ambientSky, ambientEquator, ambientGround;
    bool lightsRead;

    public bool PowerOut { get; private set; }
    public bool FlashlightOn => flashlight && flashlight.enabled;
    public bool HasFlashlight => player && player.Inventory.Has(ItemKind.Flashlight);
    public float Charge => session.flashlightCharge;

    /// <summary>Resting-pulse change from the dark (05): dark +10, flashlight on +6 (i.e. the flashlight is -4).</summary>
    public float RestingRise => !PowerOut ? 0 : FlashlightOn ? settings.flashlightRestingRise : settings.darknessRestingRise;

    void Awake()
    {
        session = GetComponent<GameSession>();
        director = GetComponent<DayDirector>();
        settings = director.Config.power;
    }
    void OnEnable() => GameSession.DayStarted += OnDayStarted;
    void OnDisable() => GameSession.DayStarted -= OnDayStarted;

    public void Bind(MapExplorer explorer)
    {
        player = explorer;
        flashlight = new GameObject("Flashlight").AddComponent<Light>();
        flashlight.transform.SetParent(player.viewCamera.transform, false);
        flashlight.transform.localPosition = new Vector3(.18f, -.12f, .1f);
        flashlight.type = LightType.Spot;
        flashlight.range = 16; flashlight.spotAngle = 58; flashlight.innerSpotAngle = 30;
        flashlight.intensity = 6; flashlight.color = new Color(1, .95f, .85f);
        flashlight.shadows = LightShadows.None;
        flashlight.enabled = false;
    }

    void ReadLights()
    {
        if (lightsRead) return;
        lightsRead = true;
        // Room fixtures go out; the "Window"/daylight fills stay (they stand in for daylight through the glass).
        roomLights.AddRange(FindObjectsByType<Light>(FindObjectsInactive.Include)
            .Where(l => l != flashlight && (l.name.StartsWith("Light_") || l.name.StartsWith("BF_Light") || l.name.StartsWith("BasementLight"))));
        ambientSky = RenderSettings.ambientSkyColor; ambientEquator = RenderSettings.ambientEquatorColor; ambientGround = RenderSettings.ambientGroundColor;
    }

    void OnDayStarted(int day) => SetPower(!director.Config.For(day).powerOut);

    public void SetPower(bool on)
    {
        ReadLights();
        PowerOut = !on;
        foreach (var light in roomLights) if (light) light.enabled = on;
        float f = on ? 1 : settings.ambientFactor;
        RenderSettings.ambientSkyColor = ambientSky * f;
        RenderSettings.ambientEquatorColor = ambientEquator * f;
        RenderSettings.ambientGroundColor = ambientGround * f;
    }

    void OnDestroy()
    {
        // Put the scene's ambient back so leaving Play mode never leaves the editor dark.
        if (lightsRead) { RenderSettings.ambientSkyColor = ambientSky; RenderSettings.ambientEquatorColor = ambientEquator; RenderSettings.ambientGroundColor = ambientGround; }
    }

    void Update()
    {
        if (!player || !flashlight) return;
        bool canToggle = player.enabled && Cursor.lockState == CursorLockMode.Locked;
        var keyboard = Keyboard.current;
        if (canToggle && keyboard != null && keyboard.fKey.wasPressedThisFrame) ToggleFlashlight();
        if (!HasFlashlight) { flashlight.enabled = false; return; }
        if (!flashlight.enabled) return;
        session.flashlightCharge -= settings.flashlightDrainPerSecond * Time.deltaTime;
        if (session.flashlightCharge <= 0 && !SwapBattery()) { session.flashlightCharge = 0; flashlight.enabled = false; }
        // Dim a little as it runs low.
        flashlight.intensity = Mathf.Lerp(2.5f, 6, Mathf.Clamp01(session.flashlightCharge / 25f));
    }

    /// <summary>F: switch the carried flashlight on or off (on needs charge or a spare battery pack).</summary>
    public void ToggleFlashlight()
    {
        if (!HasFlashlight || !flashlight) return;
        flashlight.enabled = !flashlight.enabled && (session.flashlightCharge > 0 || SwapBattery());
    }

    /// <summary>Uses one battery pack from the pockets to refill the flashlight.</summary>
    bool SwapBattery()
    {
        var battery = player.Inventory.Pockets.FirstOrDefault(p => p.Kind == ItemKind.Batteries);
        if (!battery) return false;
        player.Inventory.Remove(battery);
        session.flashlightCharge = 100;
        return true;
    }
}
