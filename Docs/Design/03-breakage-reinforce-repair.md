# 03 · Breakage, reinforce and repair

> **Status:** Draft · **Owner:** unassigned · **Depends on:** 02, 05, 06
> **Code:** breakable opening component (not built yet). States: out of play, unreinforced, reinforced, cracked, broken, repaired.

## Breakage
Each object is rolled once per storm, only if in play.

| State | Window D1 | D2 | D3 | Front door D1 | D2 | D3 |
|---|---|---|---|---|---|---|
| Unreinforced | 30% | 50% | 80% | 50% | 70% | 95% |
| Repaired | 15% | 15% | 15% | 30% | 30% | 30% |
| Reinforced | scripted | 10% | 10% | 20% | 20% | 20% |
| Broken, not repaired | 100% | 100% | 100% | 100% | 100% | 100% |

- The front door is deliberately the weakest point.
- Unreinforced objects can also break during Preparation as the wind rises, creating repair tasks. Reinforced objects do not break during Preparation.
- A break near Cane hurts and spikes the pulse (see [05](05-pulse.md), [06](06-health-and-saves.md)). A broken front door floods the living room.
- Broken objects stay broken into the next day. An unrepaired breach adds a flood hazard the next day.
- With 14 reinforced windows the chance none breaks is only about 23% (at most two break about 84% of the time). Success is judged by health and damage, never by a single break.
- A breaking object should creak, shake and drop dust first so the player sees the warning.

## Reinforce and repair
Reinforce = toolkit plus plank panel (**plank prop does not exist yet**). Repair = toolkit plus repair tape (limited rolls). Do not use tape to "reinforce": taping windows is a myth.

| Action | Preparation | Disaster (x1.5) |
|---|---|---|
| Reinforce a window (about 5 s hold plus walking) | 12 s | 18 s |
| Brace the front door | 20 s | 30 s |
| Repair a window | 18 s | 27 s |
| Repair the front door | 30 s | 45 s |
| Upgrade repaired window to reinforced | 12 s | 18 s |
| Fetch toolkit and planks from the basement workbench | 20 s | 30 s |

## Still to design
- [ ] When during Disaster Time each roll fires (spread evenly, random, scripted beats?).
- [ ] Break chance during Preparation for unreinforced objects (not given yet).
- [ ] "Cracked" state: is it a step before broken, and does it have its own roll?
- [ ] How many planks and tape rolls exist, and whether one fetch trip carries enough for all windows.
- [ ] What "flood hazard the next day" does (damage, slows movement, blocks a room?).
- [ ] Living room flooding from a broken front door: gameplay effect.

## Open questions
- **Q1.** Will the team add the plank prop and boarded window state? Without it reinforcement has no item or visual.
