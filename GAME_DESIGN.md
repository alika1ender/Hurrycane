# Hurrycane: Game Design and Project Guide

Entry point for the hurricane game jam project. The design is split into modules in [`Docs/Design/`](Docs/Design/). Read this file, then the module you are working on and the modules it depends on.
If code and the design disagree, ask before changing either. Anything under "Open questions" is NOT decided: do not assume an answer.

## Pitch
First-person survival game set inside one house, in the style of 60 Seconds. One hurricane hits over three days. The player (Cane) has a short, timed window each day to prepare the house, then rides out the storm, then repairs. Whatever is left unfixed carries into the next day. Theme: natural disasters. Total play time about 14 minutes.

Player character: **Cane**. A contact named **Broski** sends texts. There are no NPCs and the player never leaves the house.

## Modules
| # | Module | Covers | Owner | Status |
|---|---|---|---|---|
| 01 | [Core loop and days](Docs/Design/01-core-loop-and-days.md) | Phases, opening cutscene, per-day timings and storm | unassigned | Draft |
| 02 | [House and openings](Docs/Design/02-house-and-openings.md) | Rooms, front door, the 14 windows and their days | unassigned | Draft |
| 03 | [Breakage, reinforce, repair](Docs/Design/03-breakage-reinforce-repair.md) | Break chances, warnings, action durations | unassigned | Draft |
| 04 | [Tasks and supplies](Docs/Design/04-tasks-and-supplies.md) | 38 pickups, task lists per day, workload targets | unassigned | Draft |
| 05 | [Pulse](Docs/Design/05-pulse.md) | BPM zones, per-day levels, raise/lower rules | unassigned | Draft |
| 06 | [Health and saves](Docs/Design/06-health-and-saves.md) | Damage, fainting, game over, autosave | unassigned | Draft |
| 07 | [HUD, phone, handbook](Docs/Design/07-hud-phone-handbook.md) | Timer, pulse/health display, task sources | unassigned | Draft |
| 08 | [Ending and score](Docs/Design/08-ending-and-score.md) | Ending cutscene, 1000-point score | unassigned | Draft |
| 09 | [Architecture and build order](Docs/Design/09-architecture-and-build-order.md) | Data model, build order, cut list | unassigned | Draft |
| 10 | [Assets](Docs/Design/10-assets.md) | Available and missing art | unassigned | Draft |

Status values: **Draft** (has gaps) → **Ready** (no open checkboxes, numbers agreed) → **Building** (someone is coding it) → **Done** (in the game and playtested).

## How to work in this repo
- Unity **6000.5.3f1**, URP, Input System. Repo: `alika1ender/Hurrycane` (uses Git LFS: run `git lfs pull`).
- Main scene: `Assets/Scenes/HouseMap.unity`. Rebuild menu: **Hurrycane > Rebuild House Map**.
- Existing scripts (`Assets/Scripts/Interactions`): `CabinetOpenable` (E toggles doors/drawers), `MapExplorer` (first-person movement: walk 2.6 m/s, sprint 4.2 m/s, reach 2.3 m), `MapPlaytest`, `HouseInteractionTest`. Extend these instead of rewriting them.
- No game systems exist yet (timer, tasks, health, pulse, phone, save).
- Keep `.meta` files with assets. Do not edit `Library/` or `Temp/`.
- Numbers in the design are starting values for playtesting. Keep them in data (ScriptableObjects/JSON), not hard-coded.

## Team workflow
1. **Pick a module.** Put your name in the Owner column above and push, so two people don't edit the same file.
2. **Work inside that module file.** Fill in its "Still to design" checklist; tick boxes as they get decided. If you change a number another module uses, update that module too and say so in your handoff entry.
3. **Before you stop** (out of credits, end of session): add an entry at the top of [`Docs/Design/HANDOFF.md`](Docs/Design/HANDOFF.md), update the Status column, commit and push.
4. **Picking up someone else's work:** `git pull`, read the newest HANDOFF entry, then the module it names.
5. **Using an AI agent:** point it at this file first ("read GAME_DESIGN.md, then Docs/Design/0X-....md, then HANDOFF.md").

## Open questions (do not assume)
Each one also appears in the module it affects.
1. Plank prop and boarded window state? → [03](Docs/Design/03-breakage-reinforce-repair.md), [10](Docs/Design/10-assets.md)
2. Sandbags: a prop, or towels/furniture blocking the basement stairs? → [04](Docs/Design/04-tasks-and-supplies.md), [10](Docs/Design/10-assets.md)
3. Do the day groupings of windows match what art wants? → [02](Docs/Design/02-house-and-openings.md)
4. Phone: a simple first-person prop with a UI overlay? → [07](Docs/Design/07-hud-phone-handbook.md)
5. Is Day 3 flooding only via the basement stairs? → [01](Docs/Design/01-core-loop-and-days.md)
6. Health bar placement on the HUD. → [07](Docs/Design/07-hud-phone-handbook.md)
