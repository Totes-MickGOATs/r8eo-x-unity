# E2E Testing Guide

## What is E2E Testing?

End-to-end (E2E) testing verifies full user journeys — boot the game, navigate menus, enter gameplay, interact with systems, and verify state. Unlike unit tests (isolated logic) or integration tests (a few systems together), E2E tests exercise the entire game stack as a player would experience it.

E2E tests answer: "Does the game actually work when everything is wired together?"

**Details:** [Common patterns & best practices](./e2e-testing-patterns.md)

---

## Why E2E Tests Matter

- Unit tests can pass while the game is broken (wiring issues, missing signals, wrong scene setup)
- Integration tests cover pairs of systems but miss emergent failures from the full chain
- E2E tests catch: scene transitions that leak nodes, input that stops after a pause, state that doesn't persist

## Design Principles

1. **Real scene tree** — tests run in the actual game, not a mock environment
2. **Full user journeys** — start from boot, traverse menus, enter gameplay, verify outcomes
3. **Async by nature** — scene transitions, loading screens, and physics all take real time
4. **Step-based structure** — each test is a sequence of named steps for clear failure reporting
5. **Non-destructive** — tests should not corrupt save data or settings (use test profiles/configs)
6. **Timeout-guarded** — every wait operation has a timeout to prevent infinite hangs

---

## Architecture

### E2E Test Base Class

The foundation provides:
- **Step tracking** — named steps within a test for clear failure reporting
- **Assertion helpers** — `assert_true`, `assert_eq`, `assert_gt`, `assert_lt`, `assert_not_null`
- **Timeout-guarded waits** — `wait_for_state`, `wait_until`, `wait_for_signal`, `wait_seconds`, `wait_frames`
- **Input injection** — `press_action`, `release_action`, `hold_action`, `tap_action`
- **Game state helpers** — `setup_gameplay`, `find_player`, `get_node_count`, `get_orphan_count`

```
class E2ETest:
    func step(name)                              # Mark a named step
    func abort(reason)                           # Abort test early
    func assert_true/eq/gt/lt/not_null(...)      # Assertions (return bool)
    func wait_seconds/frames/for_state/until/for_signal(...)  # Async waits
    func press/release/hold/tap_action(...)      # Input injection
    func setup_gameplay(config) -> bool          # Skip menus, load gameplay
    func find_player() -> Object
    func get_node_count/get_orphan_count() -> int
    func run()                                   # Override — async test steps
```

### E2E Runner

Activated by CLI flag (`--e2e-test=all` or `--e2e-test=<name>`). Discovers test files by naming convention, runs each sequentially, collects pass/fail/abort results, prints summary.

### Logging Convention

```
[E2E] --- Running: boot_to_gameplay ---
[E2E]   [STEP] Wait for main menu
[E2E]     [PASS] Reached main menu
[E2E]     [FAIL] Player health (expected > 0, actual = 0)
[E2E] --- boot_to_gameplay: FAIL ---
[E2E] === E2E SUMMARY ===
[E2E] Passed: 3/4, Failed: 1
```

---

## Running E2E Tests

```
<engine_command> -- --e2e-test=all
<engine_command> -- --e2e-test=boot_to_gameplay
<engine_command> -- --e2e-test=pause
```

## Relationship to Other Test Types

| Aspect | Unit Tests | Integration Tests | E2E Tests |
|--------|-----------|-------------------|-----------|
| **Scope** | Single function/class | 2-3 systems together | Full game |
| **Speed** | Milliseconds | Seconds | 10-60 seconds per test |
| **Dependencies** | Mocked | Partially real | All real |
| **When to write** | Always | When wiring matters | Key user journeys |
| **Maintenance cost** | Low | Medium | High (brittle to UI changes) |

E2E tests are the most expensive to write and maintain. Focus them on critical paths: boot-to-gameplay, save/load, and any flow that has historically broken.
