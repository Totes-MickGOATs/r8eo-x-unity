# physics-conformance-audit/

Physics conformance testing framework -- 93 checks across 12 categories validating simulation accuracy against analytical predictions.

## Files

| File | Role |
|------|------|
| `SKILL.md` | Full conformance methodology, check categories, recorder API, how to add checks |

## Key Concepts

- **Black-box testing:** Expected values derived from Newtonian mechanics, never from implementation code
- **Tolerance tiers:** Excellent (<1%), Good (<5%), Noticeable (<15%), Poor (<50%), Broken (>50%)
- **12 categories:** Geometric, Force, Conservation, Kinematic, Contact, Suspension, Grip, Drivetrain, Air, Temporal, ESC, Compound
- **DB persistence:** Results stored in SQLite for trend tracking and regression detection

## Related Skills

- `debug-system` -- Structured logging, overlays
- `unity-physics-tuning` -- RC-specific PhysX configuration
- `clean-room-qa` -- Black-box test methodology
- `unity-testing-patterns` -- Unity Test Framework patterns
