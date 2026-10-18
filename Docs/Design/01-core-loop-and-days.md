# 01 · Core loop and days

> **Status:** Review · **Owner:** Ali (drafted by Claude) · **Depends on:** 02, 04, 05, 06
> **Code:** `PhaseTimer`, `DayConfig` (not built yet)

## Pitch (short)
First-person survival game set inside one house, in the style of 60 Seconds. One hurricane hits over three days. Cane has a short, timed window each day to prepare the house, then rides out the storm, then repairs. Whatever is left unfixed carries into the next day. Total play time about 6 minutes (shortened 2026-10-18).

## Core loop (every day)
1. Day starts (autosave snapshot taken when control is given).
2. **Preparation Time** (timed): do tasks, reinforce, gather.
3. **Disaster Time** (timed): storm hits, breakages roll, repair and keep working.
4. **After-Disaster Time** (timed): fix leftovers.
5. Calming animation, Cane sleeps in the master bedroom, sirens wake him next morning.

Day 3 ends with the ending cutscene instead of sleep (see [08](08-ending-and-score.md)).

Phase flips happen **on the clock only**. When a timer ends, the next phase starts even if tasks are unfinished. Unfinished tasks stay available at the storm cost (x1.5 time). A few tasks are locked after Preparation (for example filling water containers).

## Opening (cutscene, no input)
Living room: TV news mentions tomorrow's hurricane, Cane waves it off and falls asleep to a match. Sirens wake him, TV text reads "Hurricane is coming". Phone notification from Broski: "Cane did you see the news, quickly get prepared." There is no reply task. Opening the phone shows the government alert; tapping it starts gameplay.

Draft alert text: "EMERGENCY ALERT: Hurricane warning in effect for your area. Landfall expected this evening. Board up or secure windows and doors. Gather water, food, medication and a flashlight. Charge your phone. Stay indoors."

## Days
| | Day 1 · Tutorial | Day 2 · Blackout | Day 3 · Survive |
|---|---|---|---|
| Preparation Time | 00:50 | 00:50 | 01:35 |
| Disaster Time | 00:25 | 00:25 | 01:00 |
| After-Disaster Time | 00:15 | 00:15 | 00:25 |
| Day total | 1:30 | 1:30 | 3:00 |
| New windows | 3 | 5 | 6 |
| Storm | Outer rain bands | Landfall, power out | Back half, flooding |
| Task source | Phone, one task and hint at a time | Handbook | Handbook |
| Can everything be done? | Easily | Barely | No |
| At zero health | Faint | Faint | **Game over** |

- Days 1 and 2 cannot be failed. Day 3 is the only place to lose.
- Day 1 has one scripted break (a living room window) to teach repair. No other reinforced object breaks on Day 1.
- Day 2: power is out from the start. The emergency handbook replaces the phone as the task source.
- Day 3: flooding comes down the basement stairs and threatens the safe room.

## Implemented (2026-10-17)
`Assets/Scripts/Systems/DayDirector.cs`, tuning in `Resources/HurrycaneData/days.json`. The day runs: Day 1 alert (phone overlay; click or Space starts the clock), Preparation → Disaster → After-Disaster flipping on the clock only, then the night card (6 s) with overnight recovery, then the next day starts and autosaves. Day 3 ends on a placeholder "storm has passed" card (ending and score are not built yet). Esc pauses the clock. From Day 2 on, Cane wakes standing beside the master bed (before the autosave).
- **Blackout (built):** Days 2-3 (`powerOut` in `days.json`) switch off the 12 room lights and cut the ambient light to 30%. Grey daylight through the windows remains, and the windowless basement is dark. See `PowerSystem` and the flashlight in 04/05.
- **Day 3 flooding (built, as designed below):** `FloodSystem` plus `StairBarrier` at the top of the basement stairs. The water is a murky placeholder plane that rises over the basement floor. Tested: an early start from a breach, the rise to 40 cm, 0.5/s damage and 70% speed while wading, and a stop when blocked.
- **Main menu (built):** the scene opens on a menu over a still view of the living room: CONTINUE (latest saved day), NEW GAME (replaces saves) and QUIT. "Play again" and game-over "New game" skip the menu. Turn it off with `showMainMenu` in `days.json`.
- **Opening cutscene (built, `CutsceneSystem`, about 37 s, captions in `Resources/HurrycaneData/cutscenes.json`):** a letterboxed first-person view from the sofa. The TV news warns about tomorrow's storm, Cane waves it off ("Tomorrow. It's always tomorrow."), flips to the match (green screen), blinks and dozes off, then black, "The next morning", sirens (red/blue wash), the TV storm warning, and he looks at the buzzing phone, which leads into the alert. Hold Space to skip; a single press once it's been seen. It plays on every Day 1 start (new game or restart from Day 1), and gameplay begins at the front-door spawn.
Not built yet: and the Preparation-only locks for unplug/utilities (those tasks don't exist yet).

## Decisions (proposed 2026-10-17, change freely)
### End of day and sleep
- When After-Disaster Time ends, control is taken away. The screen fades to black over 2 s. A **night card** shows for about 6 s: "Day N survived", the overnight recovery result ("Water and food: +50 health"), and a one-line Broski text if any are left.
- Sirens fade in over the black screen, then Cane wakes in the master bedroom (first-person, lying down to standing, about 3 s). Control returns and the day's autosave is taken.
- Any key skips the night card after 2 s. The wake-up cannot be skipped (it is short and places the player).

### Opening cutscene
- About 40 s. The first time, it can be skipped by holding Space for 1 s. Restarting from a snapshot never replays it. Restarting from Day 1 replays it, skippable at once.

### Tasks that lock after Preparation
Rule: a task is **Preparation-only** when doing it during the storm makes no sense in the story. Current list:
- Day 2: "Shut the utility valve and the fuse panel", "Unplug appliances".
- Day 3: "Shut utilities and unplug devices".
- The old example "filling water containers" is dropped: no such task exists, and the bathtub was removed.

Locked tasks show as greyed out and struck through on the handbook ("Too late"). They count as not done for scoring.

### Day 3 flooding (stairs only, decided)
- Water only enters the basement down the stairs from the stair hall. There are no basement windows and no ground-floor flood mesh beyond puddles.
- **Start:** 20 s into Day 3 Disaster Time. Each unrepaired ground-floor breach (W01-W06, W13 or the front door) makes it start 6 s earlier, down to the moment Disaster Time begins.
- **Rise:** the basement water level rises from 0 to 40 cm over 50 s, then stops (both shortened 2026-10-18 with the shorter day).
- **Live water (built 2026-10-18):** while the main breaker (`Basement_FusePanel`, beside the basement stairs) is on, the flood water is electrified. Blue-white arcs crackle over it with a flickering blue light. It does no damage, but Cane can't step into it: he is put back where he last stood dry, with a "ZAP!" toast, the centre message "THE WATER IS LIVE. YOU CAN'T GO IN. Switch off the main breaker", and the breaker glowing through walls for 8 s. The breaker can be reached from the stairs (the stair rail no longer blocks aiming). If the water rises under him he can still walk out.
- **Blocking:** towels and blankets at the top of the stairs (Prep task, 20 s) mean the flood never starts. Blocking during the flood (Disaster task, 38 s) stops the rise at the current level. Water already in the basement stays.
- **Effects at 20 cm or more:** the safe room no longer lowers pulse; standing in the water costs 0.5 health per second; walking speed in the water is 70%.
- Supplies on the low basement shelves are not destroyed (keeps scoring simple).
