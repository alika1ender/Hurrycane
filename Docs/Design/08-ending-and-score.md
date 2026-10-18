# 08 · Ending and score

> **Status:** Review · **Owner:** Ali (drafted by Claude) · **Depends on:** 03, 04, 06
> **Code:** ending cutscene, score screen (not built yet).

After Day 3's After-Disaster timer: ending cutscene (the city in ruins seen from the house), then a score screen. Dying on Day 3 shows the restart menu, no score.

| Part | Points | Measures |
|---|---|---|
| House health | 150 | House health percent at the end of Day 3 (06; added 2026-10-18) |
| Your health | 150 | Cane's health percent at the end of Day 3 |
| Openings still intact | 250 | Share of the 14 windows and the door unbroken at the end |
| Reinforcements made | 150 | Total across the three days (maximum 15 openings) |
| Supplies left | 100 | Water, food, medical items |
| Tasks completed | 100 | Share of all tasks done |
| Damage repaired | 100 | Share of breaks repaired before the end |
| **Total** | **1000** | 900+ Prepared, 700+ Survivor, 450+ Shaken, below that Unprepared; below 300 the failed ending plays and the rank is Failed |

## Implemented (2026-10-17)
`Assets/Scripts/Systems/Score.cs`, shown by `GameUI`. When Day 3's After-Disaster timer ends (and Cane is alive), the score screen opens: "THE STORM HAS PASSED", one row per part (points, out-of, and the raw numbers), the total out of 1000, the rank in colour, the rank line, a "Did you know?" fact, and PLAY AGAIN (a new game).
- Supplies = water + food (carried + stash) + first aid uses; 16 or more gives full points. Tasks = required tasks done / required, over all days. Reinforcements = openings ever boarded (max 15). Damage repaired = 1 - (cracked or broken now / storm hits); 100 if nothing was hit.
- **Ending cutscene (built, `CutsceneSystem`, about 23 s):** at the foot of the basement stairs ("The wind has stopped."), then up and through the stair hall, entry hall and living room to W01 ("Morning. Day four."), in full daylight with the room lights still off. W01's glass turns clear (boards stay if boarded). Outside is a **placeholder ruined street**: mud, standing water, a fallen tree, a broken fence, scattered boards, and roofless houses across the road, lit by a morning sun. Broski: "bro u alive??", then fade to the score. The street should be replaced with real art.

- **Failed ending (built 2026-10-18):** a final score below `failedScoreBelow` (300 of 1000, in `days.json`) plays the losing cutscene instead (`CutsceneSystem.PlayFailed`, kind `"failed"`, about 23 s, captions under `"failed"` in `cutscenes.json`). Power stays out, a cold dark fog, storm sky still in the windows. The living room's window ("The storm is over."), the top of the flooded basement stairs ("But the house didn't hold."), a stagger with a stumble to the front door ("Neither did you."), which is smashed in with water seeping under it (the door is part of the hall mesh, so it can't swing open), Broski: "bro?? answer me", "bro please", then the view tips and sinks, eyes close, black. Then a full-screen **FAILED TO SURVIVE** card (score, a fact; Space or 7 s), then the score screen with a red title and the rank **FAILED** ("The house didn't hold, and neither did you."). Audio: no music or city sirens, gusts, a slowing heartbeat, the phone buzzing, a thud. A Day 3 faint is still the separate game over.
- Editor test keys: **F9** plays the failed ending, **F10** the normal one, from any phase (also on `DayDirector`'s context menu).
## Decisions (proposed 2026-10-17, change freely)
- **Supplies left** = water + food + first aid uses, in the shelter stash plus carried, at the end of Day 3. Full 100 points at 16 or more; it scales linearly below that. Hoarding everything is not needed.
- **Tasks completed** = done tasks / required tasks across all days (optional tasks done count as bonus, capped at 100%).
- **Ending cutscene (about 20 s):** morning light. Cane walks up from the basement, and the camera moves through the living room to the south window (W01, boarded or not, as the player left it). Through the window, the street in ruins: fallen trees, roofs torn off, standing water. Sirens far away. A Broski text: "bro u alive??" Fade to the score.
- The cutscene is the same for every rank. Only the score screen changes (rank title plus one line, for example Prepared: "The house held. So did you.").
- **"Did you know?"** (one random line on the score screen; cut first if short on time):
  - Taping windows does not stop them breaking. Use boards or storm shutters.
  - Store at least one gallon (about 4 litres) of water per person per day, for several days.
  - Never run a generator indoors or in a garage. Carbon monoxide kills.
  - Stay away from windows during the storm. Shelter in an interior room.
  - Storm surge and flooding, not wind, cause most hurricane deaths.

  Fact-check these against the official guidance from FEMA or NOAA (ready.gov) before shipping.
