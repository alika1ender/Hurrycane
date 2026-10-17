# 01 · Core loop and days

> **Status:** Draft · **Owner:** unassigned · **Depends on:** 02, 04, 05, 06
> **Code:** `PhaseTimer`, `DayConfig` (not built yet)

## Pitch (short)
First-person survival game set inside one house, in the style of 60 Seconds. One hurricane hits over three days. Cane has a short, timed window each day to prepare the house, then rides out the storm, then repairs. Whatever is left unfixed carries into the next day. Total play time about 14 minutes.

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

## Still to design
- [ ] Calming animation and sleep transition: length, skippable or not.
- [ ] When exactly is the opening cutscene skippable (replays after restart?).
- [ ] Full list of tasks that lock after Preparation (only "filling water containers" is named, and it is not in any task list yet).
- [ ] Day 3 flooding: how fast it rises, what happens if it reaches the safe room. See open question 5 below.

## Open questions
- **Q5.** Is Day 3 flooding only via the basement stairs (no basement windows)?
