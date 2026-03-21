# Sequential Coordination Mode

> Part of the `swarm-development` skill. See [SKILL.md](SKILL.md) for the overview.

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

