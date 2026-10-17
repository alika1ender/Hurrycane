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
1. Open `HouseMap.unity`, confirm the 14 window objects and the front door.
2. Extend `MapExplorer` with pickup, carry and hold-to-use, using the `PICKUP_*` objects.
3. `HouseState` with start-of-day snapshots and the restart menu.
4. Windows and door as reusable breakable objects with the states: out of play, unreinforced, reinforced, cracked, broken, repaired.
5. Task engine with phase windows and durations; phone and handbook UI.
6. Phase timer, HUD, pulse system.
7. Day 1 fully playable, then Day 2 (blackout, handbook), then Day 3 and the ending.

**Cut first if time runs out:** surge mechanic on Day 2, Broski texts after the opening, chain effects, repair minigame, wind visuals, "Did you know?" facts on the score screen.

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
