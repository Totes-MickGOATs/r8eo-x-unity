# Model Routing

> Part of the `swarm-development` skill. See [SKILL.md](SKILL.md) for the overview.

## Model Routing

Always set the `model` parameter explicitly when dispatching subagents. Inheriting the orchestrator's model wastes cost or loses quality.

| Agent Type | Model | Reason |
|------------|-------|--------|
| **Explore** subagents (research, search, read-only) | `haiku` | Fast and cheap for information retrieval |
| **general-purpose** subagents (Builder, Fixer) | `sonnet` | Best code quality for implementation |
| **Plan** subagents (architecture, design) | `opus` | Deepest reasoning for complex decisions |

---

