# 07 · HUD, phone and handbook

> **Status:** Review · **Owner:** Ali (drafted by Claude) · **Depends on:** 01, 04, 05, 06
> **Code:** `Assets/Scripts/UI/GameUI.cs` (screen UI), `UiKit.cs` (style and builders), `PauseMenu.cs` (pause state).

## HUD
- **Top center:** timer in mm:ss with the phase label: "Preparation Time", "Disaster Time", "After-Disaster Time", in that order. Last 10 seconds pulse.
- **Bottom left:** pulse (heart icon plus BPM number). Green, then yellow, then red as it rises. The heart beats at the real BPM and the number is shown so color is not the only signal.
- **Health bar** directly above the pulse (see Decisions).
- Phone and handbook are in-hand objects, not HUD.
- No separate food or water bar.

## Phone
Day 1 task source. Notifications and one-way Broski texts (no reply task). Battery drains slowly; at zero it goes dark and a message points to the handbook. The timer is on the HUD, not the phone.

## Handbook
Physical object on the living room table, highlighted. After pickup it opens with a quick key. Illustrated pages by category (Water, Windows, Power, First aid). Shows the full current task list and a floor plan with live window and door states (in play, unreinforced, reinforced, cracked, broken, repaired). Needs no power, so a dead phone never blocks progress.

## Screen UI (implemented 2026-10-17)
Style: **emergency broadcast**. Dark translucent rounded panels and white **Barlow Condensed** type (OFL license, in `Assets/Resources/HurrycaneUI/Fonts`). Phase colours are amber for Preparation, red for Disaster and blue for After-Disaster; green means done or healthy. It's built in code with uGUI (1920x1080 reference, scales to any screen), so the scene file is untouched. Rounded panels and rings are drawn procedurally, so no image assets are needed. Screenshots: `WalkthroughPreviews/ui/` (not committed).
- **Timer** (top centre): "DAY N · PHASE" in the phase colour, a large mm:ss, and a draining progress bar. The digits pulse red in the last 10 s.
- **Health** (bottom left): bar plus number, green, then amber below 50, then red below 25. The screen flashes red when Cane is hurt.
- **Crosshair and prompt:** a dot that turns amber on something usable, and a key cap "E" with the action ("HOLD" for hold actions). Missing items show in amber underneath ("Need: toolkit, planks"). A radial ring fills around the crosshair while holding.
- **Carried items** (bottom centre): chips for pocket items (tape shows uses), plus an amber ARMS chip with a count (for example 4/8).
- **Task panel** (top right): PHONE on Day 1 (battery %, NEXT TASK, hint). EMERGENCY HANDBOOK checklist on Days 2-3: green box = done, red = too late, plus "optional" tags.
- **Toasts:** task done, creaking warning, glass shattered, boards held. **Phase banner:** a big coloured title with one line underneath.
- **Alert:** a phone mock-up with the clock, Broski's notification, and a red EMERGENCY ALERT card. "Tap to open" pulses.
- **Night card:** fades to black, "DAY N SURVIVED", the recovery line, then "Morning is coming...".
- **Pause (Esc):** RESUME, a restart card per saved day (health, boarded, water, food) and a controls line. Time is frozen while it's open.

- **Vitals** (bottom left) now also show the pulse: a beating dot, the BPM and the zone word. Above the panel, contextual hints appear: "Q hold to breathe" (with the cooldown) and "H hold for first aid (uses)".
- **Rings:** amber for E holds, green for first aid, blue for breathing (with a caption).
- **Screens:** "YOU PASSED OUT", game over (restart cards + NEW GAME), and the final score (see 08).

Still missing: the phone and handbook as in-hand props with Tab, the handbook floor plan, and more Broski texts.

## Decisions (proposed 2026-10-17, change freely)

### Controls
| Key | Action |
|---|---|
| WASD, mouse, Shift | Move, look, sprint (existing `MapExplorer`) |
| E (tap / hold) | Interact / hold-to-use |
| Tab | Raise or lower the task source: the phone on Day 1, the handbook once picked up |
| F | Flashlight on or off |
| Q (hold) | Breathe ([05](05-pulse.md)) |
| H (hold) | Use first aid |
| G | Drop the armful |
| Esc | Pause |

### Phone (decided: prop plus UI overlay)
- Tab raises a simple phone model into the lower right of the view. The screen is a UI overlay rendered on it. Cane can keep walking but cannot interact (E) while it is raised.
- The screen shows the current task and its hint, notifications, and Broski texts.
- **Battery** starts at 40% on Day 1. It drains 0.1% per second all the time, plus 1% per second while raised. It cannot be charged. Expect it to die early on Day 2, which pushes the player to the handbook. At 0%: "Phone dead. Use the handbook."

### Handbook
- Tab raises it in both hands (it covers more of the view than the phone). Left page: the task list, grouped by category. Right page: the floor plan, ground floor and upper floor, with opening states colour-coded and also marked with an icon (colour is never the only signal).
- Locked tasks show struck through with "Too late".

### HUD
- **Health bar** (Q6, decided): a thin bar directly above the pulse readout, bottom left. It turns red and pulses below 25.
- **Interaction prompt:** centered under the crosshair. For example "Hold E · Board up window" with a radial fill. When something is missing: "Need: toolkit, planks" in grey.

### Broski texts (one-way; cut first if short on time)
| When | Text |
|---|---|
| Opening | "Cane did you see the news, quickly get prepared." |
| Day 1, first window boarded | "nice. do the door too, doors go first" |
| Day 1, night card | "u good? that was just the outer bands bro" |
| Day 2 start (if the phone is alive) | "power's out on my street too. use the handbook" |
| Day 2, night card | "landfall was crazy. stay in the basement tmrw" |
| Day 3 start | "back half is the worst part. don't go near windows" |

## Open questions
- **Q4. Decided:** first-person prop with a UI overlay.
- **Q6. Decided:** above the pulse, bottom left.
