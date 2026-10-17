# 05 · Pulse

> **Status:** Draft · **Owner:** unassigned · **Depends on:** 01, 03
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

## Still to design
- [ ] Amounts for "storm level", "darkness", "last 10 s" and "many unfinished tasks" (no numbers yet).
- [ ] Breathing key: which key, cooldown length.
- [ ] Is "nearby" for a window break the same 2 m radius used for health damage in [06](06-health-and-saves.md)?
- [ ] Keep or cut the sprint penalty.
