# 04 · Tasks and supplies

> **Status:** Review · **Owner:** Ali (drafted by Claude) · **Depends on:** 01, 02, 03
> **Code:** `TaskDefinition`, task engine (not built yet); pickups extend `MapExplorer`.
> This is the main difficulty knob: re-check the workload numbers after every playtest.

## Supplies (38 pickups)
| Item | Count | Where | Used for |
|---|---|---|---|
| Water | 4 + 8 | Kitchen cabinet; basement shelves and bulk water | Supplies, score, overnight recovery |
| Food | 4 + 6 | Kitchen cabinet; basement shelves | Same |
| First aid | 1 + 2 | Bathroom medicine cabinet and vanity; basement | Health +30 per use, 2 uses per kit |
| Flashlight | 1 | Kitchen drawer | Blackout, lowers pulse in the dark |
| Batteries | 1 + 4 | Study bedside drawer; basement | Flashlight, radio |
| Radio | 1 | Ground floor | News and information tasks |
| Backpack | 1 | Master wardrobe | Go-bag task |
| Blanket | 1 | Master wardrobe | Shelter prep, stair blocking |
| Toolkit | 2 | Basement workbench drawer | Reinforce and repair |
| Repair tape | 2 | Basement workbench drawer | Repair (4 uses per roll) |
| **Towels (new)** | 6 | Bathroom vanity stack (4), basement shelf (2) | Door gap, stair blocking, wiping puddles. **Prop needed.** |
| **Plank sets (new)** | 18 | Basement workbench stack | Reinforce and brace. **Prop needed.** |

Task hints: Day 1 phone shows a location hint, Day 2 less, Day 3 only the handbook checklist.

## Decisions (proposed 2026-10-17, change freely)

### Carrying (implemented, see [09](09-architecture-and-build-order.md))
- **Pockets** (unlimited, no slowdown): flashlight, batteries, first aid, toolkit, tape, phone, handbook.
- **Armful** (one kind at a time, no sprinting): up to 6 water/food items, or up to 8 plank sets, or up to 4 towels, or the blanket. Picking up a different armful kind asks you to drop the current one.
- Fainting drops the armful only. Pockets are kept.
- The backpack goes in the pockets (Cane wears it). The radio is not carried: it is switched on where it stands.

### The shelter stash (implemented)
The existing low platform `Basement_ShelterStash` in the basement (no new marker needed). It holds about 21 items per layer. "Bring supplies to the shelter" tasks count only the items placed there. Overnight, Cane eats 1 food and drinks 1 water from the stash (see [06](06-health-and-saves.md)).

### Wiping puddles (replaces mopping)
There is no mop. With a towel in the armful, hold E on a puddle for 4 s to remove it. One towel wipes any number of puddles.

### Day 1 tuning (applied)
- First aid becomes an **optional bonus** on Day 1. The phone shows it last and it does not count against task completion.
- On Day 1 only, the toolkit and 4 plank sets are staged at the top of the basement stairs, so the trip takes about 10 s.
- Required workload: 114 s of 150 s (76%).

## Task engine (implemented 2026-10-17)
The tasks below live in `Assets/Resources/HurrycaneData/tasks.json`, and `TaskEngine` checks each task's `condition` against the live house every frame. Condition words are listed at the top of that file. A task is done for good once its condition is true. If it isn't done by the end of its last allowed phase, it shows "too late". Day 1 shows one task at a time on the phone panel; Days 2-3 show the handbook list.
- **Towels (built, placeholder):** 6 pale-blue slabs, 4 on the bathroom vanity and 2 on the floor beside the basement shelter. They're carried in the arms (up to 4). Holding E at the top of the basement stairs with 2 towels blocks them (`StairBarrier`, 4 s hold). That enables `d3_stairs` (optional, Preparation only) and `d3_flood` (appears when water starts; required).
- **Flashlight (built):** F toggles it while carried. It drains about 0.55%/s while on (about 3 minutes per pack); when empty it swaps in a battery pack from the pockets automatically. Its chip shows ON and the %.
- **Still turned off** (`"disabled": true`): wiping water (needs puddles), picking up the handbook, the door-gap towels, unplugging, utilities, and loose items.
- **Day 1 staging is built:** on a new game, the spare toolkit and 4 plank sets appear on the floor next to the basement stairs (the stack then holds 14).
- **Friction found in testing:** water and planks are both carried in the arms, so after "Take 3 water bottles" the player must drop (G) or stash the water before grabbing planks. Consider letting a few bottles fit in the pockets.

## Tasks
Phases: **P** Preparation, **D** Disaster, **A** After-Disaster. Seconds = Preparation cost (x1.5 in D and A). "Req" tasks count for completion score; "Opt" tasks count only if done.

### Day 1 · 3 windows · phone gives one task at a time, in this order
| ID | Task | Phases | s | Req | Effect on HouseState |
|---|---|---|---|---|---|
| d1_alert | Tap the government alert | P | 0 | Req | Starts the day |
| d1_radio | Turn on the radio | P D A | 5 | Req | Radio on (news lines play) |
| d1_flashlight | Take the flashlight (kitchen drawer) | P D A | 8 | Req | Flashlight in pockets |
| d1_water | Take 3 water bottles | P D A | 8 | Req | +3 water carried |
| d1_tools | Grab toolkit and planks (stair hall) | P D | 10 | Req | Toolkit + 4 plank sets |
| d1_board | Board up W01-W03 | P D | 36 | Req | W01-W03 reinforced |
| d1_door | Brace the front door | P D | 20 | Req | Door reinforced |
| d1_repair | Repair the cracked living room window | D A | 27 | Req | W01 repaired (appears after scripted crack) |
| d1_firstaid | Bonus: take the first aid kit (bathroom) | P D A | 14 | Opt | First aid kit in pockets |
| d1_wipe | Wipe up the water | A | 10 | Req | Puddles removed |

### Day 2 · 8 windows · handbook
| ID | Task | Phases | s | Req | Effect |
|---|---|---|---|---|---|
| d2_handbook | Pick up the handbook (living room table) | P D A | 5 | Req | Handbook unlocked |
| d2_tools | Fetch toolkit and planks (basement) | P D | 20 | Req | Plank sets carried |
| d2_board | Board up W04-W08 | P D | 60 | Req | W04-W08 reinforced |
| d2_doorgap | Block the front door gap with towels | P D | 12 | Req | Door break chance -10 points this storm; no puddle if the door breaks |
| d2_shelter | Bring kitchen water and food to the shelter (4 + 4) | P D A | 30 | Req | Stash +4 water, +4 food |
| d2_radio | Find batteries (study drawer) and use the radio | P D A | 12 | Req | Radio on, flashlight has spare batteries |
| d2_unplug | Unplug appliances (TV, fridge, kitchen) | P only | 12 | Req | Appliances safe |
| d2_utilities | Shut the utility valve and the fuse panel | P only | 15 | Req | Utilities off |
| d2_loose | Move 3 loose items away from windows | P D | 15 | Req | Removes +10% break chance per item |
| d2_firstaid | Take the first aid kit (if not taken on Day 1) | P D A | 14 | Opt | First aid in pockets |
| d2_repair | Repair breaks (about 2, one scripted) | D A | 54 | Req | Broken/cracked become repaired |
| d2_after | Fix leftovers and wipe puddles | A | 15 | Req | Puddles removed |

### Day 3 · 14 windows · handbook only (cannot all be done)
| ID | Task | Phases | s | Req | Effect |
|---|---|---|---|---|---|
| d3_shelter | Stock the shelter: at least 6 water and 6 food | P D A | 30 | Req | Stash topped up from basement shelves |
| d3_medical | First aid and medication to the shelter | P D A | 10 | Req | First aid in stash or pockets |
| d3_prep | Prepare the shelter: blanket, flashlight, batteries | P D A | 20 | Req | Safe room calms pulse 2x faster |
| d3_tools | Fetch toolkit, planks and tape | P D | 30 | Req | Carried |
| d3_board | Board up W09-W14 | P D | 72 | Req | W09-W14 reinforced |
| d3_upgrade | Upgrade repaired windows (about 2) | P D | 24 | Opt | Repaired become reinforced |
| d3_door | Re-brace or repair the front door | P D | 10 | Opt | Door reinforced |
| d3_stairs | Block the basement stairs with towels and the blanket | P | 20 | Opt | Flood never starts |
| d3_gobag | Pack the go-bag (backpack, water, food, first aid) | P D A | 20 | Opt | Score: counts as 2 tasks |
| d3_utilities | Shut utilities and unplug devices | P only | 22 | Opt | Utilities off |
| d3_loose | Move 4 loose items away from windows | P D | 20 | Opt | Removes +10% per item |
| d3_carried | Repair unrepaired Day 2 breaks (about 1) | P D A | 18 | Req if any | Repaired |
| d3_flood | Block the flood at the basement stairs | D (appears when the flood starts) | 38 | Req if flooding | Stops the rise |
| d3_repair | Repair new breaks (about 4) | D A | 108 | Req | Repaired |
| d3_after | Last check and cleanup | A | 10 | Req | Puddles removed |

Workload targets (task seconds as a share of Preparation plus Disaster time): Day 1 76% after tuning, Day 2 about 92% (about 105% with spill-over), Day 3 about 134% (about 148% with spill-over).

Removed on purpose: all "bucket under a leak" tasks, attic, bathtub, sandbags (replaced by towels), mop (replaced by towel wipe).

## Still to design
- [ ] Pick the actual loose items in the scene (3 near W04-W08, 4 near W09-W14) and tag them `LOOSE_*`.
- [ ] Radio news lines (one per day, about 3 lines each).

## Open questions
- **Q2. Decided:** towels and blankets instead of sandbags.
