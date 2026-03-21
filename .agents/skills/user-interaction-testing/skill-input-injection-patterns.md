# Input Injection Patterns

> Part of the `user-interaction-testing` skill. See [SKILL.md](SKILL.md) for the overview.

## Input Injection Patterns

### Direct Action Injection

Most engines/frameworks provide a way to programmatically create and dispatch input events. Use this instead of relying on real hardware input during tests.

```
# Pseudocode — adapt to your engine/framework
func inject_action_press(action_name: String) -> void:
    var event = create_input_action(action_name, pressed=true)
    dispatch_input(event)
    process_frame()  # Let the engine handle the input

func inject_action_release(action_name: String) -> void:
    var event = create_input_action(action_name, pressed=false)
    dispatch_input(event)
    process_frame()
```

### Key Principles

1. **Always process a frame after injection** — the input event must propagate through the engine's input pipeline before assertions
2. **Pair press with release** — unless testing hold behavior, always release after pressing to avoid stuck-input state
3. **Clear input state between tests** — leftover pressed keys from a previous test will corrupt the next test
4. **Use action names, not raw keys** — test the abstraction layer the game uses, not hardware-specific bindings

### Continuous Input (Axes, Analog)

For analog input (steering, throttle), inject a value and hold it for multiple frames:

```
# Pseudocode
func inject_axis(action_negative: String, action_positive: String, value: float) -> void:
    if value > 0:
        set_action_strength(action_positive, abs(value))
        set_action_strength(action_negative, 0.0)
    else:
        set_action_strength(action_negative, abs(value))
        set_action_strength(action_positive, 0.0)
    process_frame()
```

### Mouse/Pointer Input

For UI testing, simulate mouse movement and clicks:

```
# Pseudocode
func click_at(position: Vector2) -> void:
    move_mouse_to(position)
    process_frame()
    inject_mouse_button(BUTTON_LEFT, pressed=true, position=position)
    process_frame()
    inject_mouse_button(BUTTON_LEFT, pressed=false, position=position)
    process_frame()

func click_control(control: UIElement) -> void:
    var center = control.global_position + control.size / 2
    click_at(center)
```

## Menu Flow Simulation

### Navigation Helper Pattern

Create a helper that knows how to navigate between screens:

```
# Pseudocode
class MenuNavigator:
    func navigate_to(target_screen: String) -> bool:
        # Define paths between known screens
        var paths = {
            "main_menu": ["splash"],           # from splash, go to main_menu
            "options": ["main_menu"],           # from main_menu, go to options
            "gameplay": ["main_menu", "mode_select", "pre_race"],
        }
        for intermediate in paths[target_screen]:
            if not _advance_from(intermediate):
                return false
        return _verify_screen(target_screen)

    func _advance_from(screen: String) -> bool:
        match screen:
            "splash":
                click_control(find_button("Start"))
                await_screen_transition()
                return true
            "main_menu":
                click_control(find_button("Play"))
                await_screen_transition()
                return true
        return false

    func _verify_screen(screen: String) -> bool:
        # Check that the expected screen is actually displayed
        return current_scene_name() == screen
```

### Screen Transition Verification

After every navigation action, verify:

1. **The old screen is gone** — it shouldn't still be processing input
2. **The new screen is present** — the expected UI elements exist
3. **Focus is correct** — the expected element has input focus (especially for gamepad navigation)
4. **State carried over** — any data from the previous screen is available (selected track, chosen car, etc.)

```
# Pseudocode
func assert_screen_transition(from: String, to: String) -> void:
    assert_false(is_screen_active(from), "Old screen '%s' still active" % from)
    assert_true(is_screen_active(to), "New screen '%s' not active" % to)
    assert_not_null(get_focused_control(), "No UI element has focus on '%s'" % to)
```

### Back Navigation

Test that every screen's back/cancel button returns to the correct parent:

```
# Pseudocode
func test_back_navigation():
    for screen in ["options", "mode_select", "leaderboard"]:
        navigate_to(screen)
        press_back()
        assert_screen("main_menu", "Back from %s should return to main_menu" % screen)
```

## User Journey Simulation

A user journey is a complete path through the application that accomplishes a real goal. Journeys test the full integration — not just individual screens.

### Journey Template

```
# Pseudocode
func test_journey_new_player_first_race():
    # Boot → Splash
    launch_application()
    assert_screen("splash")

    # Splash → Main Menu
    click_control(find_button("Start"))
    await_transition()
    assert_screen("main_menu")

    # Main Menu → Mode Select
    click_control(find_button("Play"))
    await_transition()
    assert_screen("mode_select")

    # Mode Select → Pre-Race Setup (default selections)
    click_control(find_button("Quick Race"))
    await_transition()
    assert_screen("pre_race_setup")

    # Pre-Race → Loading → Gameplay
    click_control(find_button("Start Race"))
    await_transition(timeout=10)  # Loading may take longer
    assert_screen("gameplay")

    # Verify gameplay state
    assert_true(race_is_active())
    assert_true(player_vehicle_exists())
    assert_equals(countdown_state(), "running")
```

### Critical Journeys to Test

Every application should have automated tests for these journeys at minimum:

| Journey | What It Tests |
|---------|--------------|
| **First launch** | Boot → initial setup → main screen |
| **Primary task** | The main thing users do (play a race, edit a document, etc.) |
| **Settings round-trip** | Change a setting → verify it takes effect → restart → verify it persisted |
| **Error recovery** | Trigger an error state → verify the user can recover without restarting |
| **Exit and resume** | Start a task → exit → restart → verify state is preserved |

## Edge Cases

### Rapid Input

Users press buttons faster than developers expect. Test:

```
# Pseudocode
func test_rapid_button_presses():
    navigate_to("main_menu")
    # Mash the play button 5 times quickly
    for i in range(5):
        click_control(find_button("Play"))
        # Do NOT wait for transition between clicks
    # Should end up on mode_select, not crash or double-navigate
    process_frames(30)  # Let everything settle
    assert_screen("mode_select")
```

### Simultaneous Inputs

Test conflicting inputs:

```
# Pseudocode
func test_simultaneous_throttle_and_brake():
    inject_action_press("throttle")
    inject_action_press("brake")
    process_frames(10)
    # Verify the game handles this gracefully
    # (behavior depends on design — brake priority, cancel out, etc.)
    assert_no_errors()
    inject_action_release("throttle")
    inject_action_release("brake")
```

### Unexpected Navigation

Test what happens when users go "off script":

| Scenario | Test |
|----------|------|
| Press back during loading | Should cancel gracefully, not crash |
| Open pause menu during countdown | Should pause the countdown or be blocked |
| Alt-tab during gameplay | Should auto-pause or continue without corruption |
| Resize window during menu | UI should adapt, not break layout |
| Disconnect controller during gameplay | Should pause or show reconnect prompt |

### Input During Transitions

Test input that arrives while a screen transition is animating:

```
# Pseudocode
func test_input_during_transition():
    click_control(find_button("Play"))
    # Immediately try to interact before transition completes
    inject_action_press("ui_cancel")
    process_frames(5)
    inject_action_release("ui_cancel")
    # Wait for transition to complete
    await_transition()
    # Verify we're in a sane state
    assert_true(is_on_valid_screen())
```

