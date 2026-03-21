# Menu Flow

> Part of the `unity-racing-ui` skill. See [SKILL.md](SKILL.md) for the overview.

## Menu Flow

```
Main Menu
  ├── Vehicle Select
  │     └── Tuning / Livery
  ├── Track Select
  │     └── Race Config (laps, AI count, weather)
  ├── Settings
  │     ├── Graphics
  │     ├── Audio
  │     ├── Controls / Rebinding
  │     └── Accessibility
  ├── Leaderboards
  └── Quit

Race Config → Pre-Race (countdown) → Race → Results
                                              ├── Replay
                                              ├── Retry
                                              └── Back to Menu
```

### Scene Transitions

Each major screen maps to a scene or an additive scene:

| Screen | Scene Strategy |
|--------|---------------|
| Main Menu | Persistent scene, loaded at boot |
| Vehicle / Track Select | UI overlay in menu scene (no scene load) |
| Race | Full scene load (track + vehicles + HUD) |
| Results | Additive scene over race scene (keeps race state for replay) |

---

