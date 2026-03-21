---
description: E2E test base class API reference and writing guide
---

# E2E Test API Reference

Part of the [e2e-test command](./e2e-test.md).

---

## E2E Test Base Class API

### Step Tracking
- `step(name)` — mark a named test step (for failure reporting)
- `abort(reason)` — abort the current test early

### Assertions
- `assert_true(condition, msg)` — check boolean condition
- `assert_eq(actual, expected, msg)` — equality check
- `assert_gt(actual, threshold, msg)` — greater-than check
- `assert_lt(actual, threshold, msg)` — less-than check
- `assert_not_null(value, msg)` — null check

### Wait Helpers (Async)
- `wait_seconds(n)` — wait real time
- `wait_frames(n)` — wait N frames
- `wait_for_state(state, timeout)` — wait for game state transition
- `wait_until(condition, timeout)` — poll condition each frame
- `wait_for_signal(obj, signal_name, timeout)` — wait for a signal

### Input Injection
- `press_action(action)` — simulate input press
- `release_action(action)` — simulate input release
- `hold_action(action, duration)` — press, wait, release
- `tap_action(action)` — quick press and release

### Game State
- `setup_gameplay(config)` — skip menus, load gameplay directly (for focused tests)
- `find_player()` — locate the player entity
- `get_node_count()` — for leak detection
- `get_orphan_count()` — for leak detection

---

## Test Structure Template

```
class BootToGameplayTest extends E2ETest:
    func run():
        step("Wait for boot sequence")
        ok = wait_for_state(MAIN_MENU, timeout=15.0)
        assert_true(ok, "Reached main menu")

        step("Navigate to gameplay")
        tap_action("ui_accept")
        ok = wait_for_state(GAMEPLAY, timeout=30.0)
        assert_true(ok, "Entered gameplay")

        step("Verify gameplay state")
        player = find_player()
        assert_not_null(player, "Player exists")
        assert_gt(player.health, 0, "Player is alive")

        step("Return to menu")
        tap_action("pause")
        tap_action("quit_to_menu")
        ok = wait_for_state(MAIN_MENU, timeout=10.0)
        assert_true(ok, "Returned to main menu")
```

---

## Writing New E2E Tests

1. Create a test file in `tests/e2e/test_<name>.<ext>`
2. Extend the E2E test base class
3. Implement `run()` with sequential steps
4. Use `step()` to name each phase for clear failure reporting
5. Use `assert_*` helpers for verification (log PASS/FAIL automatically)
6. Use `wait_*` helpers for async operations (always with timeouts)
7. Register with the E2E runner or use auto-discovery via naming convention

---

## Verification Points (Every E2E Test Should Check)

- No crashes or unhandled exceptions during the entire flow
- No leaked nodes/objects after scene transitions
- Correct state transitions (game state machine follows expected path)
- Player input is responsive after every transition
- UI elements are visible and interactive when expected
- No NaN or infinite values in gameplay state (positions, scores, timers)

---

## When Tests Complete — Report Format

**Tests Run**: [count] | **Passed**: [count] | **Failed**: [count]

| Test | Status | Steps Completed | Notes |
|------|--------|-----------------|-------|
| boot_to_gameplay | PASS/FAIL | 8/10 | ... |

**Failures** (if any):
- Test: [name], Step: [step], Assertion: [what failed]
- Suggested fix: [based on the failure context]
