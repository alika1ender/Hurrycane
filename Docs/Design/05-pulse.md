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

## Decisions (proposed 2026-10-17, change freely)
- **Storm level** is already built into the resting level per phase (the table above). No separate modifier.
- **Darkness** (Day 2-3, in a room with no light and the flashlight off): resting level +10.
- **Last 10 s of a timer:** +2 BPM per second.
- **Unfinished tasks at a phase flip:** +2 BPM per unfinished required task, at most +12, applied once.
- **Breathing:** hold **Q**. The first 4 s does nothing except a breathing sound and a slight screen dim. After that it is -8 BPM per second while held, for up to 4 s. Cooldown is 20 s. Cane cannot move while breathing.
- **"Nearby" for pulse** is 4 m (you hear and see it). Health damage still uses 2 m ([06](06-health-and-saves.md)).
- **Sprint penalty:** kept, but only on Day 3 (+6 BPM per second of sprinting). It is a data toggle per day.
- **Pulse floor and cap:** never below 60, never above the day's practical ceiling.
