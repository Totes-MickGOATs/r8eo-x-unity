# Verification: State Assertions After Each Step

> Part of the `user-interaction-testing` skill. See [SKILL.md](SKILL.md) for the overview.

## Verification: State Assertions After Each Step

After every interaction, assert the expected state change occurred. Don't just assert the final state — assert intermediate states too. This makes failures easier to diagnose.

### Assertion Patterns

```
# Pseudocode — after clicking "Start Race"

# Immediate: button should be disabled to prevent double-click
assert_false(start_button.is_enabled(), "Start button should disable after click")

# After transition: loading screen should be visible
await_transition()
assert_screen("loading")
assert_true(loading_bar.is_visible(), "Loading bar should be visible")

# After loading: gameplay should be active
await_condition(func(): return is_screen("gameplay"), timeout=10)
assert_true(race_manager.is_race_active(), "Race should be active after loading")
assert_not_null(player_vehicle, "Player vehicle should exist")
assert_equals(hud.lap_display.text, "Lap 1/3", "HUD should show starting lap")
```

### State Machine Verification

If your UI uses a state machine, verify state transitions:

```
# Pseudocode
func test_menu_state_machine():
    assert_state("idle")

    click_play()
    assert_state("transitioning")

    await_transition()
    assert_state("mode_select")

    click_back()
    assert_state("transitioning")

    await_transition()
    assert_state("idle")  # Back to main menu idle state
```

