---
name: unity-state-machines
description: Unity State Machines
---


# Unity State Machines

Use this skill when implementing state machines for game logic, AI behavior, animation control, or UI flow, whether using Unity's Animator or custom FSM code.

## When to Use a State Machine

Use a state machine when an entity has:
- Distinct behavioral modes (idle, running, attacking, dead)
- Rules about which transitions are valid (can't attack while dead)
- Entry/exit logic per state (play animation on enter, stop particles on exit)

If you have nested `if/else` chains checking booleans (`_isAttacking && !_isDead && _isGrounded`), you need a state machine.

## Choosing the Right Approach

| Factor | Animator FSM | Custom Code FSM | SO State FSM |
|--------|-------------|-----------------|--------------|
| Visual editing | Built-in visual graph | None (code only) | Inspector per state |
| Animation sync | Seamless | Manual | Manual |
| Testability | Hard (needs Animator) | Easy (pure C#) | Medium (needs SO instances) |
| Designer-friendly | Medium (complex UI) | No | Yes (drag-drop states) |
| State count | 3-8 | Any | Any |
| Complexity | Low-medium | High (but flexible) | Medium |
| Performance | Good | Best | Good |

**Recommendations:**
- **Character animation:** Animator FSM (it is designed for this)
- **Player controller logic:** Custom code FSM (testable, explicit)
- **AI behavior:** SO state FSM (designer-tunable, variant-friendly)
- **Game flow (menu/play/pause):** Simple enum + switch (under 6 states)
- **Complex AI with many conditions:** Consider a behavior tree instead


## Topic Pages

- [Unity Animator as a State Machine](skill-unity-animator-as-a-state-machine.md)

