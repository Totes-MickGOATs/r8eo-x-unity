# Clean Loop — End-of-Task Knowledge Capture

Use this skill at the end of every task to run cleanup, capture lessons learned, update documentation, sync memories, and verify clean git state before declaring done.

## Design Principles

- **FAST** — no subagent dispatch, no deep audits. 2-5 minutes max.
- **Checklist, not process.** Run each step, fix what's wrong, move on.
- **Complements `/dev:bulletproof`** Phase 4 but much lighter. Bulletproof is for big tasks; clean-loop is for everything.
- **Can be run standalone** or as the final step of any workflow.

## When to Run

- At the end of every task, before declaring "done"
- After any PR is pushed and CI is green
- Whenever you realize you learned something worth preserving

## Steps

### Step 1: Lessons Learned

Review what happened during the task:

1. **What went wrong?** — Unexpected failures, wrong assumptions, wasted effort
2. **What was surprising?** — Behavior that contradicted expectations, undocumented quirks
3. **What worked well?** — Patterns, tools, or approaches worth repeating

**Actions:**
- For significant bugs (Blocker/Major severity, or novel diagnosis): save a postmortem memory (`postmortem_<short_name>.md`) with issue summary, diagnostic signal, fix approach, and reusable pattern
- For new patterns or insights: save to the appropriate memory type (`feedback_`, `project_`, or `reference_`)
- Check if any existing memories are now stale or contradicted by this task's work — update or remove them
- Convert any relative dates in memory content to absolute dates

### Step 2: Documentation Sweep

For every directory where files were added, removed, or modified during this task:

1. **Read** that directory's `CLAUDE.md`
2. **Verify** the file listing matches reality:
   - New files are listed with accurate descriptions
   - Removed files are gone from the listing (no "removed" comments left behind)
   - Existing descriptions are still accurate
3. **Create** a `CLAUDE.md` if the directory doesn't have one (with skill references)
4. **Update skill references** if the work revealed relevant skills that weren't listed

### Step 3: Memory Check

- **In-flight cleanup:** Check for any `project_inflight_*.md` memory files. If this task's PR has merged, delete the corresponding in-flight memory. If other in-flight memories reference systems you just changed, update their "Downstream Watch Items" to reflect the now-live state.

Capture any knowledge gained during the task:

- **User feedback** — corrections, preferences, "don't do X" instructions → `feedback_<topic>.md`
- **Project context** — deadlines, decisions, architecture choices, who's working on what → `project_<topic>.md`
- **External references** — URLs, tools, dashboards, documentation links → `reference_<topic>.md`
- **Convert relative dates** to absolute dates (e.g., "next week" → "2026-03-21")

### Step 4: Uncommitted Changes Audit

1. Run `git status` — there should be **NO** uncommitted changes
2. If uncommitted changes exist:
   - Review each changed file
   - Verify no sensitive files are staged (`.env`, credentials, API keys, etc.)
   - Commit with appropriate conventional commit messages (`type: description`)
3. If sensitive files are found staged, unstage them and add to `.gitignore` if not already there

### Step 5: Status Update

If the task changed project state:

- **New system added** → verify manifest exists in `resources/manifests/`
- **Phase change or milestone reached** → update `.ai/knowledge/status/project-status.md`
- **System status changed** (ACTIVE to DEPRECATED, etc.) → update its manifest
- **No state change** → skip this step (most tasks)

## Output Format

Report concisely what was found and fixed:

```
## Clean Loop Complete

**Lessons:** [what was learned, or "none — routine task"]
**Docs updated:** [list of CLAUDE.md files touched, or "all current"]
**Memories:** [memories added/updated/removed, or "no changes"]
**Git state:** [clean / N files committed]
**Status:** [updates made, or "no project state changes"]
```

## Related Skills

- **`branch-workflow`** — Git workflow, PR lifecycle, Definition of Done
- **`swarm-development`** — Multi-agent coordination (clean-loop runs per-agent)
