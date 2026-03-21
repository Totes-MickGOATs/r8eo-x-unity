# Common Pitfalls

> Part of the `reverse-engineering` skill. See [SKILL.md](SKILL.md) for the overview.

## Common Pitfalls

### Correlation vs Causation

"The bug started happening after I changed X" does not mean X caused the bug. It might be:
- A pre-existing bug that X's change made visible
- A timing change that surfaced a race condition
- An unrelated change that was merged around the same time

**Test:** Revert X. Does the bug go away? If yes, X is involved (but may not be the root cause). If no, X is a red herring.

### Confirmation Bias

Once you have a hypothesis, you'll unconsciously filter evidence to support it. Counter this by:
- **Actively trying to disprove your hypothesis** — what evidence would contradict it?
- **Asking "what else could cause this?"** before committing to a fix
- **Having a second pair of eyes** review your diagnosis

### Fixing Symptoms, Not Causes

| Symptom Fix (Wrong) | Root Cause Fix (Right) |
|---------------------|----------------------|
| Add a null check before the crash | Fix why the variable is null |
| Clamp the value to a safe range | Fix why the value goes out of range |
| Add a retry loop | Fix why the operation fails |
| Catch and ignore the exception | Fix the condition that throws |

Symptom fixes are sometimes necessary as **temporary stopgaps** (to unblock a release), but they must always be followed by a root cause fix. Leave a `# TODO: root cause fix needed` comment with a description.

### Premature Code Changes

The most common debugging anti-pattern:

1. See a bug
2. Immediately start changing code to "fix" it
3. The change doesn't work, or introduces a new bug
4. Now you're debugging two problems

**Rule:** Do not change production code until you can explain the root cause in one sentence. If you can't articulate it, you haven't found it yet.

