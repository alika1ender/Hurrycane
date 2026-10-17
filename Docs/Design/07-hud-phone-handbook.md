# 07 · HUD, phone and handbook

> **Status:** Draft · **Owner:** unassigned · **Depends on:** 01, 04, 05, 06
> **Code:** HUD canvas, phone and handbook UI (not built yet).

## HUD
- **Top center:** timer in mm:ss with the phase label: "Preparation Time", "Disaster Time", "After-Disaster Time", in that order. Last 10 seconds pulse.
- **Bottom left:** pulse (heart icon plus BPM number). Green, then yellow, then red as it rises. The heart beats at the real BPM and the number is shown so color is not the only signal.
- **Health bar** near the pulse (placement open).
- Phone and handbook are in-hand objects, not HUD.
- No separate food or water bar.

## Phone
Day 1 task source. Notifications and one-way Broski texts (no reply task). Battery drains slowly; at zero it goes dark and a message points to the handbook. The timer is on the HUD, not the phone.

## Handbook
Physical object on the living room table, highlighted. After pickup it opens with a quick key. Illustrated pages by category (Water, Windows, Power, First aid). Shows the full current task list and a floor plan with live window and door states (in play, unreinforced, reinforced, cracked, broken, repaired). Needs no power, so a dead phone never blocks progress.

## Still to design
- [ ] Phone battery: starting charge, drain rate, can it be charged before the blackout?
- [ ] Broski text list: what he says and when (cut first if time runs out).
- [ ] Keys for phone and handbook.
- [ ] Interaction prompt style (e.g. "Hold E to reinforce" with a progress ring).

## Open questions
- **Q4.** Phone: a simple first-person prop with a UI overlay?
- **Q6.** Health bar placement on the HUD.
