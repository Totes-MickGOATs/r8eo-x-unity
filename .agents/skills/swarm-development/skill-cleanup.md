# Cleanup

> Part of the `swarm-development` skill. See [SKILL.md](SKILL.md) for the overview.

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

