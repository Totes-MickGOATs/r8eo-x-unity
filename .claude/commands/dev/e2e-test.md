---
description: Run end-to-end tests that exercise full user journeys through the live game
---

Run end-to-end tests that exercise full user journeys through the live game.

**API reference & writing guide:** [e2e-test-api.md](./e2e-test-api.md)

## What Are E2E Tests?

E2E tests run inside the actual game process with real systems — real physics, real scene transitions, real input handling. They verify that the full chain works, not just individual units.

## Design Principles

1. **Real scene tree** — tests run in the actual game, not a mock environment
2. **Full user journeys** — start from boot, traverse menus, enter gameplay, verify outcomes
3. **Async by nature** — scene transitions, loading screens, and physics all take real time
4. **Step-based structure** — each test is a sequence of named steps for clear failure reporting
5. **Non-destructive** — tests should not corrupt save data or settings (use test profiles/configs)
6. **Timeout-guarded** — every wait operation has a timeout to prevent infinite hangs

## Setup

1. **Launch the game** with E2E test flags:
   ```
   # ENGINE-SPECIFIC: Replace with your engine's run command
   <engine_run_command> --e2e-test=<filter>
   ```

2. **Available filters:**
   - `all` — run all E2E tests
   - `boot_to_gameplay` — full boot -> menu -> gameplay flow
   - `menu_navigation` — verify all menu paths are reachable
   - `state_persistence` — save/load roundtrip verification
   - `scene_leak` — scene transitions don't leak nodes/objects

3. **Announce** to the user which tests will run.

## Common E2E Patterns

### Boot to Gameplay
Verify the full startup chain works end-to-end:
- Game boots without errors, main menu is interactive
- Starting a game session loads correctly, player can control their vehicle

### Menu Navigation
Verify all menu paths are reachable and functional:
- Every button leads to the expected screen; back/cancel returns to previous screen
- Settings changes persist through menu transitions; no dead-end screens

### State Persistence
Verify save/load roundtrip:
- Save game state (settings, progress), reload, verify saved state was restored
- Check edge cases: first launch (no save file), corrupted save

### Scene Leak Detection
Verify scene transitions don't leak objects:
- Record node/object count before transition, transition and back, compare counts
- Repeat multiple times to detect gradual leaks; check for orphan nodes specifically

### Pause/Resume
Verify pause behavior:
- Pause freezes gameplay (position, physics, timers); UI remains responsive
- Resume restores exact state; input correctly routed (gameplay input blocked while paused)

## Monitoring Loop

While E2E tests run:
1. **Poll debug output** every 3-5 seconds
2. **Watch for tagged log lines:**
   - `[STEP]` — test step starting
   - `[PASS]` — assertion passed
   - `[FAIL]` — assertion failed (highlight these)
   - `[ABORT]` — test aborted early
3. **Capture screenshots** at key moments (test start, failures, completion)
4. **Watch for non-E2E errors** — game errors during E2E runs are bugs too
