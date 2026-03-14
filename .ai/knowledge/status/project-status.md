# Project Status

## Current Phase: 1 — Core Mechanics

Phase 0 (Foundation) is complete. Phase 1 (Core Mechanics) is in progress.

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
- [ ] **Phase 1: Core Mechanics** — Vehicle physics refinement, camera, input, testing
- [ ] **Phase 2: Gameplay** — Game loop, scoring, AI, progression
- [ ] **Phase 3: Content** — Tracks, assets, audio, effects
- [ ] **Phase 4: Polish** — UI/UX, settings, accessibility, performance
- [ ] **Phase 5: Ship** — Build pipeline, testing, release prep

## Phase 1 Priorities

1. **Write unit tests for physics formulas** — suspension, grip, drivetrain math (100% coverage target)
2. **Write integration tests** — verify wheel-car wiring, drivetrain distribution, air physics
3. **Add ScriptableObject configs** — motor presets, grip curves, surface types as assets
4. **Enhance camera system** — multiple camera modes, smooth transitions, orbit
5. **Upgrade input system** — Unity Input System package (replace legacy Input Manager)
6. **Surface type detection** — terrain-based grip modifiers

## Systems Inventory

> Each system has a manifest in `resources/manifests/`.

| System | Status | Manifest | Namespace | Assembly | Notes |
|--------|--------|----------|-----------|----------|-------|
| Vehicle | ACTIVE | `vehicle.json` | `R8EOX.Vehicle` | `R8EOX.Vehicle` | Core physics: car controller, wheels, drivetrain, air physics |
| Input | ACTIVE | `input.json` | `R8EOX.Input` | `R8EOX.Input` | Keyboard + gamepad with auto-detection |
| Camera | ACTIVE | `camera.json` | `R8EOX.Camera` | `R8EOX.Camera` | Chase camera with smooth interpolation |
| Debug | ACTIVE | `debug.json` | `R8EOX.Debug` | `R8EOX.Debug` | Telemetry HUD overlay |
| Editor | ACTIVE | `editor.json` | `R8EOX.Editor` | `R8EOX.Editor` | Scene/prefab construction utilities |
