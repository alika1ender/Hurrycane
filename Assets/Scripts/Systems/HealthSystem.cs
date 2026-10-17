using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Health outcomes (Docs/Design/06): hold H to use first aid (+30, 2 uses per kit); at 0 health Cane faints on
/// Days 1-2 (5 s fade, wakes by the master bed at 35 health, arms dropped where he fell, clock keeps running)
/// and Day 3 is game over. Damage itself comes from DayDirector (nearby breaks).
/// </summary>
[RequireComponent(typeof(DayDirector))]
public sealed class HealthSystem : MonoBehaviour
{
    GameSession session;
    DayDirector director;
    MapExplorer player;
    HealthSettings settings;
    float faintLeft, firstAidHeld;

    public bool Fainted { get; private set; }
    public float FirstAidProgress => Mathf.Clamp01(firstAidHeld / Mathf.Max(.01f, settings.firstAidHoldSeconds));
    public bool CanUseFirstAid => player && player.Inventory.Uses(ItemKind.FirstAid) > 0 && session.health < 100;

    void Awake()
    {
        session = GetComponent<GameSession>();
        director = GetComponent<DayDirector>();
        settings = director.Config.health;
    }
    void OnEnable() => GameSession.DayStarted += OnDayStarted;
    void OnDisable() => GameSession.DayStarted -= OnDayStarted;
    public void Bind(MapExplorer explorer) => player = explorer;

    void OnDayStarted(int day) { Fainted = false; faintLeft = 0; firstAidHeld = 0; }

    bool Active => director.Phase >= DayPhase.Preparation && director.Phase <= DayPhase.AfterDisaster;

    void Update()
    {
        if (!player) return;
        if (Fainted)
        {
            faintLeft -= Time.deltaTime;
            if (faintLeft <= 0) WakeUp();
            return;
        }
        if (!Active) { firstAidHeld = 0; return; }
        if (session.health <= 0) { Collapse(); return; }

        var keyboard = Keyboard.current;
        bool held = keyboard != null && keyboard.hKey.isPressed && player.enabled && !player.movementLocked
            && Cursor.lockState == CursorLockMode.Locked && CanUseFirstAid;
        if (!held) { firstAidHeld = 0; return; }
        firstAidHeld += Time.deltaTime;
        if (firstAidHeld < settings.firstAidHoldSeconds) return;
        firstAidHeld = 0;
        if (player.Inventory.ConsumeUses(ItemKind.FirstAid, 1)) session.health = Mathf.Min(100, session.health + settings.firstAidHeal);
    }

    void Collapse()
    {
        player.Inventory.DropArmful(player.transform);
        if (director.Today.faintIsGameOver) { director.GameOver(); return; }
        Fainted = true;
        faintLeft = settings.faintSeconds;
        player.enabled = false;
    }

    void WakeUp()
    {
        Fainted = false;
        session.health = settings.faintWakeHealth;
        session.pulse = director.Today.Resting(director.Phase);
        director.PlaceAtBedside();
        if (!Active) return; // the day ended while he was out; the night/next day takes over control
        player.enabled = true;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
    }
}
