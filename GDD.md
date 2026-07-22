# Game Design Document: GMTK Jam 2026

> **Status:** Concept locked (brainstorm, Wed July 22). Keep this SHORT. If a section doesn't earn its place in a 4-day jam, delete it.

## Overview

| | |
|---|---|
| **Theme** | Countdown |
| **Title** | Count Down Under |
| **Genre** | Score-attack action / arcade |
| **Perspective** | 2.5D side-scroller (low poly) |
| **Engine** | Unity 6000.3.15f1 (URP, new Input System) |
| **Platform** | Windows + WebGL (itch.io) |
| **Session length** | Target 5–10 min per run |
| **Deadline** | Sunday July 26, 11 AM |

## Pitch

You are Dracula on a wild all-night blood bender in the Australian outback. Drink as much blood as you can, then crawl into a coffin and sleep it off whenever you decide the party's over. Once the sun comes up, daylight starts draining your hoard, so every second you keep partying in the open is a gamble.

## Theme Interpretation

The countdown is the run's clock ticking toward sunrise. While it's dark you gather blood freely. When it hits zero the sun rises and daylight begins eating your score, fast in open light, slow in shadow. There's no forced ending: you sleep when you choose, banking whatever score you still have. The countdown turns "one more victim" into a real risk, which is the whole game.

## Core Loop

1. Hunt: move through the level looking for victims.
2. Dash into bat form (temporary) to close distance and fly; no fall damage.
3. Hit a victim to latch on and drain their blood (their health bar empties into your score).
4. Before sunrise: gather freely. After sunrise: stick to shadows to slow the score drain.
5. Decide the bender's over and sleep in a coffin to end the run and bank your score.

## Minimum Viable Product

*The version we'd submit if everything goes wrong after Thursday. Be brutal.*

- [ ] Player can: move in humanoid form, dash into bat form to fly, and drain a victim on contact
- [ ] Sunrise countdown timer visible and running
- [ ] Score = total blood gathered
- [ ] Daylight drain: after sunrise, score drains over time; standing in shadow slows it (full sun ~1 pt/sec, full shadow ~1 pt per 10 sec, tunable)
- [ ] End state: player can sleep in a coffin at any time (before or after sunrise) to end the run and bank current score
- [ ] One complete level with a few victims, shadow cover, and at least one coffin
- [ ] Title screen → game → end/results screen flow
- [ ] Sound: SFX for core actions (bat, drain/scream, burning)

## Stretch Goals (in priority order)

1. Victim health bars and varied victim types (different blood values)
2. Bat-form ability meter that recharges, forcing timing decisions
3. Pickups (blood vials / temporary boosts)
4. Shadow-rich level design: awnings, trees, rock overhangs that cast moving shade as the sun climbs
5. Background music and ambient outback/gothic environment SFX

## Mechanics

### Player

Two forms. **Humanoid** Dracula moves on the ground, slower, and can pounce on nearby victims. **Bat** is entered with a dash (Space): faster, flies, no fall damage, temporary. While flying, contact with a victim starts a blood drain. Bat form is time-limited (meter or fixed duration) and reverts to humanoid when it runs out.

### Systems

- **Sunrise countdown:** single run clock. At zero the sun rises and the daylight drain phase begins.
- **Blood / score:** draining a victim transfers their health bar into the player's score.
- **Daylight drain + shadows:** after sunrise, held score drains over time. Standing in shadow lowers the rate (full sun ~1 pt/sec down to ~1 pt per 10 sec in full shade, tunable). Creates a stay-in-the-shade sub-game.
- **Sleep to end:** the player enters a coffin at any time, before or after sunrise, to end the run and bank the score. Ending is voluntary; the drain is the pressure to stop.
- **Random start:** player spawns at a random point in the level each run for replay variety.

## Art Direction

- **Asset strategy:** source existing low-poly assets and credit them wherever possible, rather than modelling everything from scratch. Custom work only for gaps or things that need to match. A cohesive look beats any single hero asset. Keep a running credits list from day one.
- Target style: "Gothic Safari". Low-poly outback (red dirt, gum trees, corrugated shacks) meets gothic vampire (coffins, fog, moonlight). Source toward this look.
- Palette: night-time blues and purples, warm blood red as the accent, hard orange sunrise as the threat color.
- Low poly throughout; readable silhouettes over detail. 2.5D layered depth.

## Audio

- Music: one looping night-time background track (sourced and credited; check jam asset rules).
- SFX: bat flap/dash, victim scream on drain, daylight sizzle, environment ambience. Source and credit where possible.

## UI / Screens

- Title screen
- HUD: score/blood counter, **sunrise countdown**, bat-form meter, victim health bar (when draining)
- Pause
- End/results screen (score banked, or burned)
- Asset credits

## Team & Responsibilities

Day 1 = Wednesday July 22, 2–8 PM. Owner sticks with their area through the jam unless we re-balance. Feature freeze Friday 2 PM.

| Who | Role | Day 1 focus | Owns through jam |
|-----|------|-------------|------------------|
| **Yuki** | Programming lead | Player controller (2.5D ground move), form-swap on Space dash → bat flight (no fall damage); stub the sun/shadow drain (score ticks down in a "sun" zone, slower in a "shadow" zone). Get a capsule flying, landing, and losing points in the light by end of day | Core gameplay, form system, victim drain, sunrise timer, sun/shadow drain, score |
| **Marina** | Art lead | Set the target look, then source cohesive low-poly packs for Dracula, bat, victims, and outback/gothic environment; start the credits list. Custom modelling only for gaps. Hand Kyle the palette/style early | Art direction, asset sourcing + credits, any custom models/animation |
| **Kyle** | Art sourcing / materials / UI | Source art asset packs toward Marina's target look; modify and create materials so sourced assets match the art direction. Coordinate with Marina on the credits list and Mike on clean import. Then build the HUD (score, sunrise countdown, bat meter, victim health bar) and title/end screens | Asset sourcing, material creation/matching, UI screens, asset credits screen, supporting art |
| **Mike** | Tech artist / floater | Input mapping (new Input System: move, dash/Space, interact), project + build settings, WebGL export test; then own the asset import pipeline (get sourced packs in-engine clean: materials, scale, animation hookup) and assist Yuki on the drain/score system | Repo/build health, asset pipeline, tech art (lighting, shaders/materials, post-processing, VFX, the sun/shadow look), gap-filling programming and integration |
