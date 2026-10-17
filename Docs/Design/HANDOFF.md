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
