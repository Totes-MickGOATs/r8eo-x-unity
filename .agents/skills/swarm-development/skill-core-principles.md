# Core Principles

> Part of the `swarm-development` skill. See [SKILL.md](SKILL.md) for the overview.

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

