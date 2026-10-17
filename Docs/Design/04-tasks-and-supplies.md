# 04 · Tasks and supplies

> **Status:** Draft · **Owner:** unassigned · **Depends on:** 01, 02, 03
> **Code:** `TaskDefinition`, task engine (not built yet); pickups extend `MapExplorer`.
> This is the main difficulty knob: re-check the workload numbers after every playtest.

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

Workload targets (task seconds as a share of Preparation plus Disaster time): Day 1 about 92% (aim for 76-80%), Day 2 about 92% (about 105% with spill-over), Day 3 about 134% (about 148% with spill-over).

Removed on purpose: all "bucket under a leak" tasks, attic, bathtub.

## Still to design
- [ ] Decide on the Day 1 tuning recommendation (move first aid to Day 2?).
- [ ] For every task: allowed phases, effect on `HouseState`, and whether it locks after Preparation.
- [ ] Carry rules: how many items Cane can hold, and whether carrying slows him.
- [ ] Mop-up / cleanup tasks: what the player physically does.
- [ ] Day 2 "move loose items": which objects, and are they interactable yet?

## Open questions
- **Q2.** Sandbags: add a prop, or replace with towels or furniture blocking the basement stairs?
