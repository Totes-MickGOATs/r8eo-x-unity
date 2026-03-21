# Output Format

> Part of the `swarm-development` skill. See [SKILL.md](SKILL.md) for the overview.

## Output Format
Return your findings in EXACTLY this format:

### Blocking Issues (must fix)
[numbered list, or "None"]

### Non-Blocking Issues (should fix)
[numbered list, or "None"]

### Passing Checks
[checklist]

### Verdict: PASS | NEEDS_WORK | FAIL

Do NOT edit any files. Report findings only.
```

### Fixer Template

Dispatch the Fixer with `model="sonnet"` — it writes code and must produce quality output:

```
Agent(
  subagent_type="general-purpose",
  description="Fix review feedback for [task name]",
  prompt="... findings and instructions ...",
  model="sonnet",
  run_in_background=true
)
```

Fixer prompt template:

```
You are working in a git worktree at [WORKTREE_PATH] on branch [BRANCH_NAME].

