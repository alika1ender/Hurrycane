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
- **No health bars (later 2026-10-18):** both bars below are hidden; the vitals panel shows only the heart monitor. House and Cane health still count for the score and still flash the screen.
- **House + you (2026-10-18, now hidden):** the vitals panel led with a big HOUSE bar (percent; blue, then amber at 60% or less, then red at 30% or less; the screen flashes amber when the house loses health) and a thin YOU bar for Cane's health underneath (06).
- **Prompt text** under the crosshair has a black outline so it reads over bright walls and glows.
- **Centre messages:** the 180 BPM freeze ("PRESS Q TO TAKE A DEEP BREATH", 05) and live flood water ("THE WATER IS LIVE. YOU CAN'T GO IN", 01).
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

- **Blackout and flood:** an "F flashlight" hint when it's dark and the flashlight is off; the flashlight chip shows ON/%; a red "You're wading in flood water" hint; a "Water is pouring down the basement stairs!" toast; and the Preparation banner adds "The power is out." At most 3 toasts at a time, placed above the item chips.

- **Phone and handbook in hand (built, `HandheldSystem`, texts in `Resources/HurrycaneData/phone.json`):** Tab raises the handbook once carried, otherwise the phone. A simple 3D phone or book rises into view, and while raised Cane can walk but not use things.
  - **Phone screen:** clock and battery, Broski's thread (last 5) and a NEXT TASK card. When dead: "BATTERY DEAD, use the emergency handbook".
  - **Battery:** drains 0.1%/s, plus 1%/s while raised, from 40%, so it dies on Day 2 as designed.
  - **Broski texts:** arrive at day start, on the first window boarded each day, at night and when the flood starts. Each one is a toast and -3 BPM. They're lost if the phone is dead.
  - **Handbook:** a red book with cream pages. The left page is the full checklist with hints; the right page is a **live floor plan** of both floors, with every opening colour-coded by state, numbered, and a white dot for Cane.
  - **Compact panel:** on Days 2-3 it keeps showing the phone-style next task ("Pick up the handbook") until the handbook is carried, and it shows "Tab open ..." and unread messages.

- **Main menu and cutscene overlays (built):** the menu has a dark side panel with the HURRY**CANE** title, a tagline and buttons. Cutscenes get letterbox bars, speaker-tagged captions, closing eyelids, a siren wash, fades and a "Hold Space skip" hint.

- **Teaching the controls (built):**
  - **How to play card:** after the Day 1 alert, before the clock starts, a card lists every key in four groups (MOVE, DO THINGS, GEAR, GAME) with key caps, plus a one-line summary of the goal. Space or LET'S GO starts Preparation. It's also on the main menu (HOW TO PLAY) and the pause menu (CONTROLS).
  - **First-time tips:** a key-cap card at mid-left, about 6.5 s each and one at a time, the first time each control matters: tap E, hold E, full arms (G), the phone (Tab), the handbook (Tab), the dark (F), a racing heart (Q), hurt (H), the storm arriving, and Esc. Each shows once per new game.
  - **Always on:** the bottom-right key strip, the E prompt under the crosshair, and the Q/H hints above the vitals.

- **Playtest changes (2026-10-18):**
  - The bottom item chips are gone; a single "HOLDING ..." label shows only what's in your arms.
  - The task card is opaque with an amber frame that blinks hard for 3.5 s when a task arrives or its location appears, then pulses gently. It shows YOUR TASK / NOW, the task, and WHERE (or "Location in N s..."), with "It's glowing, even through walls."
  - Pulsing amber rings mark the task's targets on screen.

Still missing: real phone and book models (cubes for now), the illustrated category pages (Water, Windows, Power, First aid).

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
