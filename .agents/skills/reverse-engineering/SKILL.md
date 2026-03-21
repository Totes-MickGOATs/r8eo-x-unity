---
name: reverse-engineering
description: Reverse Engineering & Debug Methodology Skill
---


# Reverse Engineering & Debug Methodology Skill

Use this skill when diagnosing bugs through systematic chain-of-custody debugging, or when you need to understand how an unfamiliar system works before modifying it.

## Core Principle: Chain of Custody

Every bug has a chain of custody — a sequence of events from the trigger to the symptom. Your job is to trace that chain backwards from what you can see (the symptom) to what actually went wrong (the root cause).

```
Symptom → Proximate Cause → Intermediate Steps → Root Cause
```

**Never fix the symptom.** Always trace to the root cause. A symptom fix creates a new bug waiting to happen.

## Binary Search Technique

When you can't tell which of many changes (or code paths) introduced the bug:

### Git Bisect (for regression bugs)

```bash
git bisect start
git bisect bad                    # Current commit is broken
git bisect good <known-good-sha> # This commit was working
# Git checks out a midpoint — test it
git bisect bad   # or git bisect good
# Repeat until git identifies the culprit commit
git bisect reset
```

### Code Block Bisect (for logic bugs)

When the bug is in a single file or function:

1. Comment out the bottom half of the suspect code
2. Test — does the bug still occur?
3. If yes: bug is in the top half. Comment out the bottom half of the top half.
4. If no: bug is in the bottom half. Restore and comment out the top half of the bottom half.
5. Repeat until you've narrowed to a few lines.

**Important:** Undo all your bisect changes before implementing the fix. Bisecting is diagnostic, not therapeutic.

## State Inspection Patterns

### Runtime Variables

When you suspect a variable has the wrong value:

1. **Add a log at the assignment point** — not just the usage point. You need to know when and why it changed.
2. **Check initialization** — is the variable set to a sane default? Is it set before first use?
3. **Check all writers** — search for every place that assigns to this variable. One of them is wrong.
4. **Check timing** — is the variable read before it's written? Race conditions between initialization order.

### Config Files

When behavior doesn't match what the code says:

1. **Verify the config is loaded** — add logging to the config-loading code
2. **Check for overrides** — project settings, user settings, command-line args, environment variables
3. **Check file paths** — is the code loading the config you think it's loading?
4. **Check parse errors** — a malformed config may silently use defaults

### Logs

When reading existing logs:

1. **Find the last normal log entry** — the bug happened between this and the first abnormal entry
2. **Check timestamps** — gaps in logging may indicate a hang or crash
3. **Filter by subsystem** — use tags/categories to isolate relevant entries
4. **Look for warnings before errors** — warnings often foreshadow the real problem

## When to Add Logging vs Read Existing Logs

| Situation | Action |
|-----------|--------|
| You don't understand the code flow | Read existing logs first, then add logging at decision points |
| You understand the flow but not the values | Add targeted logging at the suspect code path |
| The bug is intermittent | Add persistent logging (not just for this debug session) with a tag you can filter |
| The bug happens in production/CI only | Add logging that will survive to the next CI run — don't remove it after |
| You're about to add more than 10 log lines | Stop. You're fishing. Form a hypothesis first. |

### Structured Logging

Always use structured, tagged logging:

```
# Good — filterable, searchable, informative
Debug.log("physics", "wheel %d slip: lateral=%.2f longitudinal=%.2f" % [i, lat, lon])

# Bad — noise in the log, no context, can't filter
print("slip is " + str(slip))
```

## Post-Mortem Process

After fixing a significant bug, document:

1. **Symptom** — what was observed
2. **Root cause** — what actually went wrong and why
3. **Fix** — what was changed and why that addresses the root cause
4. **Prevention** — what can be done to prevent similar bugs (test, lint rule, design change, documentation)
5. **Detection** — how could this bug have been caught earlier? (better tests, better logging, code review focus area)

Update project memory or documentation with any new gotchas, patterns, or rules discovered during the investigation. The next agent or developer shouldn't have to rediscover this knowledge.

## Checklist: Before You Say "Fixed"

- [ ] I can explain the root cause in one sentence
- [ ] I have a test that fails without the fix and passes with it
- [ ] I verified the fix doesn't break related tests
- [ ] I checked for similar patterns elsewhere in the codebase that might have the same bug
- [ ] I updated documentation/memory with the finding
- [ ] The commit message explains the root cause, not just "fix bug"


## Topic Pages

- [The Debugging Protocol](skill-the-debugging-protocol.md)
- [Signal Tracing in Event-Driven Architectures](skill-signal-tracing-in-event-driven-architectures.md)
- [Common Pitfalls](skill-common-pitfalls.md)

