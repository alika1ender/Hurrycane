# 03 · Breakage, reinforce and repair

> **Status:** Review · **Owner:** Ali (drafted by Claude) · **Depends on:** 02, 05, 06
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
Reinforce = toolkit plus plank set. Repair = toolkit plus repair tape (limited uses). Do not use tape to "reinforce": taping windows is a myth.

| Action | Preparation | Disaster (x1.5) |
|---|---|---|
| Reinforce a window (about 5 s hold plus walking) | 12 s | 18 s |
| Brace the front door | 20 s | 30 s |
| Repair a window | 18 s | 27 s |
| Repair the front door | 30 s | 45 s |
| Upgrade repaired window to reinforced | 12 s | 18 s |
| Fetch toolkit and planks from the basement workbench | 20 s | 30 s |

The seconds above are workload estimates (hold plus walking). The actual hold times are under "Hold times" below.

## Decisions (proposed 2026-10-17, change freely)

### What a "hit" does
A successful break roll is a **hit**. What happens depends on the state:

| State when hit | Becomes | Injury and pulse spike? | Rain gets in? |
|---|---|---|---|
| Unreinforced | Broken | Yes | Yes |
| Repaired | Broken | Yes | Yes |
| Reinforced | **Cracked** (boards hold, glass behind them cracks) | No, pulse spike only | No |
| Cracked | Broken | Yes | Yes |
| Broken | Stays broken | No | Yes |

So reinforcing helps twice: a lower chance, and a softer result. A cracked object rolls at the **unreinforced** chance in the next storm. For scoring, cracked counts as intact.

The Day 1 scripted break turns W01 **cracked**, whatever its state, 20 s into Disaster Time. This teaches repair without hurting Cane.

### When rolls fire
- **Disaster Time:** each in-play object gets a random moment between 15% and 85% of the phase. Hits are at least 6 s apart; push later ones back if needed.
- **Warning:** 3 s before a hit, the object creaks, shakes and drops dust. Only hits get a warning; misses are silent.
- **Preparation:** in the last 30 s of Preparation, each unreinforced in-play object rolls at a quarter of its Disaster chance (Day 2 window 12.5%, Day 3 window 20%, door 17.5% / 23.75%). Day 1 has no Preparation breaks.

### Repair
- Tape fixes a cracked or broken object, which becomes **repaired**. Repaired can be upgraded to reinforced with a plank set.
- Repairing the front door uses 2 tape uses.

### Supplies of planks and tape
- **Planks** are in sets: one set boards one window or braces the door. There are 18 sets in the basement workbench stack (15 openings plus 3 spare). Cane can carry up to 8 sets at once in his arms, which stops him sprinting. One trip per day is enough if he plans it.
- **Repair tape:** 2 rolls with 4 uses each, so 8 repairs in the whole game. Expected breaks: about 1 on Day 1, 2 on Day 2 and 5 on Day 3, so tape runs out only if the player prepared badly.
- **Toolkit:** must be in the inventory to reinforce, brace or repair. The second toolkit is a spare, so losing one when fainting (items drop) doesn't softlock.

### Hold times (the real interaction length)
| Action | Hold (Prep) | Hold (storm x1.5) |
|---|---|---|
| Reinforce a window / upgrade | 5 s | 7.5 s |
| Brace the front door | 8 s | 12 s |
| Repair a window | 8 s | 12 s |
| Repair the front door | 12 s | 18 s |

Pulse task penalties (+15% / +30%) stack on top. Letting go of E keeps the progress for 5 s, then it resets.

### Hazards from damage
- **Unrepaired broken window:** a rain puddle forms below it (1.5 m radius). Walking speed in the puddle is 60%, and it stays into the next day until mopped (see [04](04-tasks-and-supplies.md), towel wipe).
- **Broken front door:** puddles cover the entry hall and living room. On Day 3 the basement flood starts the moment Disaster Time begins (see [01](01-core-loop-and-days.md)).
- **Loose items:** each loose item left near an in-play window adds +10% to that window's break chance (it becomes a projectile). This is the only chain effect.

## Open questions
- **Q1. Decided:** simple boards. 2-3 plain plank boxes are nailed across the window frame, and the door brace is 2 diagonal planks. See [10](10-assets.md).
