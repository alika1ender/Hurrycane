# 09 · Architecture and build order

> **Status:** Draft · **Owner:** unassigned · **Depends on:** all modules
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

## Still to design
- [ ] Folder layout for new scripts and data assets (e.g. `Assets/Scripts/Systems`, `Assets/Data/Days`).
- [ ] "Surge mechanic on Day 2" and "chain effects" are listed as cuttable but never described. Define or drop them.
