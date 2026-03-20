---
name: swarm-development
description: Swarm Development
---

# Swarm Development

Orchestrate multiple subagents to execute, review, and converge on complex tasks. Use this skill when a task is large enough that parallel execution and continuous objective review will produce higher quality than a single agent working alone.

## When to Use

- Task involves creating or modifying 3+ files across different concerns
- Task requires both creative output AND objective verification
- Quality bar is high — the output must pass black-box tests, not just "look right"
- Task benefits from multiple perspectives (architecture, correctness, style, usability)
- You want to catch issues before they reach a PR, not after

**Do NOT use** for simple, well-scoped tasks (single file edits, bug fixes with clear root cause). The overhead of swarm coordination exceeds the benefit for small tasks.

---

## Core Principles

### 1. Separation of Concerns Between Agents

No agent should both create AND review its own work. The value of swarming comes from independent verification.

| Role | Responsibility | Can Edit? |
|------|---------------|-----------|
| **Builder** | Creates the initial output (code, skill, config) | Yes |
| **Reviewer** | Evaluates output against objective criteria | No — reports findings only |
| **Fixer** | Incorporates reviewer feedback into the output | Yes |
| **Verifier** | Runs tests, checks compilation, validates objectively | No — pass/fail only |

### 2. Objective Over Subjective

Reviews must be grounded in verifiable criteria, not opinion:

| Objective (prefer) | Subjective (avoid) |
|--------------------|--------------------|
| "Source article X has a section on Y that is not represented in the skill" | "I think the skill could be more thorough" |
| "This code example references `rb.velocity` which was renamed to `rb.linearVelocity` in Unity 6" | "The code examples could be improved" |
| "The `[TearDown]` method doesn't destroy the instantiated GameObject" | "Test cleanup could be better" |
| "The skills index CLAUDE.md was not updated" | "Documentation might need updating" |

### 3. Convergence, Not Infinite Iteration

Agents iterate until **all reviewers report zero actionable findings**, not until "it feels done." Define a maximum iteration count (typically 2-3 rounds) to prevent infinite loops.

### 4. Fail Fast, Fix Forward

If a reviewer finds a blocking issue (compilation error, missing file, broken test), the fixer addresses it immediately before the next review round. Non-blocking issues (style nits, optional improvements) are batched.

---

## The Swarm Protocol

### Phase 0: Decompose and Scope

Before dispatching any agents, the orchestrator (main agent) must:

1. **Define the deliverables** — exact files to create/modify, with expected content
2. **Define the quality gates** — what "done" means in objective, testable terms
3. **Choose the swarm shape** — which reviewer types are needed (see Reviewer Roster below)
4. **Create the worktree** — all builder/fixer agents work in the same isolated worktree

```
Quality Gates Example:
- [ ] All source material is represented (completeness)
- [ ] Code examples compile (accuracy)
- [ ] Follows project skill format conventions (consistency)
- [ ] Skills index updated (documentation)
- [ ] Tests pass if applicable (verification)
- [ ] PR created with CI green (delivery)
```

### Phase 1: Build

Dispatch the **Builder** agent with:
- Full task specification
- All source material / research results
- The worktree path to write to
- Explicit list of files to create

The Builder commits its work and reports completion. It does NOT push or create a PR.

```
Agent(
  subagent_type="general-purpose",
  description="Build [task name]",
  prompt="... full specification ...",
  model="sonnet",
  run_in_background=true
)
```

### Phase 2: Review (Parallel)

Once the Builder completes, dispatch **multiple Reviewer agents in parallel**, each with a different focus. Reviewers read the Builder's output and return a structured report.

Each reviewer MUST return findings in this format:

```markdown
## Review: [Focus Area]

### Blocking Issues (must fix before shipping)
1. [File:line] Description of issue
2. ...

### Non-Blocking Issues (should fix if time permits)
1. [File:line] Description of issue
2. ...

### Passing Checks
- [x] Check 1 passed
- [x] Check 2 passed

### Verdict: PASS | NEEDS_WORK | FAIL
```

**Dispatch pattern:**

```
# Launch all reviewers in parallel in a single message
Agent(description="Review completeness", model="sonnet", ...)
Agent(description="Review accuracy", model="sonnet", ...)
Agent(description="Review style", model="sonnet", ...)
Agent(description="Review practical value", model="sonnet", ...)
```

### Phase 3: Fix

If ANY reviewer returned `NEEDS_WORK` or `FAIL`:

1. Compile all reviewer findings into a single fix list
2. Dispatch a **Fixer** agent with the compiled findings
3. The Fixer addresses all blocking issues and as many non-blocking issues as practical
4. The Fixer commits and reports completion

### Phase 4: Re-Review (if needed)

If Phase 3 ran, dispatch reviewers again — but ONLY the ones that reported issues. Reviewers that passed in Phase 2 do not need to re-review unless the Fixer changed files in their domain.

**Convergence check:** If all reviewers return `PASS`, proceed to Phase 5. If not, repeat Phase 3-4. Maximum 3 iterations.

### Phase 5: Verify and Ship

1. Run objective verification (compilation check, test execution, lint)
2. Push the branch
3. Create PR with summary of all review rounds
4. Enable auto-merge
5. Monitor CI — if it fails, fix and push again

---

## Reviewer Roster

Choose reviewers based on the task type. Not every task needs all reviewers.

### For Skill/Documentation Creation

| Reviewer | Focus | Key Questions |
|----------|-------|---------------|
| **Completeness** | Coverage of source material | Is every source article represented? Are there gaps? |
| **Accuracy** | Technical correctness | Are API names correct? Do code examples compile? Are Unity version details right? |
| **Style** | Project conventions | Does it match existing skill format? Tables, headers, code blocks consistent? |
| **Practical Value** | Developer usefulness | Are examples actionable? Is the decision tree useful? Would a developer find what they need quickly? |

### For Code Implementation

| Reviewer | Focus | Key Questions |
|----------|-------|---------------|
| **Architecture** | Design quality | Does it follow project patterns? Is it properly decoupled? |
| **Correctness** | Logic and edge cases | Does it handle null, zero, negative, boundary cases? |
| **Testing** | Test coverage | Do tests exist? Do they follow TDD? Are they black-box? |
| **Integration** | System wiring | Does it integrate correctly with existing systems? |

### For Bug Fixes

| Reviewer | Focus | Key Questions |
|----------|-------|---------------|
| **Root Cause** | Diagnosis quality | Was the root cause identified, not just the symptom? |
| **Regression** | Side effects | Could this fix break anything else? Is there a regression test? |
| **Clean Room QA** | Independent verification | Does a black-box test confirm the fix? (See `clean-room-qa` skill) |

---

## Model Routing

Always set the `model` parameter explicitly when dispatching subagents. Inheriting the orchestrator's model wastes cost or loses quality.

| Agent Type | Model | Reason |
|------------|-------|--------|
| **Explore** subagents (research, search, read-only) | `haiku` | Fast and cheap for information retrieval |
| **general-purpose** subagents (Builder, Fixer) | `sonnet` | Best code quality for implementation |
| **Plan** subagents (architecture, design) | `opus` | Deepest reasoning for complex decisions |

---

## Agent Dispatch Templates

### Builder Template

```
You are working in a git worktree at [WORKTREE_PATH] on branch [BRANCH_NAME].

## Task
[Full specification of what to build]

## Source Material
[All research, fetched content, requirements]

## Files to Create/Modify
- [file1] — [description]
- [file2] — [description]

## Quality Gates
- [ ] [Gate 1]
- [ ] [Gate 2]

## Git Instructions
- Commit after creating each file
- Commit message: [type]: [description]
- Do NOT push or create PR — the orchestrator handles that
- Do NOT use --no-verify
```

### Reviewer Template

```
You are reviewing work done in [WORKTREE_PATH] on branch [BRANCH_NAME].

## Your Review Focus: [FOCUS AREA]

Read the following files and evaluate them against the criteria below:
- [file1]
- [file2]

## Review Criteria
1. [Criterion 1]
2. [Criterion 2]
3. [Criterion 3]

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

## Reviewer Findings to Address

### From [Reviewer 1]:
[blocking issues]

### From [Reviewer 2]:
[blocking issues]

### Non-Blocking (address if practical):
[combined list]

## Instructions
1. Address ALL blocking issues
2. Address non-blocking issues where practical
3. Commit fixes with message: fix: address review feedback
4. Do NOT push or create PR
5. Do NOT use --no-verify
```

---

## Convergence Criteria

The task is **done** when ALL of the following are true:

1. **All reviewers return PASS** — zero blocking issues across all review dimensions
2. **Objective verification passes** — compilation, tests, lint all green
3. **Definition of Done met** — all quality gates from Phase 0 are checked
4. **PR is open with CI green** — auto-merge enabled or `ready-to-merge` label applied

If convergence is not reached after 3 review rounds, the orchestrator must:
1. Escalate remaining issues to the user
2. Explain what's blocking convergence
3. Ask for direction on which issues to prioritize vs. defer

---

## Anti-Patterns

| Anti-Pattern | Why It's Bad | Instead |
|-------------|-------------|---------|
| **Builder reviews its own work** | Confirmation bias — you can't objectively review what you just created | Separate builder and reviewer agents |
| **Subjective review criteria** | "Could be better" is not actionable — leads to infinite iteration | Define objective, verifiable criteria |
| **No maximum iterations** | Agents loop forever seeking perfection | Cap at 3 rounds, escalate remaining issues |
| **Fixing during review** | Reviewer changes files, corrupting the review process | Reviewers report only, fixers edit |
| **Skipping verification** | "Tests probably pass" — they don't until you run them | Always run tests/compilation before shipping |
| **Swarming simple tasks** | Overhead exceeds benefit for single-file changes | Use swarm only for 3+ file, multi-concern tasks |
| **All reviewers every round** | Wasted work — reviewers that passed don't need to re-review | Only re-dispatch reviewers that found issues |

---

## Integration with Project Workflow

This skill integrates with the project's existing workflow:

1. **Worktree isolation** — all swarm work happens in a worktree (per `branch-workflow` skill)
2. **TDD verification** — verifier agents run tests using `/dev:run-tests-unity` patterns
3. **Clean room QA** — reviewer agents can invoke `clean-room-qa` methodology for black-box testing
4. **Bulletproof checklist** — Phase 5 verification maps to `/dev:bulletproof` quality phases
5. **Definition of Done** — convergence criteria align with the project's DoD in CLAUDE.md

---

## Sequential Coordination Mode

### When to Use

- **Multi-task plans where tasks must be serialized** — shared files, architectural dependencies, or sequential features that build on each other
- Tasks that touch overlapping systems where parallel dispatch would cause merge conflicts or semantic drift
- Plans where later tasks depend on the architectural decisions made in earlier tasks

**Contrast with Swarm Mode:** Swarm mode dispatches parallel builder+reviewer agents for ONE complex task. Sequential Coordination dispatches ONE subagent at a time across MULTIPLE atomic tasks, with memory-mediated awareness between each dispatch.

### The Protocol

1. **Plan:** Decompose the work into atomic, independently-mergeable tasks. Each task should produce a shippable PR on its own.
2. **Dispatch:** Send one subagent at a time — **without** `isolation: "worktree"`. The subagent calls `bash scripts/tools/safe-worktree-init.sh <task>` as its first action. Provide it with any in-flight context from previous tasks.
3. **Report:** The subagent completes the task, watches CI through merge, updates local main, and reports back with a summary of changes made, files affected, and downstream implications.
4. **Memory Bridge:** The main agent creates or updates `project_inflight_<task>.md` in memory with: branch name, affected files/systems, and what downstream agents should watch for.
5. **Next Task:** Dispatch the next subagent, providing it with in-flight awareness context so it can peek at relevant branches or anticipate changes landing in main.
6. **Post-Merge Cleanup:** After each PR merges — delete the in-flight memory entry, update permanent memories to reflect the now-live architecture.

### Interface-First Strategy

For coupled work where Task B depends on Task A's output, land the interface/contract first in a small PR. This eliminates the "I built against what I assumed the API would be" failure mode.

**The pattern:**

1. **Task A (small PR):** Create the interface, abstract class, or API contract that defines the boundary between the two tasks. Merge it.
2. **Task B (full PR):** Implement against the real, merged interface — not a guess or assumption about what Task A will produce.

**Why this costs less than it seems:** The extra small PR takes 5-10 minutes and one CI cycle. The alternative — Task B guessing the API and needing rework when Task A's actual output differs — costs 30-60 minutes of rework and a second review cycle.

**Example:**
- Task A lands `ISurfaceDetector` interface with method signatures and documentation. Merges.
- Task B implements `TerrainSurfaceDetector : ISurfaceDetector`, coding against the real interface with full IDE support and type checking.

**When to skip:** If the interface between tasks is trivial (e.g., a single file path or constant value), a note in the in-flight memory is sufficient. Use Interface-First when the contract involves method signatures, data structures, or event protocols.

### In-Flight Memory Template

Create `project_inflight_<task>.md` in the memory directory with this format:

```markdown
# In-Flight: <task-name>

## Branch
`feat/<task-name>` — PR #<number>

## Status
OPEN | MERGING | MERGED

## Affected Systems
- <system1> — <what changed>
- <system2> — <what changed>

## Files Modified
- `path/to/file1` — <summary>
- `path/to/file2` — <summary>

## Downstream Watch Items
- <thing the next agent should know about>
- <API change, new file, renamed constant, etc.>

## Migration Fence (optional — include when replacing an existing system/pattern)
- **Old system:** <name and path of the system being replaced>
- **New system:** <name and path of the replacement>
- **Toggle:** <feature toggle gating the new system, if any>
- **Directive:** Do NOT add new code using <old system>. Use the new interface at <path>.
- **Migration complete when:** <criteria for removing the fence and old system>

## Cleanup
Delete this file after PR merges and permanent memories are updated.
```

### Migration Fence Protocol

When a task is **replacing** an existing system or pattern (not just adding new functionality), the in-flight memory MUST include a Migration Fence section. This prevents downstream agents from adding new code that uses the old system while the migration is in progress.

**When to use:** Any time a task introduces a new implementation that supersedes an existing one — new input system replacing old, new physics model replacing legacy, new UI framework replacing ad-hoc scripts, etc.

**How it works:**

1. The in-flight memory includes the “Migration Fence” section (see template above) with explicit directives
2. Downstream agents check in-flight memories before starting work (see SessionStart hook and `/dev:next-task`)
3. If a downstream agent needs to touch the system being migrated, it codes against the **new** interface, not the old one
4. The new system should be behind a feature toggle until migration completes (see `feature-toggles` skill for the toggle pattern and decision matrix)
5. Once the migration PR merges and the toggle is flipped, remove the fence from in-flight memory during post-merge cleanup

**Example fence directive:**

> System `LegacyInputManager` is being replaced by `R8EOX.Input` (New Input System). Do NOT add new code using `Input.GetAxis()` or `Input.GetButton()`. Use the new `InputActions` asset and `PlayerInput` component at `Assets/Scripts/Input/`. The new system is gated by `USE_NEW_INPUT_SYSTEM` define until migration completes.

### Anti-Patterns

| Anti-Pattern | Why It’s Bad | Instead |
|-------------|-------------|---------|
| **Dispatching all tasks in parallel without coordination** | Merge conflicts, semantic drift, duplicated work | Serialize tasks with memory bridges between each |
| **Skipping memory updates between tasks** | Downstream agents lack context, make conflicting decisions | Always update in-flight memories after each subagent reports |
| **Leaving stale in-flight memories after merge** | Future agents see phantom “in-flight” work that already landed | Delete in-flight entries and update permanent memories after merge |
| **Overloading a single subagent with multiple tasks** | Defeats atomicity — large PRs are harder to review and more likely to conflict | One atomic task per subagent |

> **See also:** `.ai/knowledge/architecture/subagent-patterns.md` for real-world failure cases — singleton file bottlenecks, rebase cascade costs, and why “CI green” doesn’t mean “correct.”

### When NOT to Use

- **Simple single-task work** — just dispatch one subagent directly
- **Tasks with zero overlap** — if tasks touch completely different files and systems, parallel dispatch is faster and safe
- **Swarm-appropriate tasks** — if one complex task needs parallel build+review, use Swarm Mode instead

---

## Parallel Coordination Mode

### When to Use

Parallel subagent dispatch is allowed ONLY when ALL of these conditions are true:

1. **System independence:** Tasks touch completely different system manifests (check `resources/manifests/`)
2. **File independence:** Zero overlap in files modified (check manifests and anticipate new files)
3. **No shared constants:** Tasks do not modify shared constants, enums, or configuration files
4. **No CLAUDE.md conflicts:** Tasks do not modify the same directory's `CLAUDE.md`
5. **Max 3 parallel branches:** Keeps rebase cascade cost manageable (3 branches = max 6 CI runs vs 2 sequential)

**Contrast with Sequential Coordination:** Sequential mode serializes tasks with memory bridges between each. Parallel mode dispatches all tasks simultaneously but demands strict file independence. If independence cannot be proven, use Sequential.

**Contrast with Swarm Mode:** Swarm mode uses parallel reviewers for ONE complex task. Parallel Coordination dispatches MULTIPLE independent tasks simultaneously, each handled by a single subagent.

### Independence Verification Checklist

Before dispatching parallel subagents, the main agent MUST perform this analysis and document the results in the conversation:

1. **List each task's target systems** by manifest name (from `resources/manifests/`)
2. **List each task's expected file modifications** (both existing files and anticipated new files)
3. **Verify zero intersection** between all task pairs — no shared files across any two tasks
4. **Verify no shared infrastructure files** — project settings, shared assembly definitions, root configuration files
5. **Document the independence analysis** explicitly in the conversation before dispatching
6. **Check in-flight memories** — read any `project_inflight_*.md` files. If another agent/session has in-flight work on a system that overlaps with any planned task, that task CANNOT be parallelized with the in-flight work. Either wait for the in-flight PR to merge, or sequence your task after it.

If ANY step fails verification, fall back to Sequential Coordination Mode.

### Parallel Dispatch Protocol

1. Main agent performs the independence verification checklist (above)
2. Dispatch all subagents simultaneously — **without** `isolation: "worktree"`, with explicit `model` params — each calls `safe-worktree-init.sh` as first action to get its own worktree and branch
3. Each subagent follows the standard branch workflow independently (develop, commit, push, PR, CI, auto-merge)
4. NO in-flight memory files needed — tasks are independent by definition, so there is no cross-task context to share
5. Main agent monitors all PRs and CI runs concurrently
6. If any PR introduces unexpected file overlap (merge conflict), immediately convert remaining unmerged tasks to sequential mode
7. After all PRs merge, update local main once: `git fetch origin && git update-ref refs/heads/main origin/main`

### When to Fall Back to Sequential

- Any file overlap discovered during or after dispatch
- Merge conflict on any PR
- A task's scope grows during implementation to touch another task's system
- More than 3 tasks in the plan (sequential is safer for larger batches)
- Any task needs to reference the output of another task

### Examples of Safe Parallel Work

These task combinations typically satisfy independence criteria:

- **UI polish** (ui manifest) + **physics tuning** (vehicle manifest) + **audio integration** (audio manifest)
- **Camera system update** (camera manifest) + **input rebinding** (input manifest)
- **New skill documentation** (skills directory) + **test coverage** (test assembly) — if they touch different systems
- **Shader work** (materials/shaders) + **terrain generation** (terrain manifest)

### Examples That MUST Be Sequential

These task combinations have inherent dependencies or overlap:

- Two physics subsystem changes (both touch vehicle manifest)
- Feature implementation + its tests if tests are in a shared test assembly
- Any task touching shared infrastructure (assembly definitions, project settings, package manifest)
- Feature toggle introduction + the feature it gates (toggle must land first)
- Two tasks that both need to update the same CLAUDE.md or skills index

### Anti-Patterns

| Anti-Pattern | Why It's Bad | Instead |
|-------------|-------------|---------|
| **Skipping independence verification** | Merge conflicts discovered post-dispatch waste all parallel benefit | Always run the full checklist before dispatching |
| **More than 3 parallel branches** | Rebase cascade cost grows quadratically — O(N^2) CI runs | Cap at 3 parallel; queue remaining tasks sequentially |
| **Assuming independence without checking manifests** | Hidden dependencies (shared utilities, shared test assemblies) cause conflicts | Explicitly verify via manifest file lists |
| **Continuing parallel after a conflict** | Cascading conflicts multiply rework | Fall back to sequential immediately |
| **Parallel tasks that modify the same CLAUDE.md** | Guaranteed merge conflict on documentation | Either serialize, or designate one task as the CLAUDE.md owner |

---

## Example: Skill Creation Swarm

Here's how the swarm was used to create the `unity-architecture-patterns` skill:

```
Phase 0: Decompose
  - 22 source URLs to compile
  - Deliverables: SKILL.md, CLAUDE.md, skills index update
  - Quality gates: completeness, accuracy, style, practical value

Phase 1: Build
  - 22 fetch agents dispatched in parallel (one per URL)
  - 3 URLs failed → user provided content manually
  - Builder agent compiled all research into skill files

Phase 2: Review (4 agents in parallel)
  - Completeness reviewer: cross-referenced all 22 sources
  - Accuracy reviewer: checked code examples and API references
  - Style reviewer: validated format against existing skills
  - Practical value reviewer: evaluated from developer perspective

Phase 3: Fix
  - Fixer addressed all blocking issues from reviewers

Phase 4: Re-Review
  - Only reviewers with issues re-checked
  - All returned PASS

Phase 5: Ship
  - Pushed, PR created, CI green, auto-merge enabled
```

---

## Related Skills

| Skill | How It Integrates |
|-------|-------------------|
| **`branch-workflow`** | Worktree creation, PR lifecycle, and sequential task coordination |
| **`clean-room-qa`** | Black-box test methodology for verifier agents |
| **`reverse-engineering`** | Root cause analysis for bug fix swarms |
| **`unity-testing-patterns`** | Test execution patterns for verification phase |
| **`debug-system`** | Structured logging for diagnosing agent issues |
| **`subagent-patterns`** | Lessons learned: singleton bottlenecks, rebase cascade cost, diff verification (`.ai/knowledge/architecture/subagent-patterns.md`) |
