# Project Status

## Current Phase: 0 → 1 Transition

Phase 0 (Foundation) is nearly complete. Phase 1 (Core Mechanics) work begins now.

## Phase Checklist

- [x] **Phase 0: Foundation** — Project setup, engine config, CI/CD, basic scene structure
  - [x] Unity project initialized with RCBuggyUnity import
  - [x] Git branch protection (3-layer: hooks + Claude + GitHub)
  - [x] CI/CD pipeline (Lint & Preflight, auto-merge queue, post-merge tests)
  - [x] MERGE_TOKEN secret configured
  - [x] Coding standards documented (`.ai/knowledge/architecture/coding-standards.md`)
  - [x] System overview filled in (`.ai/knowledge/architecture/system-overview.md`)
  - [x] ADR-001: Physics model decision recorded
  - [ ] Add namespaces to all scripts (`R8EOX.*`)
  - [ ] Add Assembly Definitions for each system
  - [ ] Create system manifests for existing systems
- [ ] **Phase 1: Core Mechanics** — Vehicle physics refinement, camera, input, testing
- [ ] **Phase 2: Gameplay** — Game loop, scoring, AI, progression
- [ ] **Phase 3: Content** — Tracks, assets, audio, effects
- [ ] **Phase 4: Polish** — UI/UX, settings, accessibility, performance
- [ ] **Phase 5: Ship** — Build pipeline, testing, release prep

## Phase 1 Priorities

1. **Add namespaces + Assembly Definitions** — enforce `R8EOX.*` namespace convention
2. **Refactor existing code to coding standards** — naming conventions (`_camelCase`), constants (`k_`), remove magic numbers
3. **Write unit tests for physics formulas** — suspension, grip, drivetrain math (100% coverage target)
4. **Write integration tests** — verify wheel-car wiring, drivetrain distribution, air physics
5. **Add ScriptableObject configs** — motor presets, grip curves, surface types as assets
6. **Enhance camera system** — multiple camera modes, smooth transitions, orbit
7. **Upgrade input system** — Unity Input System package (replace legacy Input Manager)
8. **Surface type detection** — terrain-based grip modifiers

## Systems Inventory

> Each system should have a manifest in `resources/manifests/`.

| System | Status | Manifest | Scripts | Notes |
|--------|--------|----------|---------|-------|
| Vehicle | ACTIVE | needs creation | `RCCar.cs`, `RaycastWheel.cs`, `Drivetrain.cs`, `RCAirPhysics.cs` | Core physics — ported from Godot, needs namespace + standards pass |
| Input | ACTIVE | needs creation | `RCInput.cs` | Legacy Input Manager — upgrade to Input System planned |
| Camera | ACTIVE | needs creation | `ChaseCamera.cs` | Basic chase camera — needs enhancement |
| Debug | ACTIVE | needs creation | `TelemetryHUD.cs` | OnGUI telemetry — functional, low priority for refactor |
| Editor | ACTIVE | needs creation | `SceneSetup.cs` | Editor utilities |