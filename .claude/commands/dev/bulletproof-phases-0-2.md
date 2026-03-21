---
description: Bulletproof phases 0-2 — churn detection, requirements alignment, implementation dispatch
---

# Bulletproof — Phases 0–2

Part of the [Bulletproof QA](./bulletproof.md) command.

---

## Phase 0: Churn Detection & Self-Reflection

### Ask-First Prerequisite Check

> **MANDATORY:** Verify the Ask-First workflow (`.agents/skills/ask-first/SKILL.md`) has been completed.

| Check | How to Verify | If Missing |
|-------|---------------|------------|
| Phase 1 (Interrogate) completed | Agent can state problem in one sentence, confidence >= 3 | STOP — complete Phase 1 first |
| Phase 2 (Test-First) completed | Black-box tests confirmed RED | STOP — dispatch test-writing agent first |
| Full contract verified | Input chains, signal wiring, dependencies traced | STOP — complete contract verification |

### Churn Detection — Trigger Signals

| Signal | Weight | Examples |
|--------|--------|----------|
| Explicit frustration | HIGH | "we've tried this 3 times", "this keeps breaking" |
| Repeated task | HIGH | Substantially similar task in memory/recent conversation |
| Conflicting sources | MEDIUM | CLAUDE.md says X, memory says Y, code does Z |
| Dead references | MEDIUM | Memory/docs reference files or classes that don't exist |
| Stale status | LOW | Memory says "IN PROGRESS" for something that's done |
| Ambiguous phrasing | IGNORE | "let's try this" |

**Fire threshold:** At least one HIGH signal, OR two or more MEDIUM signals. Single LOW signals do NOT trigger.

### If threshold met:

1. Diagnose root cause — offending memory entry? Stale CLAUDE.md? Deleted/renamed file?
2. Fix the knowledge base FIRST — update/remove memory entries, correct stale CLAUDE.md, reconcile conflicts
3. Report: `CHURN DETECTED — Knowledge cleanup performed: [what was wrong] → [what was fixed]`
4. If root cause unclear, STOP and ask: "I'm seeing [X conflict]. Which is correct: [A] or [B]?"
5. Record systems verified as clean — Phase 4 can skip re-checking these.

---

## Phase 1: Requirements Alignment

Before ANY code is written:

1. Parse the task into concrete acceptance criteria (AC). Each AC must be binary pass/fail.
2. **Ground-truth verification** — verify key assumptions with DIRECT tools (Read, Grep, `wc -l`), not subagent reports. Check actual file sizes, imports/references, const/method existence.
3. Identify affected systems, files, and potential side effects.
4. **Identify affected contracts** — for each file created/moved/deleted, list which contracts need updating (manifests, CLAUDE.md files, assembly definitions, constant classes).
5. Cross-check against memory and CLAUDE.md — flag conflicts now, not mid-implementation.
6. Present to the user:
   - **Acceptance Criteria** — numbered list, each testable
   - **Scope Diagram** — ASCII/markdown visual of affected files/systems
   - **Contract Impact** — which manifests, CLAUDE.md files, declarations will be updated
   - **Out of Scope** — what this task does NOT include
   - **Risk Areas** — things that could break or need extra attention
   - **Context Conflicts** (if any)
7. Ask: "Does this match your expectations? Any AC to add/remove/change?"
8. Do NOT proceed until the user confirms alignment.

---

## Phase 2: Implementation (Subagent)

Once requirements are confirmed, dispatch an implementation subagent with:

- **Model routing:** `model: "sonnet"` for implementation, `model: "opus"` for Plan agents, `model: "haiku"` for Explore agents
- The confirmed acceptance criteria (copy verbatim — no paraphrasing)
- TDD instructions (Red-Green-Commit cycle per CLAUDE.md)
- Relevant skill invocations for the domain
- Any context corrections from Phase 0
- **In-flight awareness:** Check for `project_inflight_*.md` memory files for affected systems
- **Context budget:** Only CLAUDE.md sections and memory entries relevant to affected systems
- Instructions to update all affected contracts as part of implementation
- The Phase 1 contract impact list
- Report back format: files changed, tests written, tests run (pass/fail output), total lines, contracts updated

### CI-First Testing (IMPORTANT)

- Run ONLY the specific test file being developed using `/dev:run-tests-unity` or MCP tools
- **NEVER run the full test suite locally.** It ties up the user's machine.
- After implementation, push the branch and let CI run the full suite.
- Check CI results with `gh run list --branch <branch>` and `gh run view --log-failed`.
- **CI is the source of truth**, not local test runs.

> **Multi-task plans:** Follow the Sequential Coordination protocol in `.agents/skills/swarm-development/SKILL.md` — dispatch one subagent per task.
