# Anti-Patterns

> Part of the `swarm-development` skill. See [SKILL.md](SKILL.md) for the overview.

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

