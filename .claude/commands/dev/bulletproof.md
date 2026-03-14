Multi-pass quality assurance for code changes. Ensures requirements alignment, implementation quality, convergence, and knowledge hygiene before declaring done.

User's task: $ARGUMENTS

---

## Complexity Gate

Before entering the full bulletproof pipeline, assess the task scope:

| Criteria | LIGHT | FULL |
|----------|-------|------|
| Lines of code changed | < 50 | >= 50 |
| Files touched | 1-2 | 3+ |
| New files created | 0 | 1+ |
| Cross-system interaction | No | Yes |
| User explicitly requested thoroughness | No | Yes |

**LIGHT path** (any trivial change — typo, config tweak, single-function fix):
- Skip subagent dispatch — implement inline
- Phase 0: Quick churn check (30 seconds, not a deep audit)
- Phase 1: State AC briefly, ask user to confirm (no scope diagram needed)
- Phase 2: Implement directly with TDD
- Phase 3: One self-review pass against the checklist, fix anything found, done
- Phase 4: Update docs only if directly affected
- Phase 5: Brief summary (no architecture diagram)

**FULL path** (substantial changes): Run all phases as described below.

If in doubt, start LIGHT — escalate to FULL if complexity reveals itself during implementation.

---

## Phase 0: Churn Detection & Self-Reflection

Before starting, check for signals that we've been here before or are working against stale context.

### Trigger Signals

Evaluate these signals with **weighted scoring**, not single-keyword matching:

| Signal | Weight | Examples |
|--------|--------|----------|
| Explicit frustration | HIGH | "we've tried this 3 times", "this keeps breaking", "how many times" |
| Repeated task | HIGH | Substantially similar task found in memory/recent conversation |
| Conflicting sources | MEDIUM | CLAUDE.md says X, memory says Y, code does Z |
| Dead references | MEDIUM | Memory/docs reference files or classes that don't exist |
| Stale status | LOW | Memory says "IN PROGRESS" for something that's done |
| Ambiguous phrasing | IGNORE | "let's try this" (could be new task, not frustration) |

**Fire threshold:** At least one HIGH signal, OR two or more MEDIUM signals.
Single LOW or ambiguous signals alone do NOT trigger cleanup — just note them and proceed.

### If threshold met:

1. **Diagnose the root cause** — don't just fix the symptom. Ask:
   - Is there a memory entry causing the agent to repeat a wrong approach?
   - Is there a CLAUDE.md file with outdated instructions that contradict the current code?
   - Is there a deleted/renamed file still referenced in docs?
   - Are two memory files or CLAUDE.md files giving conflicting guidance?
2. **Fix the knowledge base FIRST** before touching any game code:
   - Update or remove the offending memory entries
   - Correct stale CLAUDE.md documentation
   - Remove references to deleted files/systems
   - Reconcile conflicting guidance (pick the one that matches actual code, delete the other)
   - Commit each documentation fix immediately
3. **Report to the user** what was found and cleaned:
   ```
   CHURN DETECTED — Knowledge cleanup performed:
   - [what was wrong] → [what was fixed]
   - [what was wrong] → [what was fixed]
   Proceeding with clean context.
   ```
4. **If the root cause is unclear**, STOP and ask the user: "I'm seeing [X conflict]. Which is correct: [option A] or [option B]?"
5. **Record the systems verified as clean** — Phase 4 can skip re-checking these.

### If threshold NOT met:

Proceed to Phase 1. Do not waste tokens on cleanup that isn't needed.

---

## Phase 1: Requirements Alignment

Before ANY code is written:

1. Parse the user's task into concrete acceptance criteria (AC). Each AC must be binary pass/fail.
2. **Ground-truth verification** — Before drafting AC, verify key assumptions about the current state of affected files using DIRECT tools (Read, Grep, `wc -l`), not subagent reports. Specifically:
   - Check actual file sizes (`wc -l`) for any file mentioned in AC
   - Check actual imports/references (`grep ClassName file.cs`) for any "is/isn't wired" claims
   - Check actual const/method existence (`grep _PARAMS file.cs`) for any "still exists" claims
   - **NEVER present AC based solely on subagent research** — always spot-check the critical claims yourself with 2-3 direct tool calls. Subagents can read stale data or hallucinate file contents.
3. Identify affected systems, files, and potential side effects.
4. **Identify affected contracts** — for each file expected to be created/moved/deleted, list which contracts will need updating (manifests, CLAUDE.md files, assembly definitions, constant classes). This becomes a checklist for Phase 3 review.
5. Cross-check against memory and CLAUDE.md — does anything in our stored knowledge contradict or complicate this task? Flag it now, not mid-implementation.
7. Present back to the user:
   - **Acceptance Criteria** — numbered list, each testable
   - **Scope Diagram** — ASCII/markdown visual showing affected files/systems and their relationships
   - **Contract Impact** — which manifests, CLAUDE.md files, and declarations will be updated (from step 4)
   - **Out of Scope** — explicitly state what this task does NOT include
   - **Risk Areas** — things that could break or need extra attention
   - **Context Conflicts** (if any) — "Memory says X but code does Y — which is correct?"
8. Ask the user: "Does this match your expectations? Any AC to add/remove/change?"
9. Do NOT proceed until the user confirms alignment. If anything is ambiguous, ask.

## Phase 2: Implementation (Subagent)

Once requirements are confirmed, dispatch an implementation subagent with:
- The confirmed acceptance criteria (copy them verbatim — no paraphrasing that could drift)
- Instructions to follow TDD (Red-Green-Commit cycle per CLAUDE.md)
- Instructions to invoke relevant skills for the domain (e.g., `unity-csharp-mastery`, `unity-physics-3d`, `unity-architecture-patterns`)
- Instructions to commit each change per project git rules
- Any context corrections from Phase 0 (so the subagent doesn't repeat old mistakes)
- **Context budget:** Only include CLAUDE.md sections and memory entries relevant to the affected systems. Do NOT dump the entire project context.
- Instructions to update all affected contracts as part of implementation (manifests, CLAUDE.md files, declarations) — not deferred to Phase 4
- The Phase 1 contract impact list (so the subagent knows exactly which contracts to update)
- The subagent should report back: files changed, tests written, tests run (with pass/fail output), total lines changed, contracts updated

### CI-First Testing (IMPORTANT)

**Local tests are for TDD feedback only.** The subagent should:
- Run ONLY the specific test file being developed using the `/dev:run-tests-unity` command or Unity MCP tools (`run_tests`, `read_console`)
- **NEVER run the full test suite locally.** It ties up the user's machine.
- After implementation is complete, push the branch and let CI run the full suite on GitHub Actions (GameCI).
- Check CI results with `gh run list --branch <branch>` and `gh run view --log-failed`.
- If CI fails, diagnose from CI output, fix locally, push again. Repeat until green.
- **CI is the source of truth**, not local test runs.

## Phase 3: Review Loop

The review loop improves quality iteratively until convergence.

### Early Exit Ramp

If the implementation subagent reports ALL of:
- All tests green (actually run)
- Total lines changed < 50
- No new files created
- No cross-system wiring changes

Then: **drop to single review pass**. If that pass is clean, skip straight to Phase 4. No need for two consecutive clean passes on simple changes.

### Review Checklist (used by every review pass)

Each reviewer evaluates against:

| # | Check | Severity |
|---|-------|----------|
| 1 | All acceptance criteria met | MUST FIX |
| 2 | Tests exist and PASS (actually run, not just read) | MUST FIX |
| 3 | No regressions in existing tests | MUST FIX |
| 4 | Follows project conventions (CLAUDE.md rules) | MUST FIX |
| 5 | No security issues (injection, XSS, etc.) | MUST FIX |
| 6 | Edge cases handled | MUST FIX |
| 7 | **Contract sync** — all new/moved/deleted files reflected in manifests, CLAUDE.md listings, assembly definitions (see Contract Sync Checklist below) | MUST FIX |
| 8 | Code is minimal — no over-engineering, no unnecessary additions | SHOULD FIX |
| 9 | Type annotations on function signatures | SHOULD FIX |
| 10 | Debug logging used correctly (no bare `Debug.Log` in production code) | SHOULD FIX |
| 11 | No contradiction between new code and stored memory/docs | SHOULD FIX |

**Convergence rule:** Only MUST FIX and SHOULD FIX items block convergence. Cosmetic/style nits do NOT.

### Contract Sync Checklist (referenced by Review Check #7)

Every review pass MUST verify these contracts are in sync with the actual code changes. This is the primary defense against drift between code and declarations.

**How to verify:** Run `git diff --cached --name-only` (or the review diff) and for EACH new/moved/deleted file, check every applicable contract:

| Contract | Location | When to Update | How to Verify |
|----------|----------|----------------|---------------|
| **System Manifest** | `resources/manifests/<system>.tres` | New `.cs`/`.unity`/`.asset` added to a system, file deleted/moved, new system created | `grep -l "the_file_path" resources/manifests/*.tres` — if no match, it's missing |
| **Directory CLAUDE.md** | `<dir>/CLAUDE.md` | File added/removed/renamed in that directory, behavior changed | Read the CLAUDE.md and check the file listing matches reality |
| **Root CLAUDE.md** | `CLAUDE.md` | New singleton manager, new key script, new convention, architecture change | Check tables (Singletons, Key Scripts, Scene Structure) match `ProjectSettings/` and disk |
| **Assembly Definitions** | `*.asmdef` files | New script directory, new test assembly, dependency between assemblies changed | Verify `.asmdef` references match actual assembly dependencies |
| **Package Manifest** | `Packages/manifest.json` | New Unity package dependency added or removed | `Packages/manifest.json` entries match what code actually imports |
| **Manifest CLAUDE.md** | `resources/manifests/CLAUDE.md` | New manifest `.tres` created or deleted | File listing table matches `ls resources/manifests/*.tres` |
| **Memory files** | `~/.claude/projects/.../memory/` | New system/pattern/convention that future sessions need | Check if change invalidates or extends existing memory entries |
| **Layer/tag constants** | Layer/tag constants class | New collision layer or tag added | Constants match what's configured in `ProjectSettings/TagManager.asset` and used in code |
| **Surface type constants** | Surface type constants class | New terrain surface type added | Enum matches Unity Terrain layers |

**New file decision tree:**
```
New .cs/.unity/.asset file created?
  ├─ Is it part of an existing system? → Add to that system's manifest owned_scripts/scenes/resources
  ├─ Is it a new system? → Create new manifest .tres in resources/manifests/, update manifests/CLAUDE.md
  ├─ Is it a test file? → No manifest needed (tests are excluded), but add to tests/CLAUDE.md
  └─ Is it in an excluded dir (.agents, .claude)? → No manifest needed

  Always: Add to the parent directory's CLAUDE.md file listing.
```

**Deleted/moved file decision tree:**
```
File deleted or moved?
  ├─ Remove old path from manifest (grep to find which one)
  ├─ If moved: add new path to manifest
  ├─ Update parent directory CLAUDE.md (remove old, add new if moved)
  └─ Check root CLAUDE.md Key Scripts / Scene Structure tables
```

### Incremental Review Scope

- **Pass 1:** Review all files changed since implementation (full diff from before Phase 2)
- **Pass N+1 (N > 1):** Review ONLY files changed since pass N. If pass N fixed 3 files and pass N+1 only needs to verify those 3 files, don't re-read the other 10.
- Use `git diff <commit-before-pass-N>..HEAD` to scope each review precisely.

### Loop Execution

```
pass_count = 0
consecutive_clean = 0
max_passes = 5

while consecutive_clean < 2 and pass_count < max_passes:
    dispatch review subagent:
        - Read ONLY files in the incremental diff scope
        - Evaluate against the review checklist
        - Fix any MUST FIX or SHOULD FIX issues found
        - Report: {findings: [...], fixes_applied: [...], remaining_issues: [...]}

    if report.remaining_issues is empty and report.fixes_applied is empty:
        consecutive_clean += 1
    else:
        consecutive_clean = 0  # reset — fixes were applied, need re-verification

    pass_count += 1

if consecutive_clean < 2 and pass_count >= max_passes:
    → ESCALATE (see Rollback Protocol below)
```

After the loop, the orchestrator does one final validation:
- Verify all AC are met
- Verify tests pass
- Spot-check the review findings were properly addressed

### Rollback Protocol

If the review loop hits max passes without convergence:

1. **Preserve the work** — do NOT revert or stash. The commits are in git history.
2. **Summarize remaining issues** with their severity (MUST FIX vs SHOULD FIX).
3. **Present to the user:**
   ```
   REVIEW LOOP DID NOT CONVERGE after [N] passes.

   Remaining issues:
   - [MUST FIX] description (file:line)
   - [SHOULD FIX] description (file:line)

   Options:
   A) I address the remaining issues manually (continue without loop)
   B) We revert to [commit hash before Phase 2] and re-approach
   C) Ship as-is — these are acceptable "dents"
   ```
4. Wait for user decision. Do NOT auto-revert.

## Phase 4: Knowledge Sync

After code is finalized but BEFORE reporting to the user.

**Scope optimization:** Skip re-checking any systems that Phase 0 already verified as clean. Only check:
- Systems directly affected by the code changes
- Any new systems introduced
- Cross-references from other docs that mention the changed files

Checklist:
1. **Contract verification** (MUST DO — this is the regression firewall):
   - Run the Phase 1 contract impact list as a punchlist. For each contract identified:
     - **Manifests:** `grep` the relevant `.tres` file to confirm new file paths are present and deleted paths are removed. If a new system was created, confirm its `.tres` exists in `resources/manifests/` and is listed in `resources/manifests/CLAUDE.md`.
     - **Directory CLAUDE.md:** Read each affected directory's CLAUDE.md and confirm the file listing matches `ls` output (new files listed, removed files gone).
     - **Root CLAUDE.md tables:** If singletons, key scripts, or scene structure changed, confirm the root CLAUDE.md tables are updated.
     - **Assembly definitions:** If new script directories or assemblies were added, confirm `.asmdef` files exist and reference the correct dependencies.
     - **Package manifest:** If new Unity packages were added, confirm `Packages/manifest.json` includes them.
     - **Constant classes:** If new collision layers, tags, or surface types were added, confirm the constants class is updated.
   - **Catch any contracts the implementation missed** — the subagent may have created files not anticipated in Phase 1. Check `git diff --name-only` for any new files and verify each one is covered.
2. **Memory update** — Does this change invalidate anything in memory files?
   - If a memory entry is now wrong or stale, update or remove it. Commit immediately.
3. **CLAUDE.md update** — (Should already be done from step 1, but double-check) Does this change affect any directory's CLAUDE.md?
   - New files added → add to the listing
   - Files removed → remove from the listing (no "removed" comments)
   - Behavior changed → update the description
4. **Contradiction sweep** — Quick check: does the final state of the code match what memory and docs now say? If not, fix the docs.
5. **Skills & knowledge growth** — Reflect on the work just completed and ask:
   - **New skill opportunity:** Did this task involve a domain, pattern, or workflow that would benefit from a reusable skill (`.agents/skills/`)? If the same kind of work might come up again and there's no existing skill covering it, draft one.
   - **Existing skill update:** Did the implementation reveal new patterns, gotchas, or best practices that an existing skill should capture? If a skill was consulted during the work and it was missing information that would have helped, update it now.
   - **Memory gaps:** Beyond the stale/wrong check in step 1 — did this task teach us something new about the codebase, a tricky interaction, a non-obvious constraint, or a debugging insight that future sessions should know? If so, add or update the relevant memory file.
   - **Workflow improvement:** Did the process itself surface friction? A test that was hard to write, a pattern that should be extracted, a convention that should be documented? Capture it in the appropriate place (memory, CLAUDE.md, or skill).

   For each item identified, take action immediately — create/update the file and commit it. Don't just note "should update X" and move on.

This phase is MANDATORY. Skipping it is how churn accumulates across sessions.

## Phase 5: Completion Report & Merge Readiness

**Before reporting, ensure the branch is merge-ready:**

1. All commits pushed to the remote feature branch
2. CI is green — verify with `gh run list --branch <branch>`. If CI is still running, wait for it.
3. If CI fails, fix the issue, push, and wait for green. Do NOT proceed with a red CI.
4. Apply `ready-to-merge` label: `gh pr edit <number> --add-label "ready-to-merge"`
5. Confirm the auto-merge workflow will pick it up (no conflicts, up-to-date with main)

**A task with a failing CI or an unmerged branch is NOT complete.** If you cannot get CI green, report the blocker to the user in the completion report.

Present to the user:

1. **Summary** — what was done, in 2-3 sentences
2. **Acceptance Criteria Status** — table showing each AC as PASS/FAIL
3. **CI Status** — green/red, link to the run (`gh run view <id> --web`), any failures addressed
4. **PR Status** — PR number/URL, `ready-to-merge` label applied (yes/no), merge queue position
5. **Files Changed** — list with brief description of each change
6. **Tests** — which tests were written/modified, CI pass/fail status
7. **Review Passes** — how many passes, what was caught and fixed in each
8. **Contracts Updated** — which manifests, CLAUDE.md files, and declarations were synced
9. **Knowledge Fixes** — any memory/doc corrections made (Phase 0 or Phase 4)
10. **Architecture Diagram** — ASCII/markdown visual of the final state (only if structural changes: 3+ files or new systems)
11. **Known Limitations** — anything that works but could be better (the "dents" — not penetrated, but noted)

---

## Relevant Skills

Load these skills as needed during each phase:

| Skill | Location | When to Use |
|-------|----------|-------------|
| `unity-testing-debugging-qa` | `.agents/skills/unity-testing-debugging-qa/` | Phase 2 (TDD), Phase 3 (test verification) |
| `swarm-development` | `.agents/skills/swarm-development/` | Phase 2 (subagent dispatch), Phase 3 (review loop orchestration) |
| `clean-room-qa` | `.agents/skills/clean-room-qa/` | Phase 3 (black-box testing perspective) |
| `reverse-engineering` | `.agents/skills/reverse-engineering/` | Phase 0 (churn diagnosis), Phase 2-3 (debugging failures) |
| `unity-architecture-patterns` | `.agents/skills/unity-architecture-patterns/` | Phase 1 (scope analysis), Phase 2 (implementation patterns) |
| `unity-csharp-mastery` | `.agents/skills/unity-csharp-mastery/` | Phase 2 (implementation), Phase 3 (code review) |
| `unity-physics-3d` | `.agents/skills/unity-physics-3d/` | Phase 2 (physics implementation) |

---

## Token Efficiency Rules

These are hard constraints, not suggestions:

1. **Complexity gate routes small tasks to LIGHT path** — no subagent overhead for trivial changes
2. **Phase 0 is weighted scoring** — single ambiguous keywords don't trigger expensive cleanup audits
3. **Subagent context budgets** — each subagent receives ONLY the CLAUDE.md sections and memory entries relevant to affected systems. Never dump full project context.
4. **Incremental review diffing** — pass N+1 reads only files changed since pass N, not the full diff from the beginning
5. **Early exit ramp** — small clean implementations skip the two-consecutive-clean requirement
6. **Phase 0→4 cache** — systems verified clean in Phase 0 are not re-audited in Phase 4
7. **Review checklist is fixed** — no open-ended "find anything wrong" exploration
8. **Architecture diagram is conditional** — only for structural changes (3+ files or new systems)
9. **Use `git diff` to scope** — never re-explore the codebase when the diff tells you exactly what changed
10. **Ground-truth before dispatch** — before dispatching implementation subagents, verify critical claims (file sizes, wiring state, presence of old code) with direct `wc -l`, `grep`, or `Read` calls. A 2-second check prevents wasting thousands of tokens on work that's already done or based on stale data.

## Integration with Project Rules

- TDD cycle (CLAUDE.md) is enforced in Phase 2 — tests written first, run for RED, implement, run for GREEN
- Git commits happen per file/change as required by project rules
- System manifests updated if files are added to a system
- CLAUDE.md files updated if directory contents change
- When debugging failures in Phase 2 or 3, use the `reverse-engineering` skill's chain-of-custody methodology to trace from symptom to root cause before attempting fixes
