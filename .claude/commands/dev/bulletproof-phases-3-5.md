---
description: Bulletproof phases 3-5 — review loop, knowledge sync, completion report
---

# Bulletproof — Phases 3–5

Part of the [Bulletproof QA](./bulletproof.md) command.

---

## Phase 3: Review Loop

### Early Exit Ramp

If the implementation subagent reports ALL of:
- All tests green (actually run)
- Total lines changed < 50
- No new files created
- No cross-system wiring changes

Then: **drop to single review pass**. If that pass is clean, skip to Phase 4.

### Review Checklist

| # | Check | Severity |
|---|-------|----------|
| 1 | All acceptance criteria met | MUST FIX |
| 2 | Tests exist and PASS (actually run, not just read) | MUST FIX |
| 3 | No regressions in existing tests | MUST FIX |
| 4 | Follows project conventions (CLAUDE.md rules) | MUST FIX |
| 5 | No security issues (injection, XSS, etc.) | MUST FIX |
| 6 | Edge cases handled | MUST FIX |
| 7 | Contract sync — manifests, CLAUDE.md, assembly definitions updated | MUST FIX |
| 8 | Code is minimal — no over-engineering | SHOULD FIX |
| 9 | Type annotations on function signatures | SHOULD FIX |
| 10 | Debug logging used correctly | SHOULD FIX |
| 11 | No contradiction between new code and stored memory/docs | SHOULD FIX |

### Contract Sync Checklist (Check #7)

For EACH new/moved/deleted file, verify every applicable contract:

| Contract | Location | When to Update |
|----------|----------|----------------|
| System Manifest | `resources/manifests/<system>.tres` | New/deleted `.cs`/`.unity`/`.asset` in a system |
| Directory CLAUDE.md | `<dir>/CLAUDE.md` | File added/removed/renamed in that directory |
| Root CLAUDE.md | `CLAUDE.md` | New singleton manager, key script, architecture change |
| Assembly Definitions | `*.asmdef` | New script directory, changed dependency |
| Package Manifest | `Packages/manifest.json` | New Unity package added or removed |
| Memory files | `~/.claude/projects/.../memory/` | New system/pattern/convention |

### Loop Execution

```
pass_count = 0, consecutive_clean = 0, max_passes = 5

while consecutive_clean < 2 and pass_count < max_passes:
    dispatch review subagent:
        - Read ONLY files in the incremental diff scope
        - Evaluate against the review checklist
        - Fix any MUST FIX or SHOULD FIX issues found
        - Report: {findings, fixes_applied, remaining_issues}

    if remaining_issues empty and fixes_applied empty:
        consecutive_clean += 1
    else:
        consecutive_clean = 0  # fixes applied, need re-verification
    pass_count += 1
```

**Incremental scope:** Pass 1 reviews all files changed since Phase 2. Pass N+1 reviews only files changed since pass N.

### Rollback Protocol

If review loop hits max passes without convergence:
1. Preserve the work — do NOT revert
2. Summarize remaining issues (MUST FIX vs SHOULD FIX)
3. Present options: A) address manually, B) revert to pre-Phase-2 commit, C) ship as-is
4. Wait for user decision. Do NOT auto-revert.

---

## Phase 4: Knowledge Sync

After code is finalized, BEFORE reporting to the user.

Skip re-checking systems Phase 0 already verified as clean.

1. **Run `/dev:clean-loop`** for standard knowledge sync (lessons learned, documentation updates, memory hygiene).
2. **Contract verification punchlist** — for each contract from the Phase 1 impact list:
   - **Manifests:** `grep` relevant `.tres` to confirm new paths present and deleted paths removed
   - **Directory CLAUDE.md:** Read and confirm file listing matches `ls` output
   - **Root CLAUDE.md tables:** If singletons/key scripts/scene structure changed, confirm tables updated
   - **Assembly definitions:** Confirm `.asmdef` files exist and reference correct dependencies
   - **Package manifest:** If new Unity packages added, confirm `Packages/manifest.json` includes them
3. **Catch any contracts the implementation missed** — check `git diff --name-only` for any new files.

This phase is MANDATORY. Skipping it is how churn accumulates across sessions.

---

## Phase 5: Completion Report & Merge Readiness

Before reporting, ensure the branch is merge-ready:
1. All commits pushed to remote feature branch
2. CI is green — verify with `gh run list --branch <branch>`
3. If CI fails, fix, push, wait for green. Do NOT proceed with red CI.
4. Apply `ready-to-merge` label: `gh pr edit <number> --add-label "ready-to-merge"`

Present to the user:
1. **Summary** — what was done, 2-3 sentences
2. **Acceptance Criteria Status** — table: each AC as PASS/FAIL
3. **CI Status** — green/red, link to run
4. **PR Status** — PR number/URL, `ready-to-merge` applied
5. **Files Changed** — list with brief description
6. **Tests** — which tests written/modified, CI pass/fail
7. **Review Passes** — how many, what was caught and fixed
8. **Contracts Updated** — which manifests, CLAUDE.md files, declarations synced
9. **Knowledge Fixes** — any memory/doc corrections made
10. **Architecture Diagram** — only if structural changes (3+ files or new systems)
11. **Known Limitations** — acceptable "dents"
