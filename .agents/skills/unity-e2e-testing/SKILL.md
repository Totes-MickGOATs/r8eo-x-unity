---
name: unity-e2e-testing
description: Unity E2E Testing Automation
---


# Unity E2E Testing Automation

Use this skill when writing PlayMode end-to-end tests that simulate full user journeys from input to visible outcome in a running game loop.

## E2E Coverage Requirement (MANDATORY)

> **Every user-facing feature or behavior change MUST have at least 1 E2E PlayMode test.** This is non-negotiable.

E2E tests verify the complete user journey from input to visible outcome in a real game loop. They catch wiring bugs, scene setup issues, and integration failures that unit tests cannot.

- **When required:** Any change that affects what the player sees, hears, or controls
- **Where:** `Assets/Tests/PlayMode/`
- **Pre-implementation:** E2E tests MUST be written by a separate black-box agent before implementation begins. See `.agents/skills/ask-first/SKILL.md` Phase 2.
- **Test naming:** `Feature_Scenario_ExpectedOutcome` — must read like a sentence

## Testing Pyramid for Games

| Level | What to Test | Tools | Speed |
|-------|-------------|-------|-------|
| **Unit** | Pure functions, math, data | Unity Test Framework (EditMode) | Fast |
| **Integration** | Component wiring, system interaction | Unity Test Framework (PlayMode) | Medium |
| **E2E** | Full gameplay flows, boot-to-play | PlayMode + InputTestFixture + AltTester | Slow |

**Rule of thumb:** Heavy unit/integration coverage, few but meaningful E2E tests covering critical user journeys.

---

## 7. MCP-Based Testing (Development Time)

For interactive testing during development using Claude Code:

```
Workflow:
1. read_console → check for compilation errors
2. execute_script → set up test conditions
3. play_game → enter play mode
4. execute_script → runtime assertions
5. capture_scene_object / capture_ui_canvas → visual check
6. get_unity_logs → error detection
7. stop_game → end session
```

**Best for:** Interactive development-time verification. **Not for CI.**

---

## Tools to Avoid

| Tool | Reason |
|------|--------|
| `com.unity.automated-testing` | Development halted since Dec 2021, stuck at `0.8.1-preview.2` |
| `BinaryFormatter` for test data | Security vulnerability, deprecated |
| Pixel-exact screenshot comparison | Too brittle across GPUs, use perceptual diff |
| `Thread.Sleep` in tests | Use `WaitForSeconds` / `WaitUntil` instead |


## Topic Pages

- [1. Unity Test Framework (Built-in Foundation)](skill-1-unity-test-framework-built-in-foundation.md)
- [8. Best Practices](skill-8-best-practices.md)

