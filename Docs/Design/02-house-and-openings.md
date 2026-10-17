# 02 · House and openings

> **Status:** Draft (facts read from the scene) · **Owner:** unassigned · **Depends on:** nothing
> **Code / scene:** `Assets/Scenes/HouseMap.unity`, `OpeningDefinition` (not built yet)

## The house (read from `HouseMap.unity`, coordinates are (x, height, z))
- Two storeys plus a basement. Upper floor 3.3 m above ground, basement 3.2 m below. Footprint about 12 m by 10 m. Player start is just inside the front door (`MARKER_PlayerStart`).
- Rooms: entry hall, living room, kitchen, stair hall (basement entrance at its north end), basement; upstairs master bedroom, study (second bedroom), bathroom, landing.
- Exactly **one exterior door**, in the south wall of the entry hall (`COL_EntryDoor_Closed`). Interior doors (master, study, bath) open and close but never break.
- **Basement has no windows.** It is the safe room (supply shelves, workbench, fuse panel, utility tank, "shelter stash").
- 38 pickups (`PICKUP_*`): 14 on the ground and upper floors, 24 in the basement; 15 are hidden inside cabinets and drawers. Item types are in `Assets/Art/HouseInterior/stored-supplies.json`. Details in [04](04-tasks-and-supplies.md).

## The 14 windows
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

## Still to design
- [ ] Confirm the window groupings with art (open question 3).
- [ ] How the player can tell a window is "in play" before the handbook exists (Day 1): rattle only, or also an outline/hint?

## Open questions
- **Q3.** Do the day groupings of windows match what art wants (Day 1 living room and one entry window, Day 2 kitchen, back hall, master bedroom, Day 3 the rest)?
