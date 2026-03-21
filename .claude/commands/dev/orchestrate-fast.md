---
description: Fast, hardware-aware Claude workflow orchestration for this repo
---

# Orchestrate Fast

User's task: $ARGUMENTS

Use this command to coordinate a small, fast swarm for repo work.

## Operating Style

- Optimize for wall-clock speed, low token waste, and low merge or rebase churn.
- Keep the lead agent coordinating and synthesizing. Do not start implementation while workers are still active.
- Prefer tmux split-pane mode if agent teams are enabled. Start with 3 teammates; only increase to 4 when work is clearly independent.
- Route models by role: `haiku` for explore, `opus` for planning, `sonnet` for builder or fixer.
- Use subagents mainly for research, review, and clearly partitioned workstreams. Keep task packets tight.
- Use Apple silicon headroom for search, review, synthesis, and log processing, not for overlapping writes.

## Hard Repo Rules

- Never edit on `main`.
- Code-writing subagents must start with `bash scripts/tools/safe-worktree-init.sh <task>`.
- Never use `isolation: "worktree"`.
- TDD first: write the failing test before implementation.
- Update manifests for new or moved source files.
- Serialize Unity compile and test jobs through one verifier queue.

## Concurrency Policy

- Max 4 concurrent read-only workers.
- Max 1 code-writing builder or fixer at a time.
- Max 1 Unity verification job at a time.
- Do not assign two workers to the same file.
- Sequence singleton, shared-config, and manifest edits.
- If in doubt, sequence.

## Complexity Gate

- Trivial tasks stay in the main thread.
- Multi-file, noisy, or uncertain tasks: parallelize read-only discovery and review first.
- Parallel code writing is allowed only when file ownership is fully disjoint and there is no shared singleton, config, or manifest conflict.
- If latency matters and the answer is already local, prefer the main thread over spawning workers.

## Execution Protocol

1. Intake: restate goal, acceptance, scope, and files.
2. Ground truth: verify the minimum repo facts directly.
3. Read-only parallel phase: research, grep, summarize, map test impact.
4. Synthesize: decide whether to stay single-threaded or split work.
5. Implementation phase: one builder at a time for each atomic chunk.
6. Review phase: read-only diff review before shipping.
7. Diff verification: inspect the actual changed files, not just reports.
8. Queue discipline: keep Unity verification serialized.
9. Iteration cap: fix only blocking issues, then converge.

## Agent Teams

- Use teams mainly for research, review, or clearly partitioned workstreams.
- Keep roughly 5-6 small tasks per teammate.
- Tell the lead to wait for teammates to finish before proceeding.
- Monitor and steer instead of leaving the team unattended.
- Clean up the team from the lead session only.

## Anti-Waste Rules

- Do not swarm simple tasks.
- Do not assign two workers to the same file.
- Use subagents for noisy output instead of flooding the lead.
- Sequence singleton and shared-config changes.
- Prefer the main thread when context is already local and the task is small.
- Keep implementation packets narrow and file-owned.

## Task Packet

Use this format for each worker:

```text
Task: <one atomic outcome>
Acceptance: <binary done condition>
Pattern: <reference file or method>
Allow: <exact paths this worker may touch>
Exclude: <what this worker must not change>
Tests: <test class and scenario names>
Done when: <specific verifiable state>
```

## Suggested Flow

- Start with one planner, up to two or three researchers, and one reviewer as needed.
- Promote to builders only after file ownership is partitioned and risk is clear.
- Keep the lead on synthesis, sequencing, and final diff verification.
