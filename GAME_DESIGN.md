# Hurrycane: Game Design and Project Guide

Single source of truth for the hurricane game jam project. Read this file fully before designing or coding.
If code and this file disagree, ask before changing either. Anything under "Open questions" is NOT decided: do not assume an answer.

## How to work in this repo
- Unity **6000.5.3f1**, URP, Input System. Repo: `alika1ender/Hurrycane` (uses Git LFS: run `git lfs pull`).
- Main scene: `Assets/Scenes/HouseMap.unity`. Rebuild menu: **Hurrycane > Rebuild House Map**.
- Existing scripts (`Assets/Scripts/Interactions`): `CabinetOpenable` (E toggles doors/drawers), `MapExplorer` (first-person movement: walk 2.6 m/s, sprint 4.2 m/s, reach 2.3 m), `MapPlaytest`, `HouseInteractionTest`. Extend these instead of rewriting them.
- No game systems exist yet (timer, tasks, health, pulse, phone, save). Everything below under "Systems" is to be built.
- Keep `.meta` files with assets. Do not edit `Library/` or `Temp/`.
- Numbers in this file are starting values for playtesting. Keep them in data (ScriptableObjects/JSON), not hard-coded.

## Pitch
First-person survival game set inside one house, in the style of 60 Seconds. One hurricane hits over three days. The player (Cane) has a short, timed window each day to prepare the house, then rides out the storm, then repairs. Whatever is left unfixed carries into the next day. Theme: natural disasters. Total play time about 14 minutes.

Player character: **Cane**. A contact named **Broski** sends texts. There are no NPCs and the player never leaves the house.

## The house (read from `HouseMap.unity`, coordinates are (x, height, z))
- Two storeys plus a basement. Upper floor 3.3 m above ground, basement 3.2 m below. Footprint about 12 m by 10 m. Player start is just inside the front door (`MARKER_PlayerStart`).
- Rooms: entry hall, living room, kitchen, stair hall (basement entrance at its north end), basement; upstairs master bedroom, study (second bedroom), bathroom, landing.
- Exactly **one exterior door**, in the south wall of the entry hall (`COL_EntryDoor_Closed`). Interior doors (master, study, bath) open and close but never break.
- **Basement has no windows.** It is the safe room (supply shelves, workbench, fuse panel, utility tank, "shelter stash").
- 38 pickups (`PICKUP_*`): 14 on the ground and upper floors, 24 in the basement; 15 are hidden inside cabinets and drawers. Item types are in `Assets/Art/HouseInterior/stored-supplies.json`.

### The 14 windows
| ID | Scene object | Room and wall | Position | In play |
|---|---|---|---|---|
| W01 | `COL_Window_Glass` | Living room, south | -2.3, 1.64, 0 | Day 1 |
| W02 | `COL_Window_Glass.004` | Living room, east | 0, 1.64, -2.6 | Day 1 |
| W03 | `COL_Window_Glass.001` | Entry hall, south-west | -9.9, 1.69, 0 | Day 1 |
| W04 | `COL_Window_Glass.002` | Kitchen, north | -2.05, 1.79, -10 | Day 2 |
| W05 | `COL_Window_Glass.005` | Kitchen, east | 0, 1.79, -7.4 | Day 2 |
| W06 | `COL_Window_Glass.003` | Back hall, north | -7, 1.69, -10 | Day 2 |
| W07 | `COL_Window_Glass.007` | Master bedroom, south | -2.4, 4.94, 0 | Day 2 |
| W08 | `COL_Window_Glass.010` | Master bedroom, east | 0, 4.94, -2.6 | Day 2 |
| W09 | `COL_Window_Glass.008` | Study, north | -2.3, 4.94, -10 | Day 3 |
| W10 | `COL_Window_Glass.011` | Study, east | 0, 4.94, -7.4 | Day 3 |
| W11 | `COL_Bath_FrostedGlass` | Bathroom, south | -9.9, 5.3, 0 | Day 3 |
| W12 | `COL_Window_Glass.009` | Landing, north | -7, 4.99, -10 | Day 3 |
| W13 | `COL_Window_Glass.006` | Stair hall, west (ground) | -12, 1.69, -6.5 | Day 3 |
| W14 | `COL_Window_Glass.012` | Stair hall, west (upstairs) | -12, 4.94, -6.5 | Day 3 |

New windows per day: 3, then 5, then 6. Windows not yet "in play" are never rolled for breakage. In-play windows rattle and show on the handbook map. Reinforced windows stay reinforced until they break.

## Core loop (every day)
1. Day starts (autosave snapshot taken when control is given).
2. **Preparation Time** (timed): do tasks, reinforce, gather.
3. **Disaster Time** (timed): storm hits, breakages roll, repair and keep working.
4. **After-Disaster Time** (timed): fix leftovers.
5. Calming animation, Cane sleeps in the master bedroom, sirens wake him next morning.
Day 3 ends with the ending cutscene instead of sleep.

Phase flips happen **on the clock only**. When a timer ends, the next phase starts even if tasks are unfinished. Unfinished tasks stay available at the storm cost (x1.5 time). A few tasks are locked after Preparation (for example filling water containers).

### Opening (cutscene, no input)
Living room: TV news mentions tomorrow's hurricane, Cane waves it off and falls asleep to a match. Sirens wake him, TV text reads "Hurricane is coming". Phone notification from Broski: "Cane did you see the news, quickly get prepared." There is no reply task. Opening the phone shows the government alert; tapping it starts gameplay.
Draft alert text: "EMERGENCY ALERT: Hurricane warning in effect for your area. Landfall expected this evening. Board up or secure windows and doors. Gather water, food, medication and a flashlight. Charge your phone. Stay indoors."

## Days
| | Day 1 · Tutorial | Day 2 · Blackout | Day 3 · Survive |
|---|---|---|---|
| Preparation Time | 01:30 | 02:30 | 03:00 |
| Disaster Time | 01:00 | 01:45 | 02:30 |
| After-Disaster Time | 00:30 | 00:45 | 00:30 |
| Day total | 3:00 | 5:00 | 6:00 |
| New windows | 3 | 5 | 6 |
| Storm | Outer rain bands | Landfall, power out | Back half, flooding |
| Task source | Phone, one task and hint at a time | Handbook | Handbook |
| Can everything be done? | Easily | Barely | No |
| At zero health | Faint | Faint | **Game over** |

- Days 1 and 2 cannot be failed. Day 3 is the only place to lose.
- Day 1 has one scripted break (a living room window) to teach repair. No other reinforced object breaks on Day 1.
- Day 2: power is out from the start. The emergency handbook replaces the phone as the task source.
- Day 3: flooding comes down the basement stairs and threatens the safe room.

## HUD
- **Top center:** timer in mm:ss with the phase label: "Preparation Time", "Disaster Time", "After-Disaster Time", in that order. Last 10 seconds pulse.
- **Bottom left:** pulse (heart icon plus BPM number). Green, then yellow, then red as it rises. The heart beats at the real BPM and the number is shown so color is not the only signal.
- **Health bar** near the pulse (placement open).
- Phone and handbook are in-hand objects, not HUD.
- No separate food or water bar.

## Pulse (replaces the old excitement bar)
| Zone | BPM | Color | Effect |
|---|---|---|---|
| Calm | 60-99 | Green | None |
| Tense | 100-129 | Yellow | Light vignette, audible heartbeat |
| Panic | 130-159 | Red | Strong vignette, camera shake, task time +15% |
| Peak | 160-180 | Red | Tunnel vision, desaturation, task time +30% |

| | Day 1 | Day 2 | Day 3 |
|---|---|---|---|
| Resting level (Prep / Disaster / After) | 72 / 80 / 72 | 80 / 100 / 80 | 95 / 120 / 90 |
| Practical ceiling | 95 (always green) | 135 | 180 |
| Window break nearby | +5 | +10 | +18 |
| Front door breaks | +8 | +15 | +25 |
| Penalties active | No | No (visuals only) | Yes |

Raises pulse: storm level, breaks near Cane, thunder (+6), darkness, last 10 s of a timer, many unfinished tasks at a flip, sprinting (about +6 BPM per second, optional).
Lowers pulse: finishing a task (-6), resting in the basement safe room (-5 per s), breathing key held about 4 s (-8 per s, cooldown), flashlight on in the dark (-4), a Broski text (-3).
Pulse drifts back to the current resting level at about 3 BPM per second. No tier ever blocks a task.

## Health, fainting, saves
- Health 0-100. Glass break within 2 m: -8 (Day 2), -12 (Day 3). Door break: -10 (Day 2), -18 (Day 3). First aid kit: +30, two uses.
- **Days 1-2, health reaches 0:** screen fades about 5 s, Cane wakes in the master bedroom with 35% health, pulse reset to resting level, carried items dropped where he fell, timer keeps running.
- **Overnight recovery** (replaces a needs bar): enough water and food +50, some +30, none +10.
- **Day 3, health reaches 0:** game over. Menu offers restart from Day 1, Day 2 or Day 3.
- **Autosave** at the start of each day. Restart menu shows each snapshot as a card (day, health, reinforced windows, supplies). Restarting from an earlier day deletes later snapshots.

## Breakage
Each object is rolled once per storm, only if in play.

| State | Window D1 | D2 | D3 | Front door D1 | D2 | D3 |
|---|---|---|---|---|---|---|
| Unreinforced | 30% | 50% | 80% | 50% | 70% | 95% |
| Repaired | 15% | 15% | 15% | 30% | 30% | 30% |
| Reinforced | scripted | 10% | 10% | 20% | 20% | 20% |
| Broken, not repaired | 100% | 100% | 100% | 100% | 100% | 100% |

- The front door is deliberately the weakest point.
- Unreinforced objects can also break during Preparation as the wind rises, creating repair tasks. Reinforced objects do not break during Preparation.
- A break near Cane hurts and spikes the pulse. A broken front door floods the living room.
- Broken objects stay broken into the next day. An unrepaired breach adds a flood hazard the next day.
- With 14 reinforced windows the chance none breaks is only about 23% (at most two break about 84% of the time). Success is judged by health and damage, never by a single break.
- A breaking object should creak, shake and drop dust first so the player sees the warning.

## Reinforce, repair and durations
- Reinforce = toolkit plus plank panel (**plank prop does not exist yet**). Repair = toolkit plus repair tape (limited rolls). Do not use tape to "reinforce": taping windows is a myth.

| Action | Preparation | Disaster (x1.5) |
|---|---|---|
| Reinforce a window (about 5 s hold plus walking) | 12 s | 18 s |
| Brace the front door | 20 s | 30 s |
| Repair a window | 18 s | 27 s |
| Repair the front door | 30 s | 45 s |
| Upgrade repaired window to reinforced | 12 s | 18 s |
| Fetch toolkit and planks from the basement workbench | 20 s | 30 s |

## Phone and handbook
- **Phone:** Day 1 task source. Notifications and one-way Broski texts (no reply task). Battery drains slowly; at zero it goes dark and a message points to the handbook. The timer is on the HUD, not the phone.
- **Handbook:** physical object on the living room table, highlighted. After pickup it opens with a quick key. Illustrated pages by category (Water, Windows, Power, First aid). Shows the full current task list and a floor plan with live window and door states (in play, unreinforced, reinforced, cracked, broken, repaired). Needs no power, so a dead phone never blocks progress.

## Supplies (38 pickups)
| Item | Count | Where | Used for |
|---|---|---|---|
| Water | 4 + 8 | Kitchen cabinet; basement shelves and bulk water | Supplies, score, overnight recovery |
| Food | 4 + 6 | Kitchen cabinet; basement shelves | Same |
| First aid | 1 + 2 | Bathroom medicine cabinet and vanity; basement | Health +30 |
| Flashlight | 1 | Kitchen drawer | Blackout, lowers pulse in the dark |
| Batteries | 1 + 4 | Study bedside drawer; basement | Flashlight, radio |
| Radio | 1 | Ground floor | News and information tasks |
| Backpack | 1 | Master wardrobe | Go-bag task |
| Blanket | 1 | Master wardrobe | Shelter prep |
| Toolkit | 2 | Basement workbench drawer | Reinforce and repair |
| Repair tape | 2 | Basement workbench drawer | Repair |

Task hints: Day 1 phone shows a location hint, Day 2 less, Day 3 only the handbook checklist.

## Tasks (seconds = Preparation cost)
**Day 1 (3 windows in play; total 138 s of 150 s available)**
- Tap the government alert (start). Turn on the radio (5). Take the flashlight, kitchen drawer (8). Take 3 water bottles (8). Take the first aid kit, bathroom upstairs (14). Fetch toolkit and planks (20). Reinforce W01-W03 (36). Brace the front door (20). Repair the scripted cracked living room window, Disaster only (27). After: mop up (10).
- Recommended tuning: move the first aid task to Day 2 and shorten the planks trip, giving about 114 s (76%).

**Day 2 (8 windows in play; total 235 s of 255 s)**
- Pick up the handbook (5). Fetch toolkit and planks (20). Reinforce W04-W08 (60). Block the front door gap with sandbags (12). Bring water and food down to the basement (30). Find batteries in the study drawer and use the radio (12). Unplug appliances (12). Shut the utility valve and the fuse panel (15). Move loose items away from windows (15). Repair breaks, expected about 2 with one scripted (54). After: fix leftovers and mop (15).

**Day 3 (14 windows in play; total 442 s of 330 s available, cannot all be done)**
- Core: water and food to the basement shelter (30); first aid and medication (10); prepare the shelter with blanket, flashlight, batteries (20).
- Large: fetch toolkit, planks, tape (30); reinforce W09-W14 (72); upgrade repaired windows, about 2 (24).
- Optional: re-brace or repair the front door (10); sandbag the basement stairs (20); go-bag with backpack (20); shut utilities and unplug devices (22); move loose items (20).
- Carried: unrepaired Day 2 breaks, about 1 (18).
- Reactive in Disaster Time: block the flood at the basement stairs (38); repair new breaks, about 4 when partly prepared (108).
- After: last check and cleanup (10).

Workload targets (task seconds as a share of Preparation plus Disaster time): Day 1 about 92% (aim for 76-80%), Day 2 about 92% (about 105% with spill-over), Day 3 about 134% (about 148% with spill-over). This is the main difficulty knob; re-check after playtests.
Removed on purpose: all "bucket under a leak" tasks, attic, bathtub.

## Ending and score
After Day 3's After-Disaster timer: ending cutscene (the city in ruins seen from the house), then a score screen. Dying on Day 3 shows the restart menu, no score.

| Part | Points | Measures |
|---|---|---|
| Health remaining | 300 | Health percent at the end of Day 3 |
| Openings still intact | 250 | Share of the 14 windows and the door unbroken at the end |
| Reinforcements made | 150 | Total across the three days (maximum 15 openings) |
| Supplies left | 100 | Water, food, medical items |
| Tasks completed | 100 | Share of all tasks done |
| Damage repaired | 100 | Share of breaks repaired before the end |
| **Total** | **1000** | 900+ Prepared, 700+ Survivor, 450+ Shaken, below that Unprepared |

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
Cut first if time runs out: surge mechanic on Day 2, Broski texts after the opening, chain effects, repair minigame, wind visuals, "Did you know?" facts on the score screen.

## Assets
- Available: TV with storm warning screen (`TV_StormWarning`), sofa, radio, all supply pickups, toolkit, repair tape, fuse panel, utility tank, 31 working doors and drawers, per-room lights.
- **Missing:** plank or plywood prop and a boarded window state; cracked and broken states for all 14 windows; front door reinforced, cracked and broken states; sandbag prop; emergency handbook model and pages; phone model and notification UI; flood water for the basement stairs; rain and wind visuals on windows.

## Open questions (do not assume)
1. Will the team add the plank prop and boarded window state? Without it reinforcement has no item or visual.
2. Sandbags: add a prop, or replace with towels or furniture blocking the basement stairs?
3. Do the day groupings of windows match what art wants (Day 1 living room and one entry window, Day 2 kitchen, back hall, master bedroom, Day 3 the rest)?
4. Phone: a simple first-person prop with a UI overlay?
5. Is Day 3 flooding only via the basement stairs (no basement windows)?
6. Health bar placement on the HUD.
