# swarm-development/

Orchestrate parallel subagents to build, review, and converge on complex tasks. Defines the swarm protocol: Build → Review → Fix → Re-Review → Ship, with objective convergence criteria.

## Files

| File | Role |
|------|------|
| `SKILL.md` | Swarm protocol, reviewer roster, dispatch templates, convergence criteria, anti-patterns |

## When to Use

- Tasks involving 3+ files across different concerns
- High quality bar requiring independent objective review
- Parallel execution + continuous review before shipping

## Related Skills

- **`branch-workflow`** — Worktree isolation for swarm agents
- **`clean-room-qa`** — Black-box verification methodology
- **`reverse-engineering`** — Root cause analysis for bug fix swarms
- **`unity-testing-patterns`** — Test execution for verification phase
- **`subagent-patterns`** — Hard-won lessons for multi-agent dispatch (`.ai/knowledge/architecture/subagent-patterns.md`)
