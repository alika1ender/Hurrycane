# 06 · Health, fainting and saves

> **Status:** Draft · **Owner:** unassigned · **Depends on:** 01, 03
> **Code:** `HouseState` snapshots, restart menu (not built yet).

## Health
- Health 0-100. Glass break within 2 m: -8 (Day 2), -12 (Day 3). Door break: -10 (Day 2), -18 (Day 3). First aid kit: +30, two uses.
- **Overnight recovery** (replaces a needs bar): enough water and food +50, some +30, none +10.

## Fainting and game over
- **Days 1-2, health reaches 0:** screen fades about 5 s, Cane wakes in the master bedroom with 35% health, pulse reset to resting level, carried items dropped where he fell, timer keeps running.
- **Day 3, health reaches 0:** game over. Menu offers restart from Day 1, Day 2 or Day 3.

## Saves
- **Autosave** at the start of each day. Restart menu shows each snapshot as a card (day, health, reinforced windows, supplies). Restarting from an earlier day deletes later snapshots.

## Still to design
- [ ] What counts as "enough" and "some" water and food for overnight recovery (count in the basement? carried?).
- [ ] Other health damage sources: flood water, Day 1 (no damage numbers given for Day 1).
- [ ] "First aid kit: two uses": per kit, or two uses total?
- [ ] Is there a main menu / continue option, or only the restart menu after death?
