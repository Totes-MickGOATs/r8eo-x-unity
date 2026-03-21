---
name: user-interaction-testing
description: User Interaction Testing Skill
---


# User Interaction Testing Skill

Use this skill when writing automated tests that simulate user-facing interactions such as input handling, menu navigation, button clicks, and full user journeys.

## Why Interaction Testing Matters

Unit tests verify that individual functions return correct values. Interaction tests verify that the user can actually accomplish tasks through the UI. A system where every function passes unit tests can still be unusable if:

- Buttons don't respond to clicks
- Navigation flow skips a required screen
- Input is consumed by the wrong UI element
- State isn't properly carried between screens

## Mocking External Systems

During user flow tests, external systems should be mocked to keep tests fast, deterministic, and isolated.

### What to Mock

| System | Why Mock | Mock Strategy |
|--------|----------|---------------|
| **Network** | Tests shouldn't depend on connectivity | Return canned responses for API calls |
| **File I/O** | Tests shouldn't write to real save files | Use in-memory storage or temp directory |
| **Platform services** | Steam, console APIs may not be available in CI | Stub that records calls without executing |
| **Audio** | Headless CI has no audio device | Null audio backend or silent stubs |
| **Time** | Tests need deterministic timing | Injectable clock that can be advanced manually |

### Mock Boundary Pattern

Mock at the boundary between your code and the external system, not deep inside your code:

```
# Good: mock the service interface
var mock_save_service = MockSaveService.new()
settings_manager.save_service = mock_save_service

# Bad: mock individual file operations inside the save code
# This couples your test to implementation details
```

## Test Infrastructure Recommendations

### Helpers to Build

| Helper | Purpose |
|--------|---------|
| `InputInjector` | Inject actions, keys, mouse events with automatic cleanup |
| `MenuNavigator` | Navigate to any screen by name via shortest path |
| `ScreenAssertions` | Assert screen presence, focused element, visible controls |
| `JourneyRunner` | Execute a sequence of steps with assertions between each |
| `MockServiceProvider` | Swap real services for mocks before test, restore after |

### Test Isolation

Each test must start from a known state and leave no side effects:

1. **Setup:** Initialize to a known screen/state, connect mocks
2. **Execute:** Run the interaction sequence
3. **Assert:** Verify expected state
4. **Teardown:** Release injected inputs, restore real services, return to initial state

If a test fails mid-journey, the teardown must still run to avoid corrupting subsequent tests.


## Topic Pages

- [Input Injection Patterns](skill-input-injection-patterns.md)
- [Verification: State Assertions After Each Step](skill-verification-state-assertions-after-each-step.md)

