# Handoff log

Newest entry on top. Write one before you stop working (out of credits, end of shift, switching tasks) so the next person, or their AI agent, can continue without asking you.

Template:

```
## YYYY-MM-DD HH:MM · <your name>
**Worked on:** <module file(s)>
**Done:** <what changed, decisions made>
**Next:** <the very next step>
**Blocked / questions:** <anything waiting on someone>
```

---

## 2026-10-18 05:00 · Claude (for krx40)
**Worked on:** a losing ending. Code: `Systems/CutsceneSystem.cs` (`PlayFailed`, `FailedRoutine`, gloom/fog, flood outside and under the door), `Systems/DayDirector.cs` (`EndGame`, `Failed`, F9/F10 editor keys and context-menu items), `Systems/DayConfig.cs` (`failedScoreBelow`), `Systems/FloodSystem.cs` (`ShowDepth`), `Systems/WindowWeather.cs` (storm sky during it), `UI/GameUI.cs` (FAILED TO SURVIVE card, red score title), `Audio/AudioDirector.cs` (its sound). Data: `failedScoreBelow: 300` in `days.json`, `"failed"` captions in `cutscenes.json`. Updated 08.
**Done:** below 300/1000 the Day 3 end plays the failed cutscene, then the card, then the score screen (rank FAILED). 300+ is unchanged. Tested in Play mode with F9 and F10: the whole flow, captions, card and score screen; no errors.
**Next:** a real low-score playthrough; tune the timing and the camera path if any shot clips a wall.
**Blocked / questions:** the front door is part of `12_Hall_Entry_Visual`, so it can't open; a separate door leaf would allow a swing-open shot. In a new game the basement water shows the live-water glow, which is bright for this shot.

## 2026-10-18 04:05 · Claude (for Ali)
**Worked on:** `TaskEngine` (`NeedItem`: the items a job needs glow with it), `days.json` (`locationDelaySeconds` 9). Doc 04.
**Done and tested in Play mode:** "Reinforce the living room windows" without tools targets both windows, both toolkits and the plank stack (`WalkthroughPreviews/ui/37_tools_glow.png`).
**Next:** Ali's hands-on playtest. **Blocked / questions:** nothing committed yet.

## 2026-10-18 03:45 · Claude (for Ali)
**Worked on:** `GameUI` (health bars hidden, smaller vitals panel), `HouseHealth` (`gameOverAtZero` off), `days.json` (days 1:30 / 1:30 / 3:00, `locationDelaySeconds` 11, flood starts at 20 s and rises over 50 s, Day 1 crack at 8 s), `tasks.json` (lists cut to fit, workbench hints). Docs 01, 04, 06, 07.
**Done and tested in Play mode:** prep timers 50/50/95 s, the Day 3 reveal counts down from 11 s, the HUD has no bars (`WalkthroughPreviews/ui/36_no_bars.png`), the toolkit is on the bench top (the hint now says so).
**Next:** time a real run of each day by hand. Day 2's 50 s prep with 6 tasks in the dark is the tightest.
**Blocked / questions:** nothing committed yet.

## 2026-10-18 03:15 · Claude (for Ali)
**Worked on:** Ali's next playtest list. Code: new `Systems/HouseHealth.cs`; `PulseSystem` (180 BPM freeze), `FloodSystem` (live water blocks entry instead of hurting, arcs, no wading damage), `PowerSystem` (room lamps aimed at the floor), `MapExplorer` (the basement stair rail is ignored when aiming), `TaskEngine` (the breaker glows while live water blocks Cane), `GameUI` (HOUSE/YOU bars, prompt outline, centre messages), `Score` (House health 150 + Your health 150), `DayDirector.GameOver(reason)`, `DayConfig`/`days.json` (`house` block, `freezeBpm`/`freezeSeconds`/`freezeRearm`). Docs 01, 05, 06, 07, 08.
**Done and tested in Play mode** (captures `WalkthroughPreviews/ui/30`-`35`): even room lighting; the HOUSE bar drops to 84% with two broken windows; the Day 3 freeze triggers at 180 with the centre message; stepping into live water puts Cane back, shows the message and targets the breaker; the breaker can be used from the stairs; arcs are visible. The door/drawer test passes 31/31.
**Balance reasoning:** the house bar makes repairs during and after the storm pay off visibly, and Day 3's "house at 0%" loss needs about 8 broken windows on top of the broken door, a full flood and puddles, so it punishes skipping preparation, not bad luck. Live water is a gate (do the breaker task) rather than a damage trap.
**Next:** a hands-on playtest of Day 3: is the freeze reachable often enough, does the house loss threshold feel fair, and are the arcs readable with the flashlight. `FloodSettings.damagePerSecond`/`shockDamage` are now unused.
**Blocked / questions:** none. Nothing committed yet (Ali wants to play first); the 02:20 batch is also uncommitted.

## 2026-10-18 02:20 · Claude (for Ali)
**Worked on:** Ali's playtest requests. Code: `TaskEngine` (one task at a time, urgent interrupts, the supply-box override, location delay, targets), new `TaskGlow` + `Resources/TaskGlow.shader`, `PickupHighlight` (shinier), `GameUI` (single-task card with a blinking frame, beacons, the holding label, the critical monitor), `PulseSystem` (critical: slower and blur), `MapExplorer.pulseSpeedFactor`, `GameplaySetup` (batteries hidden), `BreakableOpening` (the "Reinforce" prompt). Data: `tasks.json` (reinforce tasks split by room, battery tasks changed), `days.json` (`locationDelaySeconds` 0/7/7, no flashlight drain, `criticalBpm`), `openings.json` (10 tape uses per roll, 32 planks). Updated 04, 05 and 07.
**Done and tested in Play mode** (captures `WalkthroughPreviews/ui/25`-`28`): Day 1 shows the task and location at once; Day 2 reveals the location after 7 s; targets glow through walls with an on-screen ring; carrying water switches to "put it in the supply box" with the box glowing; critical pulse at 175 BPM slows to 65%, blurs and blinks the monitor. The door/drawer test passes 31/31.
**Next:** another hands-on playtest. Check whether one task at a time is too slow on Day 3 (11 tasks), and whether 145 BPM for critical feels right.

## 2026-10-18 00:40 · Claude (for krx40)
**Worked on:** a 17-item playtest list. Code: `Openings/OpeningVisuals.cs`, `Openings/PlankStack.cs`, `Interactions/PickupHighlight.cs` (new) + `Resources/PickupOutline.shader` (new), `Interactions/GameplaySetup.cs`, `GeneratedProps.cs`, `MapExplorer.cs`, `Pickup.cs`, `PlayerInventory.cs` (new `ItemKind.Phone`, appended), `Systems/FloodSystem.cs`, `PowerSystem.cs`, `PulseSystem.cs`, `PuddleSystem.cs`, `WindowWeather.cs`, `CutsceneSystem.cs`, `DayDirector.cs`, `HandheldSystem.cs`, `DayConfig.cs`, `UI/GameUI.cs`, `Audio/AudioDirector.cs` (one call per thunder clap); data in `openings.json`, `tasks.json`, `days.json`.
**Done:**
- Broken windows: hole, jagged glass, crack web, floor shards. They were invisible before because WindowWeather's opaque sky pane sits 3.5 cm inside the glass; all state FX now sit in front of it. Broken door: hole, splinters, split. Cracked: crack web.
- Pickup outline (near/focused), red-pulse screen border, lightning through windows (flashes with AudioDirector's thunder), menu tornado, red-only siren.
- Day 2 "blue screen": in the blackout only the blue window fill lamps were lit; they turn neutral grey while the power is out.
- One plank per press; Shift while carrying; windows 50% faster, door = window speed.
- Survival guide is Day 1's first task; guide + new phone pickup staged on the coffee table whenever missing (old saves too); Tab needs one of them. (`StageHandbook` had been dropped in 1b6d5a5.)
- Puddles/shards measure the real floor; puddles never on stairs, door makes one puddle.
- Flood water more visible; live water (main breaker on) shocks 10 HP / 2 s.
- Shelter supply box moved at runtime to free floor beside the master bed; the master bedroom is the safe room.
- Ending: linear fog (4–34 m) and a much denser ruined street.
- Compiled in Unity 6000.6; menu checked in Play mode. Not yet playtested through the days.
**Next:** playtest Days 1–3 for the new visuals, the shelter placement and the shock timing.
**Blocked / questions:** shocks repeat every 2 s while in live water (one-off instead?). The "Hall_ShelterDirection" sign may still point to the basement.

## 2026-10-17 23:58 · Claude (for Ali)
**Worked on:** teaching the controls. Code: `UI/GameUI.cs` (controls card, tips, menu and pause buttons), `Systems/DayDirector.cs` (two-step alert, `SkipIntro`) and `Systems/GameSession.cs` (`NewGameStarted`). Updated 07 and 09.
**Done:** a How to play card after the Day 1 alert (every key, grouped, with the goal), which can be reopened from the menu and pause screens. One-time key tips when each control first matters. Tested in Play mode (captures `WalkthroughPreviews/ui/23_how_to_play`, `24_tip`); the door/drawer test passes 31/31.
**Note:** this checkout also has someone's uncommitted work from about 23:17-23:30 (`TVBroadcast.cs`, `PhoneAlert.png` and its use in `GameUI.BuildAlert`, plus edits to `CutsceneSystem`, `Switchable` and `GameplaySetup`). I didn't touch it; it compiles together with this work.
**Next:** playtest notes from Ali.

## 2026-10-17 20:35 · Claude (for Ali)
**Worked on:** the main menu, the opening cutscene and the ending cutscene. Code: `Systems/CutsceneSystem.cs`, plus menu and cutscene overlays in `UI/GameUI.cs`, and flow changes in `DayDirector`/`GameSession` (new phases `Menu`, `Opening`, `Ending`). Data: `cutscenes.json` (captions) and `showMainMenu` in `days.json`. Updated 01, 07, 08, 09 and 10.
**Done:**
- Main menu: Continue (latest day) / New Game / Quit, over a living room shot.
- Opening (~37 s): the sofa, TV news, "Tomorrow. It's always tomorrow.", the match, dozing off, the next morning, sirens, the storm warning, the phone buzz, then the alert. Hold Space to skip.
- Ending (~23 s): up from the basement and through the house to W01, morning light, a placeholder ruined street outside, "bro u alive??", then the score.
- Tested in Play mode with Game view captures (`WalkthroughPreviews/ui/20_menu`, `21_opening_*`, `22_ending_*`): the whole flow menu → opening → alert → (fast-forward) → ending → score; skipping drops Cane at the front-door spawn. The door/drawer test passes 31/31.
**Next:** everything in the design is now in the game. What's left: a hands-on playtest and tuning (timings, workload, break chances), sound (none yet: sirens, rain, glass, heartbeat, TV), and real art for the placeholders (planks, towels, puddles, handbook, phone, flood water, the ending street).
**Blocked / questions:** unchanged (water and planks in the arms; Day 1 door chance; cracked-boarded repair rule; Q3).

## 2026-10-17 20:00 · Claude (for Ali)
**Worked on:** the last two disabled tasks, plus the phone and handbook in hand. Code: `Interactions/LooseItem.cs`, `Systems/HandheldSystem.cs`, a handbook prop in `GeneratedProps`, and GameUI phone/handbook screens. Data: `loose.json`, `phone.json`; in `tasks.json`, `d2_handbook` and `d2_loose` are on and `d3_loose` is new. Updated 04, 07 and 09.
**Done:**
- 7 loose items picked in the scene (3 for Day 2, 4 for Day 3). Each adds +10% break chance to its window until moved (hold E).
- The handbook on the coffee table. Tab raises the phone (battery, Broski thread, next task) or the handbook (checklist plus a live floor plan of both floors).
- Phone battery drain and Broski texts on triggers (toast, -3 BPM).
- Everything new is in snapshots.
- Tested in Play mode: phone and handbook screens (screenshots in `WalkthroughPreviews/ui/17_phone`, `18_handbook`), the Broski trigger, the loose-item move and chance bonus, the Day 2 tasks, and a restart keeping moved items. Door/drawer test passes 31/31. **No task in the design is disabled any more.**
**Next:** hands-on playtest of all three days. Remaining build: the opening TV cutscene and the ending cutscene (01, 08), a main menu (New Game / Continue), and real art for the placeholders (planks, towels, puddles, handbook, phone, flood water).
**Blocked / questions:** unchanged (water and planks in the arms; Day 1 door chance; cracked-boarded repair rule; Q3).

## 2026-10-17 19:40 · Claude (for Ali)
**Worked on:** the remaining disabled tasks. Code: `Systems/PuddleSystem.cs`, `Openings/Puddle.cs`, `Openings/DoorGap.cs`, `Interactions/Switchable.cs`, plus wiring in `GameplaySetup`, `DayDirector`, `TaskEngine`, `HouseState` and `MapExplorer`. Data: `puddles` and `doorGap` blocks in `days.json`; in `tasks.json`, `d1_wipe`, `d2_doorgap`, `d2_unplug` and `d2_utilities` are now on, and `d2_wipe`, `d3_wipe` and `d3_utilities` are new. Updated 03, 04 and 09.
**Done:** puddles from broken windows/door (they slow you, re-leak each morning while unrepaired, and are wiped with a towel). The Day 2 door-gap towel lowers the door's break chance and prevents door puddles. Unplugging the TV, fridge and kettle, and switching off the breaker and valve. Everything is in snapshots. Tested in Play mode: spawn, slowdown, the gap vs no gap, wiping, switches, and a Day 2 save/restart keeping puddles and switches. The door/drawer test passes 31/31. Only `d2_handbook` and `d2_loose` remain disabled.
**Next:** hands-on playtest. Then: loose items (pick 3 + 4 scene props and tag them), the handbook and phone as props (enables `d2_handbook`), the opening and ending cutscenes, a main menu.
**Blocked / questions:** unchanged (water and planks in the arms; Day 1 door chance; cracked-boarded repair rule; Q3). The puddle look is a placeholder dark disc; art should supply a wet decal.

## 2026-10-17 19:10 · Claude (for Ali)
**Worked on:** the Day 2 blackout and Day 3 basement flooding. Code: `Systems/PowerSystem.cs`, `Systems/FloodSystem.cs`, `Openings/StairBarrier.cs`, `Interactions/GeneratedProps.cs`, plus changes to `DayConfig`, `DayDirector`, `TaskEngine`, `PulseSystem`, `HouseState`, `GameUI` and `MapExplorer`. Data: `power` and `flood` blocks and `powerOut`/`floods` per day in `days.json`; `d3_stairs` and `d3_flood` added to `tasks.json`. Updated 01, 04, 05, 06, 07, 09 and 10.
**Done:**
- Blackout on Days 2-3 (room lights off, dim ambient). The flashlight works on F, drains, and swaps in battery packs. Darkness raises the resting pulse (+10, or +6 with the flashlight on).
- Day 3 flood per 01: down the stairs, 40 s (10 s earlier per ground-floor breach), rising to 40 cm over 90 s. Wading hurts and slows you, and the safe room stops calming. 6 placeholder towels; 2 towels at the top of the stairs block it. Score: reactive tasks that never appeared no longer count as missed.
- **Fixed a serious bug:** every break hurt Cane wherever he stood (`Collider.ClosestPoint` doesn't work on the non-convex proxies). Injury, pulse and toast distances now use `BreakableOpening.DistanceTo`.
- Tested in Play mode: Day 2 lights off and the darkness pulse, the flashlight, the near/far injury check, the flood timing, depth, damage, speed and blocking. Screens are in `WalkthroughPreviews/ui/` (not committed).
**Next:** hands-on playtest (F, Q, H, the towels, the flood). Then: puddles (unlocks wiping tasks), the door-gap towels on Day 2, the opening and ending cutscenes, a main menu, and the phone and handbook props.
**Blocked / questions:** the same open points as before (water and planks in the arms; the Day 1 door chance; the cracked-boarded repair rule; Q3). New for art: lamps keep glowing in the blackout, and the flood water needs a proper shader.

## 2026-10-17 18:35 · Claude (for Ali)
**Worked on:** (1) the real game UI, (2) pulse, health outcomes, mornings and the score. Code: `UI/GameUI.cs`, `UI/UiKit.cs`, `Systems/PulseSystem.cs`, `Systems/HealthSystem.cs`, `Systems/Score.cs`; the old IMGUI `Hud.cs` is deleted. Font: Barlow Condensed (OFL) in `Resources/HurrycaneUI/Fonts`. Updated 01, 05, 06, 07, 08, 09 and 10.
**Done:**
- UI in the emergency broadcast style (Ali's pick), built with uGUI in code. It covers the timer, vitals (health and pulse), prompt and hold rings, item chips, phone/handbook panel, toasts, phase banner, damage flash, alert phone, night card, pause, "passed out", game over and score screens. Checked in Game view captures (`WalkthroughPreviews/ui/`, not committed).
- Pulse per 05 (Q breathing, vignette and shake, Day 3 hold penalties); first aid on H; fainting on Days 1-2 (wake by the master bed); Day 3 game over; wake by the bed each morning; the 1000-point score with rank and fact.
- Fixed a real bug: strikes still in their warning could land after the day changed, and broken windows took repeat hits, so health drained. In-flight strikes are now cancelled at night, and broken openings ignore hits.
- Tested in Play mode (clock sped up): faint and wake, the Day 2/3 mornings, Panic penalties, game over, restart from Day 3, the score. The door/drawer test passes 31/31.
**Next:** a hands-on playtest with a real keyboard (Q, H, the feel of the timings). Then: the Day 2 blackout (lights off, flashlight), Day 3 flooding (01), puddles and towels (unlocks 4 tasks), the ending cutscene (08), a main menu, and the phone and handbook as props.
**Blocked / questions:** the same three open points as before (water and planks both in the arms; the Day 1 door chance; the cracked-boarded repair rule), plus Q3 for art.

## 2026-10-17 08:20 · Claude (for Ali)
**Worked on:** build steps 5-6: `Systems/DayDirector`, `TaskEngine`, `DayConfig`, `UI/Hud`, and `days.json` / `tasks.json`. Updated 01, 03, 04, 07 and 09.
**Done:** Day 1 can be played start to finish. Alert, then the timed phases, with the task list on the phone panel and Day 1 tools staged by the stairs. The W01 crack, the repair task, the night card with recovery, and Day 2 with its autosave all work. Days 2-3 run with the handbook list, storm rolls, Preparation breaks and injuries. Tested in Play mode with the clock sped up. The door/drawer test (31/31) and the window flow test still pass.
**Next:** a hands-on playtest of Day 1 for feel and timing (no one has played it with real input yet). Then: pulse (05), fainting and Day 3 game over (06), waking in the master bedroom, puddles and towels (unlocks 4 disabled tasks), and the ending and score (08).
**Blocked / questions:** (1) Water and planks are both carried in the arms, which slows down Day 1 (see 04). (2) For a reinforced door on Day 1, the 03 table says 20% but the rule says 0; I used 0. (3) The 03 "repairing a cracked boarded window" quirk is still open.

## 2026-10-17 08:00 · Claude (for Ali)
**Worked on:** build step 3, saves and restart (`Assets/Scripts/Systems/`, `Assets/Scripts/UI/PauseMenu.cs`), plus wider placeholder planks. Updated 06 and 09.
**Done:** `GameSession` starts Day 1 and autosaves. Esc opens a pause menu with a Restart card per saved day (day, health, boarded count, water, food). Restarting reloads the scene and restores openings, every item's place (world, pockets, arms, stash), tape uses, the plank stack, the radio, health and the player position. Later days' saves are deleted. Tested in Play mode: Day 1 to Day 2 autosave, a restart from Day 2 restored that exact state, a restart from Day 1 reset everything, and the door/drawer test passes 31/31.**Next:** steps 5-6, the task engine (`TaskDefinition` data from 04, listening to `MapExplorer.Used`, `BreakableOpening.StateChanged` and inventory/stash changes), the phase timer calling `GameSession.StartDay` each morning, the HUD timer, and break rolls with `Strike()` during Disaster Time.
**Blocked / questions:** still open from 03: should repairing a cracked *boarded* window keep the boards (back to reinforced) instead of becoming "repaired"? The code currently follows the doc as written.

## 2026-10-17 07:40 · Claude (for Ali)
**Worked on:** build step 4, breakable windows and door (`Assets/Scripts/Openings/`, `Resources/HurrycaneData/openings.json`), plus 03, 09 and 10.
**Done:** all 15 openings have the six states. Board up / brace (toolkit plus a plank set from the new workbench stack), repair (toolkit plus tape; 4 uses per roll, the door takes 2), upgrade repaired to boarded, storm `Hit()` following the 03 table, and `Strike()` with a 3 s shake warning. Day 1 openings (W01-W03 and the door) are in play; the others show no prompt. Placeholder visuals were checked in screenshots (`WalkthroughPreviews/step4/`, not committed).
Tested in Play mode: the full flow for windows and door, out-of-play handling, tape and plank use, the strike timing, all 37 pickups, and the door/drawer test (31/31).
**Next:** step 3 (`HouseState` plus snapshots, which can use `BreakableOpening.SetState`) or step 5/6 (task engine, phase timer, break rolls). Ali should decide the "repairing a cracked boarded window" quirk in 03.
**Blocked / questions:** none. For timed tests in the background, set `Application.runInBackground = true` (see 09).

## 2026-10-17 07:10 · Claude (for Ali)
**Worked on:** build steps 1-2 (code in `Assets/Scripts/Interactions/`), plus 02, 04 and 09.
**Done:**
- Step 1: all 14 windows and the front door are confirmed in `HouseMap.unity`, and their positions match the design table.
- Step 2: pickup, carry (pockets and armful), drop (G), hold-to-use with a progress bar, the radio switch, and the shelter stash. Everything is wired at runtime by `GameplaySetup`, and the scene file is untouched. A summary of each script is in [09](09-architecture-and-build-order.md), "What exists in code".
- Tested in Play mode through the Unity MCP: all 37 pickups are reachable, carry rules, stash and hold timing work, and the door/drawer test passes 31/31.
**Next:** build step 3, `HouseState` and start-of-day snapshots. Or step 4, `BreakableOpening` on the 14 `COL_Window_*` proxies and the door, implementing `IInteractable` (reinforce needs a toolkit plus a plank set, a 5 s hold). Plank sets and towels have item kinds but no props yet: placeholder cubes named `PICKUP_Plank_*` / `PICKUP_Towel_*` would be picked up automatically.
**Blocked / questions:** Unity throttles itself when its window is in the background, so the MCP bridge and request-file triggers don't respond until someone clicks into the editor.

## 2026-10-17 · Claude (for Ali)
**Worked on:** all modules 01-10.
**Done:** Ali decided Q1 (simple boards), Q2 (towels instead of sandbags), Q4 (phone prop plus overlay) and Q5 (flooding via the stairs only). I filled every "Still to design" gap with a proposed answer under a "Decisions (proposed)" heading. Main additions:
- 03: the "cracked" state (a hit on a reinforced opening only cracks it), when rolls fire, 18 plank sets, 8 tape uses, hold times, puddles.
- 04: a full task table with IDs, phases, required/optional and effects; carry rules; the shelter stash; Day 1 tuning applied (76%).
- 01: night card and wake-up, Preparation-only tasks, Day 3 flood timing.
- 05, 06, 07, 08, 09: pulse numbers, overnight recovery, keys (Tab, F, Q, H, G), battery, Broski texts, score details, folder layout.
All modules are now **Review**.
**Next:** the team reads the "Decisions (proposed)" sections and changes anything they disagree with. Then start coding, build order step 1-2 in [09](09-architecture-and-build-order.md): confirm the openings in the scene and add pickup/carry/hold-to-use to `MapExplorer`.
**Blocked / questions:** Q3 (window groupings) needs the art person. The towel and plank props are needed before reinforce and repair can look right (cubes work as placeholders).

## 2026-10-17 · Claude (for alika1ender)
**Worked on:** split `GAME_DESIGN.md` into module files in `Docs/Design/`.
**Done:** content moved without changing any numbers or decisions. Each module got a "Still to design" checklist of gaps found while splitting, and its open questions.
**Next:** assign an owner to each module in the status table in `GAME_DESIGN.md`, then answer the open questions Q1-Q6.
**Blocked / questions:** none.
