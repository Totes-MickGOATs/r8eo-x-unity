# Project Status

## Current Phase: 1 — Core Mechanics (Complete)

Phase 1 is complete. Ready for Phase 2 after axis audit pass.

## Phase Checklist

- [x] **Phase 0: Foundation** — Project setup, engine config, CI/CD, basic scene structure
  - [x] Unity project initialized with RCBuggyUnity import
  - [x] Git branch protection (3-layer: hooks + Claude + GitHub)
  - [x] CI/CD pipeline (Lint & Preflight, auto-merge queue, post-merge tests)
  - [x] MERGE_TOKEN secret configured
  - [x] Coding standards documented (`.ai/knowledge/architecture/coding-standards.md`)
  - [x] System overview filled in (`.ai/knowledge/architecture/system-overview.md`)
  - [x] ADR-001: Physics model decision recorded
  - [x] Add namespaces to all scripts (`R8EOX.*`)
  - [x] Add Assembly Definitions for each system
  - [x] Create system manifests for existing systems
  - [x] Refactor to coding standards (`_camelCase`, `k_` constants, `[Tooltip]`)
- [x] **Phase 1: Core Mechanics** — Vehicle physics, camera, input, testing
  - [x] Extract physics math to pure static classes (SuspensionMath, GripMath, DrivetrainMath, AirPhysicsMath, TumbleMath)
  - [x] Unit tests for physics formulas (70+ tests, 100% coverage on math)
  - [x] Wire MonoBehaviours to use extracted math classes
  - [x] ScriptableObject configs (MotorPresetConfig, SuspensionConfig, TractionConfig)
  - [x] Multi-mode camera system (Chase, Orbit, FPV, Trackside)
  - [x] Input system upgrade (IVehicleInput interface, InputMath, InputMathTests)
  - [x] Surface type detection (SurfaceType enum, SurfaceConfig, SurfaceZone)
  - [x] Core assembly (R8EOX.Core) for shared types
  - [x] Track assembly (R8EOX.Track) for track systems
  - [ ] Integration tests (blocked — needs Play mode)
  - [ ] Godot→Unity axis audit (in progress)
- [ ] **Phase 2: Gameplay** — Game loop, scoring, AI, progression
- [ ] **Phase 3: Content** — Tracks, assets, audio, effects
- [ ] **Phase 4: Polish** — UI/UX, settings, accessibility, performance
- [ ] **Phase 5: Ship** — Build pipeline, testing, release prep

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

**Play it:** Line up on a starting grid with 3+ AI opponents. Green light. Race door-to-door for position. See standings update in real time. Cross the finish line and see final results.

**Acceptance:**
- [ ] Grid start with staggered positions and countdown
- [ ] AI drivers follow racing lines, avoid obstacles, compete for position
- [ ] Live position tracking (P1, P2, P3...) on HUD
- [ ] Race finish: all cars ranked by completion time
- [ ] Basic contact physics — no clipping through AI cars

**Systems:** Vehicle, Input, Camera, Track (racing line, spawn points), RaceManager, AI (new), UI (HUD + standings)

### M2.3: "Career Start" — Session Progression

**Play it:** Pick a car from a garage. Enter a series of 3 races. Earn points per race based on finishing position. See a championship standings table after each event. Win the series.

**Acceptance:**
- [ ] Car selection screen with at least 2 distinct vehicles
- [ ] Multi-race series with points system
- [ ] Championship standings persisted across races in a session
- [ ] Season results screen: champion crowned
- [ ] Save/load session progress (stretch)

**Systems:** Vehicle, Input, Camera, Track, RaceManager, AI, UI (menus + standings), GameManager (new), Progression (new)

### Phase 2 Task Backlog

> Granular tasks feeding into the milestones above.

1. **Race Manager** — lap timing, checkpoints, race state machine (countdown → racing → finished)
2. **Game Manager** — scene loading, pause, game state transitions
3. **Lap system** — checkpoint triggers, lap counting, split times
4. **Results screen** — lap times, best lap, race position display
5. **AI driver** — basic waypoint following for opponent cars
6. **Track layout tools** — checkpoint placement, racing line, spawn points

---

## Phase 3: Content — Milestone Kernels

### M3.1: "Backyard Bash" — First Real Track

**Play it:** Race on a detailed backyard-scale track with jumps, berms, and surface transitions. Hear your motor whine through corners. See dust kick up on dirt. Feel the surface change from pavement to gravel through the physics.

**Acceptance:**
- [ ] One complete, textured track with mixed surfaces (asphalt, dirt, gravel)
- [ ] At least 2 jump features with landing physics
- [ ] Motor audio reactive to RPM and throttle
- [ ] Surface-dependent audio (tire crunch on gravel, hum on asphalt)
- [ ] Particle effects: dust/dirt roost, tire smoke on hard braking

**Systems:** Vehicle, Track (terrain, surfaces), Audio (new), VFX (new), Camera

### M3.2: "Track Day Variety" — Multiple Venues

**Play it:** Choose from 3 distinct tracks — each feels different to drive. A tight technical track with chicanes, an open high-speed oval with banked turns, and an off-road crawl course with elevation.

**Acceptance:**
- [ ] 3 playable tracks with distinct layouts and characters
- [ ] Each track uses different surface mix and elevation profiles
- [ ] Track selection screen with preview images
- [ ] Ambient audio per venue (birds, wind, crowd murmur)
- [ ] Consistent checkpoint/timing integration across all tracks

**Systems:** Track (terrain, layout tools), UI (track select), Audio, Camera (trackside positions per venue)

### M3.3: "Show Car" — Vehicle Variety

**Play it:** Pick from 3 different RC cars: a nimble buggy, a beefy short course truck, and a drift-tuned touring car. Each drives distinctly — the buggy is agile, the truck is stable over rough terrain, the touring car slides gracefully.

**Acceptance:**
- [ ] 3 vehicle models with unique meshes and paint
- [ ] Distinct physics profiles per vehicle (ScriptableObject configs)
- [ ] Vehicle preview in selection screen with stat bars
- [ ] Each vehicle sounds different (motor pitch, servo speed)
- [ ] All vehicles balanced for competitive racing

**Systems:** Vehicle (configs, models), UI (car select), Audio, Camera

---

## Phase 4: Polish — Milestone Kernels

### M4.1: "Living Room Ready" — Couch-Friendly UX

**Play it:** Boot the game. Navigate menus with a gamepad — no mouse needed. Adjust graphics quality, audio volume, controls. Everything feels snappy and responsive. Hand the controller to a friend and they can figure it out without instructions.

**Acceptance:**
- [ ] Full gamepad menu navigation (D-pad, A/B confirm/back)
- [ ] Settings screen: graphics quality, resolution, audio sliders, control remapping
- [ ] Settings persist across sessions
- [ ] Loading screens with tips and car art
- [ ] Consistent UI theme: fonts, colors, transitions, sound effects

**Systems:** UI (menus, settings, theme), Input (rebinding), Audio (UI SFX), GameManager (settings persistence)

### M4.2: "Silky Smooth" — Performance Pass

**Play it:** Race with 4 cars on the most complex track. Framerate never dips below 60fps. No hitches during jumps, collisions, or checkpoint triggers. GPU and CPU headroom for effects.

**Acceptance:**
- [ ] Consistent 60fps at 1080p on target hardware
- [ ] No GC spikes during gameplay (object pooling for effects, audio)
- [ ] LOD system for track objects and vehicles
- [ ] Draw call budget under 200 per frame
- [ ] Profiler-verified: no single-frame spikes > 16ms

**Systems:** Vehicle, Track, VFX, Audio, Camera, all systems (optimization pass)

---

## Phase 5: Ship — Milestone Kernels

### M5.1: "Press the Button" — Build Pipeline

**Play it:** Run a single command. Out comes a Windows build, zipped and ready for upload. Run it on a clean machine — it launches, shows splash screen, loads main menu, lets you race.

**Acceptance:**
- [ ] Automated build script (CI or local) producing Windows x64 build
- [ ] Splash screen with game logo
- [ ] Build runs standalone with no Unity Editor dependency
- [ ] Version number visible in main menu (from VERSIONING.md convention)
- [ ] Build size under 500MB (stretch: under 200MB)

**Systems:** Build pipeline (new), UI (splash, version display), all systems (build-time validation)

### M5.2: "On the Shelf" — Store-Ready Release

**Play it:** Download the game from itch.io (or Steam). Install. Launch. Play through the full career mode. No crashes, no missing assets, no broken menus. Read the changelog and see what's new.

**Acceptance:**
- [ ] Store page with description, screenshots, and system requirements
- [ ] Changelog generated from conventional commits (see `changelog` skill)
- [ ] No P0/P1 bugs in final QA pass
- [ ] Analytics/crash reporting opt-in on first launch (stretch)
- [ ] Community announcement posted (Discord, social media)

**Systems:** Build pipeline, UI, all gameplay systems, Release pipeline (see `release-pipeline` skill)

### M5.3: "Day-Two Patch" — Post-Launch Support

**Play it:** Report a bug on the issue tracker. Within days, a patch drops. Update the game. The bug is gone. New content teased in patch notes.

**Acceptance:**
- [ ] Patch build pipeline (incremental, not full rebuild)
- [ ] Hotfix branch workflow documented and tested
- [ ] Patch notes template and distribution
- [ ] Telemetry dashboard for crash rates and session length (stretch)
- [ ] Community feedback loop: issue tracker → triage → fix → release

**Systems:** Build pipeline, Release pipeline, all systems (maintenance mode)

---

## Systems Inventory

> Each system has a manifest in `resources/manifests/`.

| System | Status | Manifest | Namespace | Assembly | Notes |
|--------|--------|----------|-----------|----------|-------|
| Vehicle | ACTIVE | `vehicle.json` | `R8EOX.Vehicle` | `R8EOX.Vehicle` | Physics: car, wheels, drivetrain, air, math classes, configs |
| Input | ACTIVE | `input.json` | `R8EOX.Input` | `R8EOX.Input` | IVehicleInput interface, InputMath, keyboard+gamepad |
| Camera | ACTIVE | `camera.json` | `R8EOX.Camera` | `R8EOX.Camera` | Multi-mode: Chase, Orbit, FPV, Trackside |
| Core | ACTIVE | `core.json` | `R8EOX.Core` | `R8EOX.Core` | SurfaceType enum, SurfaceConfig |
| Track | ACTIVE | `track.json` | `R8EOX.Track` | `R8EOX.Track` | SurfaceZone trigger for grip modifiers |
| Debug | ACTIVE | `debug.json` | `R8EOX.Debug` | `R8EOX.Debug` | Telemetry HUD overlay |
| Editor | ACTIVE | `editor.json` | `R8EOX.Editor` | `R8EOX.Editor` | Scene/prefab construction utilities |
