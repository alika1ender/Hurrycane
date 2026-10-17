# 09 · Architecture and build order

> **Status:** Review · **Owner:** Ali (drafted by Claude) · **Depends on:** all modules
> Numbers everywhere are starting values for playtesting. Keep them in data (ScriptableObjects/JSON), not hard-coded.

## Data model (suggested)
- `HouseState`: state of every window and the door, supplies, phone battery, health, pulse. Snapshotted at the start of each day.
- `OpeningDefinition`: id, scene object, room, first day in play.
- `TaskDefinition`: id, text, allowed phases, base seconds, storm multiplier, effect on HouseState.
- `DayConfig`: phase lengths, break chances, pulse ranges, new openings, task list, scripted events.
- `PhaseTimer`: clock-only phase flips.

## Build order
1. ~~Open `HouseMap.unity`, confirm the 14 window objects and the front door.~~ **Done 2026-10-17**, see [02](02-house-and-openings.md).
2. ~~Extend `MapExplorer` with pickup, carry and hold-to-use, using the `PICKUP_*` objects.~~ **Done 2026-10-17**, see "What exists in code" below.
3. `HouseState` with start-of-day snapshots and the restart menu.
4. Windows and door as reusable breakable objects with the states: out of play, unreinforced, reinforced, cracked, broken, repaired.
5. Task engine with phase windows and durations; phone and handbook UI.
6. Phase timer, HUD, pulse system.
7. Day 1 fully playable, then Day 2 (blackout, handbook), then Day 3 and the ending.

**Cut first if time runs out:** surge mechanic on Day 2, Broski texts after the opening, chain effects, repair minigame, wind visuals, "Did you know?" facts on the score screen.

## What exists in code (keep this up to date)
All files are in `Assets/Scripts/Interactions/`.
- `IInteractable`: anything usable with E. It has `Prompt`, `HoldSeconds` (0 = tap), `Unavailable(user)` (the reason it can't be used, or null) and `Use(user)`. New interactive things (windows, door, valve, puddles) implement it.
- `MapExplorer`: movement plus E handling. Tap targets fire on press; hold targets fill a bar, and progress is kept for 5 s after letting go. `holdTimeMultiplier` is the hook for the storm (x1.5) and pulse penalties. `StepUse(down, pressedThisFrame, dt)` lets tests drive E without a keyboard. The static `MapExplorer.Used` event fires after every use, for the task engine. Sprinting is off while carrying an armful. G drops the armful.
- `PlayerInventory`: pockets and armful, with the rules from [04](04-tasks-and-supplies.md) (6 supplies / 8 plank sets / 4 towels / 1 blanket, one group at a time). Taken items are deactivated, not destroyed.
- `Pickup`: one per `PICKUP_*`. The kind comes from the object name.
- `RadioSwitch`: `PICKUP_Radio` stays in place and toggles on and off (decided: the radio is not carried).
- `ShelterStash`: on `Basement_ShelterStash`. Hold E for 1.5 s with water, food or the blanket to store them in rows on the platform. `Count(kind)` feeds overnight recovery and the score.
- `InteractionProxy`: put on an invisible `COL_*` proxy to forward E to the real object. The stash needs it; the windows and the door will too.
- `GameplaySetup`: adds all of the above **at runtime** when the scene loads (pickups get a fitted BoxCollider). The scene file is not changed, so **Hurrycane > Rebuild House Map** can't wipe gameplay. Don't add these components to the scene by hand.
- The aim ray prefers a pickup up to 0.45 m behind the first thing hit (items in open cabinets and on shelves behind their collision boxes), unless a closed door or drawer is in the way.
- Tested 2026-10-17 in Play mode: all 37 pickups can be taken by aiming (with cabinets open), carry limits work, drop and stash placement work, hold/resume/x1.5 timing works, and the door/drawer test still passes 31/31.

## Decisions (proposed 2026-10-17, change freely)
### Folder layout
```
Assets/Scripts/Interactions/   existing: MapExplorer, CabinetOpenable, tests
Assets/Scripts/Systems/        PhaseTimer, HouseState, TaskEngine, PulseSystem, HealthSystem, SaveSystem
Assets/Scripts/Openings/       BreakableOpening (windows + door), BreakScheduler
Assets/Scripts/UI/             Hud, PhoneUI, HandbookUI, InteractionPrompt, Menus
Assets/Data/Days/              Day1.asset, Day2.asset, Day3.asset   (DayConfig)
Assets/Data/Tasks/             one TaskDefinition asset per task ID from module 04
Assets/Data/Openings/          W01-W14 + Door (OpeningDefinition)
```
- One scene (`HouseMap.unity`) plus a small `MainMenu.unity`.
- Task IDs in [04](04-tasks-and-supplies.md) (`d1_radio` and so on) are the asset names. Keep them stable, because saves reference them.

### Cut-list items that were never described
- **Surge mechanic (Day 2):** dropped. "Unplug appliances" stays as a plain task.
- **Chain effects:** reduced to one rule, loose items near windows (+10% break chance, [03](03-breakage-reinforce-repair.md)). Everything else is dropped.
- **Repair minigame:** dropped. Repair is a hold.
