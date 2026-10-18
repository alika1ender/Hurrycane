# 05 · Pulse

> **Status:** Review · **Owner:** Ali (drafted by Claude) · **Depends on:** 01, 03
> **Code:** pulse system (not built yet). Replaces the old excitement bar.

| Zone | BPM | Color | Effect |
|---|---|---|---|
| Calm | 60-99 | Green | None |
| Tense | 100-129 | Yellow | Light vignette, audible heartbeat |
| Panic | 130-159 | Red | Strong vignette, camera shake, task time +15% |
| Peak | 160-180 | Red | Tunnel vision, desaturation, task time +30% |

| | Day 1 | Day 2 | Day 3 |
|---|---|---|---|
| Resting level (Prep / Disaster / After) | 72 / 80 / 72 | 80 / 100 / 80 | 95 / 120 / 90 |
| Practical ceiling | 95 (always green) | 135 | 180 |
| Window break nearby | +5 | +10 | +18 |
| Front door breaks | +8 | +15 | +25 |
| Penalties active | No | No (visuals only) | Yes |

**Raises pulse:** storm level, breaks near Cane, thunder (+6), darkness, last 10 s of a timer, many unfinished tasks at a flip, sprinting (about +6 BPM per second, optional).

**Lowers pulse:** finishing a task (-6), resting in the basement safe room (-5 per s), breathing key held about 4 s (-8 per s, cooldown), flashlight on in the dark (-4), a Broski text (-3).

Pulse drifts back to the current resting level at about 3 BPM per second. No tier ever blocks a task.

## Implemented (2026-10-17)
`Assets/Scripts/Systems/PulseSystem.cs`; numbers are in `days.json` (`pulse` block, plus `resting*`, `pulseCeiling`, `*BreakSpike`, `pulseVisuals`, `pulsePenalties` and `sprintRaisesPulse` per day).
- **Built:** drift to the resting level, the window-break spike within 4 m (half for a crack), the front-door break spike, the last 10 s rise, the unfinished-task rise at a phase flip, the Day 3 sprint rise, the task-done drop, the basement safe-room drop, and breathing on Q (4 s wind-up, then -8/s for up to 4 s, 20 s cooldown, no walking). Panic/Peak make holds +15%/+30% slower on Day 3. Vignette from Tense up, desaturation at Peak and camera shake at Panic/Peak, from Day 2.
- **HUD:** a beating dot at the real BPM, the number and the zone word (07).
- **Darkness (built):** during the blackout the resting level is +10 with the flashlight off and +6 with it on (so the flashlight is -4).
- **Not built yet:** thunder and Broski-text effects (no thunder or text system exists yet).
- **Untested by hand:** Q breathing and H first aid were checked only for logic, not with a real keyboard.

## Critical heart rate (built 2026-10-18)
At `criticalBpm` (145, so in practice only on Day 3) and above: walking slows to `criticalSpeedFactor` (65%), vision blurs (a Gaussian depth of field fading in), and the heart monitor blinks red with "CRITICAL". Values are in the `days.json` `pulse` block.

## Freeze at 180 BPM (built 2026-10-18)
At `freezeBpm` (180, Day 3's ceiling) Cane locks up for `freezeSeconds` (5 s). He can't walk or use anything, and the middle of the screen says "YOUR HEART IS AT 180 BPM. YOU CAN'T MOVE. PRESS Q TO TAKE A DEEP BREATH". Q works during the freeze even on cooldown. The freeze arms again only once the pulse drops below 165 (`freezeRearm` 15), so it can't chain.

## Decisions (proposed 2026-10-17, change freely)
- **Storm level** is already built into the resting level per phase (the table above). No separate modifier.
- **Darkness** (Day 2-3, in a room with no light and the flashlight off): resting level +10.
- **Last 10 s of a timer:** +2 BPM per second.
- **Unfinished tasks at a phase flip:** +2 BPM per unfinished required task, at most +12, applied once.
- **Breathing:** hold **Q**. The first 4 s does nothing except a breathing sound and a slight screen dim. After that it is -8 BPM per second while held, for up to 4 s. Cooldown is 20 s. Cane cannot move while breathing.
- **"Nearby" for pulse** is 4 m (you hear and see it). Health damage still uses 2 m ([06](06-health-and-saves.md)).
- **Sprint penalty:** kept, but only on Day 3 (+6 BPM per second of sprinting). It is a data toggle per day.
- **Pulse floor and cap:** never below 60, never above the day's practical ceiling.
