# 06 · Health, fainting and saves

> **Status:** Review · **Owner:** Ali (drafted by Claude) · **Depends on:** 01, 03
> **Code:** `HouseState` snapshots, restart menu (not built yet).

## Health
- Health 0-100. Glass break within 2 m: -8 (Day 2), -12 (Day 3). Door break: -10 (Day 2), -18 (Day 3). First aid kit: +30, two uses.
- **Overnight recovery** (replaces a needs bar): enough water and food +50, some +30, none +10.

## Fainting and game over
- **Days 1-2, health reaches 0:** screen fades about 5 s, Cane wakes in the master bedroom with 35% health, pulse reset to resting level, carried items dropped where he fell, timer keeps running.
- **Day 3, health reaches 0:** game over. Menu offers restart from Day 1, Day 2 or Day 3.

## Health outcomes (implemented 2026-10-17)
`Assets/Scripts/Systems/HealthSystem.cs`; numbers are in `days.json` (`health` block, plus `faintIsGameOver` per day).
- **First aid:** hold H for 1.5 s for +30. Each kit has 2 uses, shown on its inventory chip. A hint appears below 70 health.
- **Faint (Days 1-2):** a "YOU PASSED OUT" fade for 5 s. The armful drops where Cane fell, he wakes by the master bed at 35 health with the pulse at resting, and the clock keeps running.
- **Game over (Day 3):** at 0 health, or if Day 3 ends at 0, a red screen offers a Restart card per saved day plus NEW GAME. No score.
- **Flood water (Day 3):** 0.5 health per second while wading at 20 cm or deeper.
- **Injury distance:** measured to the window's box. A bug where every break hurt Cane wherever he was is fixed; see 09.
- Score totals (tasks, hits, openings ever boarded) are saved in each snapshot, so restarts keep them right. Snapshots also keep the flashlight charge and whether the stairs are blocked.

## Saves
**Implemented 2026-10-17** (`Assets/Scripts/Systems/`): `HouseState` snapshot, `SaveSystem` (`persistentDataPath/saves/day{N}.json`), `GameSession` (current day, `StartDay`, `RestartFromDay`) and an Esc pause menu with a Restart card per saved day (drawn by `GameUI`). A snapshot stores: every opening state, where every pickup is (world spot and parent, pockets, arms, stash; tape and first aid uses; generated plank sets), the plank stack count, the radio, health/pulse/battery, score totals, and the player position. Entering the scene counts as a new game (it deletes old saves) until the main menu exists. The phone battery is still a plain number (it doesn't drain yet).

- **Autosave** at the start of each day. Restart menu shows each snapshot as a card (day, health, reinforced windows, supplies). Restarting from an earlier day deletes later snapshots.

## Decisions (proposed 2026-10-17, change freely)
- **Overnight recovery:** at night Cane uses 1 water and 1 food, taken from the shelter stash first and then from what he is carrying. Both available = "enough" (+50). Only one of them = "some" (+30). Neither = +10. What he uses is gone (it lowers "Supplies left" in the score).
- **Day 1 has no health damage at all.** It is the tutorial.
- **Flood water (Day 3):** 0.5 health per second while standing in basement water that is 20 cm deep or more ([01](01-core-loop-and-days.md)).
- **First aid:** each of the 3 kits has 2 uses (+30 each), so 6 uses in the game. Use it by holding **H** for 1.5 s. It is stored in pockets.
- **Health bar** sits directly above the pulse readout, bottom left ([07](07-hud-phone-handbook.md)).
- **Menus:** the main menu has New Game, Continue (latest snapshot) and Quit. The pause menu (Esc) has Resume, Restart from a day (the snapshot cards) and Quit to menu. The game-over screen shows the snapshot cards only.
- **Save format:** one JSON file per snapshot in `Application.persistentDataPath/saves/day{N}.json`.
