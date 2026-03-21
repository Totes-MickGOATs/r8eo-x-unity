---
name: swarm-development
description: Swarm Development
---


# Swarm Development

Orchestrate multiple subagents to execute, review, and converge on complex tasks. Use this skill when a task is large enough that parallel execution and continuous objective review will produce higher quality than a single agent working alone.

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

## Integration with Project Workflow

This skill integrates with the project's existing workflow:

1. **Worktree isolation** — all swarm work happens in a worktree (per `branch-workflow` skill)
2. **TDD verification** — verifier agents run tests using `/dev:run-tests-unity` patterns
3. **Clean room QA** — reviewer agents can invoke `clean-room-qa` methodology for black-box testing
4. **Bulletproof checklist** — Phase 5 verification maps to `/dev:bulletproof` quality phases
5. **Definition of Done** — convergence criteria align with the project's DoD in CLAUDE.md

---

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

## Related Skills

| Skill | How It Integrates |
|-------|-------------------|
| **`branch-workflow`** | Worktree creation, PR lifecycle, and sequential task coordination |
| **`clean-room-qa`** | Black-box test methodology for verifier agents |
| **`reverse-engineering`** | Root cause analysis for bug fix swarms |
| **`unity-testing-patterns`** | Test execution patterns for verification phase |
| **`debug-system`** | Structured logging for diagnosing agent issues |
| **`subagent-patterns`** | Lessons learned: singleton bottlenecks, rebase cascade cost, diff verification (`.ai/knowledge/architecture/subagent-patterns.md`) |


## Topic Pages

- [Core Principles](skill-core-principles.md)
- [Output Format](skill-output-format.md)
- [Sequential Coordination Mode](skill-sequential-coordination-mode.md)
- [Cleanup](skill-cleanup.md)
- [Example: Skill Creation Swarm](skill-example-skill-creation-swarm.md)
- [Convergence Criteria](skill-convergence-criteria.md)
- [Anti-Patterns](skill-anti-patterns.md)
- [When to Use](skill-when-to-use.md)
- [Model Routing](skill-model-routing.md)

