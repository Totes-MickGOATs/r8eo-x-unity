# Project Milestones

Detailed milestone kernels for R8EO-X phases 2–5. Part of [Project Status](./project-status.md).

---

## Phase 2: Gameplay — Milestone Kernels

### M2.1: "Hot Lap" — Solo Time Trial

**Play it:** Pick up the controller. Countdown 3-2-1-GO. Drive laps around a closed circuit. See your lap times on-screen. Beat your best.

**Acceptance:**
- [ ] Race countdown sequence (3-2-1-GO) with audio/visual cues
- [ ] Lap timing with checkpoint validation (no corner-cutting)
- [ ] Live HUD: current lap time, best lap, lap counter
- [ ] Finish condition: N laps completed → results screen with lap breakdown
- [ ] Ghost replay of personal best lap (stretch)

**Systems:** Vehicle, Input, Camera, Track (checkpoints), RaceManager (new), UI (HUD + results)

### M2.2: "Pack Racing" — Multi-Car Grid Start

**Play it:** Line up on a starting grid with 3+ AI opponents. Green light. Race door-to-door for position. See standings update in real time.

**Acceptance:**
- [ ] Grid start with staggered positions and countdown
- [ ] AI drivers follow racing lines, avoid obstacles, compete for position
- [ ] Live position tracking (P1, P2, P3...) on HUD
- [ ] Race finish: all cars ranked by completion time
- [ ] Basic contact physics — no clipping through AI cars

**Systems:** Vehicle, Input, Camera, Track (racing line, spawn points), RaceManager, AI (new), UI

### M2.3: "Career Start" — Session Progression

**Play it:** Pick a car from a garage. Enter a series of 3 races. Earn points per race. See a championship standings table after each event. Win the series.

**Acceptance:**
- [ ] Car selection screen with at least 2 distinct vehicles
- [ ] Multi-race series with points system
- [ ] Championship standings persisted across races in a session
- [ ] Season results screen: champion crowned
- [ ] Save/load session progress (stretch)

**Systems:** Vehicle, Input, Camera, Track, RaceManager, AI, UI, GameManager (new), Progression (new)

### Phase 2 Task Backlog

1. Race Manager — lap timing, checkpoints, race state machine
2. Game Manager — scene loading, pause, game state transitions
3. Lap system — checkpoint triggers, lap counting, split times
4. Results screen — lap times, best lap, race position display
5. AI driver — basic waypoint following for opponent cars
6. Track layout tools — checkpoint placement, racing line, spawn points

---

## Phase 3: Content — Milestone Kernels

### M3.1: "Backyard Bash" — First Real Track

**Acceptance:**
- [ ] One complete, textured track with mixed surfaces (asphalt, dirt, gravel)
- [ ] At least 2 jump features with landing physics
- [ ] Motor audio reactive to RPM and throttle
- [ ] Particle effects: dust/dirt roost, tire smoke on hard braking

### M3.2: "Track Day Variety" — Multiple Venues

**Acceptance:**
- [ ] 3 playable tracks with distinct layouts and characters
- [ ] Track selection screen with preview images
- [ ] Consistent checkpoint/timing integration across all tracks

### M3.3: "Show Car" — Vehicle Variety

**Acceptance:**
- [ ] 3 vehicle models with unique meshes and paint
- [ ] Distinct physics profiles per vehicle (ScriptableObject configs)
- [ ] Vehicle preview in selection screen with stat bars

---

## Phase 4: Polish — Milestone Kernels

### M4.1: "Living Room Ready" — Couch-Friendly UX

**Acceptance:**
- [ ] Full gamepad menu navigation (D-pad, A/B confirm/back)
- [ ] Settings screen: graphics quality, resolution, audio sliders, control remapping
- [ ] Settings persist across sessions
- [ ] Consistent UI theme: fonts, colors, transitions, sound effects

### M4.2: "Silky Smooth" — Performance Pass

**Acceptance:**
- [ ] Consistent 60fps at 1080p on target hardware
- [ ] No GC spikes during gameplay (object pooling for effects, audio)
- [ ] LOD system for track objects and vehicles
- [ ] Draw call budget under 200 per frame

---

## Phase 5: Ship — Milestone Kernels

### M5.1: "Press the Button" — Build Pipeline

**Acceptance:**
- [ ] Automated build script producing Windows x64 build
- [ ] Build runs standalone with no Unity Editor dependency
- [ ] Version number visible in main menu
- [ ] Build size under 500MB

### M5.2: "On the Shelf" — Store-Ready Release

**Acceptance:**
- [ ] Store page with description, screenshots, system requirements
- [ ] Changelog generated from conventional commits
- [ ] No P0/P1 bugs in final QA pass

### M5.3: "Day-Two Patch" — Post-Launch Support

**Acceptance:**
- [ ] Patch build pipeline (incremental, not full rebuild)
- [ ] Hotfix branch workflow documented and tested
- [ ] Patch notes template and distribution
