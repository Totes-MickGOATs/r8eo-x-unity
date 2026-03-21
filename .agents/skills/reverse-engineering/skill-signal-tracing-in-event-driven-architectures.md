# Signal Tracing in Event-Driven Architectures

> Part of the `reverse-engineering` skill. See [SKILL.md](SKILL.md) for the overview.

## Signal Tracing in Event-Driven Architectures

Event-driven systems (signals, events, callbacks) make bugs harder to trace because the call stack doesn't show the full story. The connection between emitter and receiver is configured at runtime, not visible in static code.

### Tracing Strategy

1. **Start from the symptom** — which handler is misbehaving?
2. **Find the signal connection** — search for `.connect(` with the handler name, or the signal name
3. **Find the emission point** — search for `.emit(` or `emit_signal(` with the signal name
4. **Trace the emission context** — what state is the emitter in when it fires?
5. **Check connection timing** — is the signal connected before or after the first emission?

### Common Signal Bugs

| Bug | Symptom | Root Cause |
|-----|---------|------------|
| **Late connection** | Handler never fires | Signal emitted before `connect()` runs |
| **Double connection** | Handler fires twice | `connect()` called twice without `is_connected()` guard |
| **Wrong signal** | Handler fires at wrong time | Connected to similar-named signal (e.g., `changed` vs `value_changed`) |
| **Stale reference** | Crash on signal emit | Connected object was freed but signal wasn't disconnected |
| **Argument mismatch** | Wrong data in handler | Signal emits different args than handler expects |

### Mapping Signal Flow

For complex systems, draw the signal flow:

```
ObjectA.signal_x → ObjectB.handler_y → ObjectB.signal_z → ObjectC.handler_w
```

This makes it visible where the chain breaks. Check each arrow independently.

