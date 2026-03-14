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

## Phase 2 Priorities

1. **Race Manager** — lap timing, checkpoints, race state machine (countdown → racing → finished)
2. **Game Manager** — scene loading, pause, game state transitions
3. **Lap system** — checkpoint triggers, lap counting, split times
4. **Results screen** — lap times, best lap, race position display
5. **AI driver** — basic waypoint following for opponent cars
6. **Track layout tools** — checkpoint placement, racing line, spawn points

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
