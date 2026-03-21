# Convergence Criteria

> Part of the `swarm-development` skill. See [SKILL.md](SKILL.md) for the overview.

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

