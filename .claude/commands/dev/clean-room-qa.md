# Clean Room QA

Run black-box tests against a system with zero implementation knowledge.

## Usage
```
/dev:clean-room-qa <system-name>
```

## Process

1. Read the skill guide: `.agents/skills/clean-room-qa/SKILL.md`
2. Find the system's public API (only signatures — DO NOT read method bodies)
3. Write tests derived from physics/domain knowledge
4. Place tests in `Assets/Tests/EditMode/` using the existing assembly definition
5. Generate .meta files for new test files
6. Report which tests pass and which fail (failures = bugs found)

## Arguments
- `$ARGUMENTS` — The system name to audit (e.g., "vehicle", "suspension", "grip", "drivetrain", "input", "all")

If no argument, audit ALL systems.
