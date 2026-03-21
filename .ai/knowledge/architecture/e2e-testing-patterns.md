# E2E Testing — Common Patterns & Best Practices

Part of the [E2E Testing Guide](./e2e-testing-guide.md).

---

## Common Test Patterns

### Boot to Gameplay

```
class BootToGameplayTest extends E2ETest:
    func run():
        step("Boot sequence")
        var ok = await wait_for_state(MAIN_MENU, 15.0)
        if not assert_true(ok, "Reached main menu"):
            abort("Never reached main menu")
            return

        step("Start game")
        tap_action("ui_accept")
        ok = await wait_for_state(GAMEPLAY, 30.0)
        if not assert_true(ok, "Entered gameplay"):
            abort("Never entered gameplay")
            return

        step("Verify player")
        var player = find_player()
        assert_not_null(player, "Player spawned")

        step("Return to menu")
        tap_action("pause")
        tap_action("quit_to_menu")
        ok = await wait_for_state(MAIN_MENU, 10.0)
        assert_true(ok, "Returned to menu cleanly")
```

**What it catches:** Missing autoloads, broken scene references, initialization order bugs, scene transition failures.

### Menu Navigation

```
class MenuNavigationTest extends E2ETest:
    func run():
        step("Reach main menu")
        await wait_for_state(MAIN_MENU, 15.0)

        step("Open options")
        tap_action("options_button")
        await wait_seconds(0.5)
        assert_true(is_screen_visible("options"), "Options screen shown")

        step("Back to main menu")
        tap_action("ui_cancel")
        await wait_seconds(0.5)
        assert_true(is_screen_visible("main_menu"), "Back at main menu")
```

**What it catches:** Dead-end screens, broken back navigation, missing button connections.

### State Persistence

```
class StatePersistenceTest extends E2ETest:
    func run():
        step("Set a value")
        set_test_setting("audio_volume", 0.5)
        save_settings()

        step("Reload")
        reload_settings()

        step("Verify persisted")
        var vol = get_test_setting("audio_volume")
        assert_eq(vol, 0.5, "Volume persisted")
```

**What it catches:** Serialization bugs, missing save calls, load-order dependencies, type coercion errors.

### Scene Leak Detection

```
class SceneLeakTest extends E2ETest:
    func run():
        step("Baseline node count")
        await wait_for_state(MAIN_MENU, 15.0)
        var baseline = get_node_count()
        var orphan_baseline = get_orphan_count()

        step("Transition cycle")
        for i in range(3):
            await setup_gameplay(default_config)
            await wait_seconds(2.0)
            tap_action("pause")
            tap_action("quit_to_menu")
            await wait_for_state(MAIN_MENU, 10.0)
            await wait_seconds(1.0)

        step("Check for leaks")
        var delta = get_node_count() - baseline
        var orphan_delta = get_orphan_count() - orphan_baseline
        assert_lt(float(delta), 50.0, "Node count delta after 3 cycles")
        assert_lt(float(orphan_delta), 10.0, "Orphan count delta after 3 cycles")
```

**What it catches:** Nodes not freed on scene exit, signal connections preventing GC, orphaned nodes.

### Pause/Resume

Verify pause freezes gameplay (position, physics, timers stay constant) and resume restores exact state.
Steps: enter gameplay → record position → pause → `assert_eq(position, pos_before)` → unpause → move → `assert_true(position != pos_before)`.

**What it catches:** Process mode errors, input routing bugs, state corruption on resume.

---

## Writing E2E Tests

1. **Create the test file** in `tests/e2e/test_<name>.<ext>`
2. **Extend the E2E base class** and set the test name in the constructor
3. **Implement `run()`** as an async method with sequential steps
4. **Use `step()` to name each phase** — appears in failure reports
5. **Use `assert_*` for verification** — return `bool` so you can early-exit on critical failures
6. **Use `wait_*` for async operations** — ALWAYS with a timeout
7. **Use `abort()` for unrecoverable failures** — skips remaining steps

### Best practices

- **Early exit on critical failures:** If a step fails that makes later steps meaningless, call `abort()` and return.
- **Use `setup_gameplay()` for focused tests:** Skip menus when testing gameplay-specific behavior.
- **Keep tests independent:** Each test should start from a clean state.
- **Timeout generously but not infinitely:** Scene loads might take 10-30 seconds. Use 15-30s, not 5s or 300s.
- **Test the unhappy path too:** Pause during a loading screen? Spam the back button?

---

## Monitoring E2E Runs

1. Poll debug output every 3-5 seconds
2. Watch for `[E2E]` tagged lines — especially `[FAIL]` and `[ABORT]`
3. Capture screenshots at failures for visual debugging
4. Watch for non-E2E errors — game errors during E2E runs are bugs too
5. Wait for `=== E2E SUMMARY ===` to know all tests are complete
