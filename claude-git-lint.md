# CLAUDE.md — Git Workflow & Lint Stack Details

Expanded details for the root [CLAUDE.md](./CLAUDE.md).

---

## Git — Branch Workflow (Local-First)

> **MANDATORY:** All changes go through feature branches. Direct commits to main are blocked.
> Full details: `.agents/skills/branch-workflow/SKILL.md`

### Quick Reference

```
just lifecycle init <task>       # Create worktree + feature branch (subagent first action)
# ... TDD develop, commit ...    # Write tests, implement, commit
just queue-submit <branch>       # Submit branch to local verification queue
just queue-run                   # Run next queued verification (compile + targeted tests)
just queue-promote <branch>      # Fast-forward local main after verification passes
just task-complete <task>        # Main agent: cleanup after promotion
# --- Remote fallback (optional) ---
just lifecycle ship              # Push, create PR, merge, sync remote main
# --- Lint ---
just lint-fast                   # Blocking: C# syntax + policy on staged/changed files
just lint-csharp                 # Advisory: full-repo syntax + dotnet format
just lint-policy                 # Advisory: policy linter across all files
just lint-assets                 # Advisory: scene/asset lint (requires UNITY_PATH)
just lint-deep                   # Advisory: lint-csharp + registry + policy + assets
```

### Agent Rules (Hard)

- **NEVER** commit on `main` — PreToolUse hook blocks it
- **NEVER** use `--no-verify` — hook blocks it
- **NEVER** use `isolation: "worktree"` — subagents call `safe-worktree-init.sh`
- **ALWAYS** set `model` on Agent calls: `haiku` for Explore, `opus` for Plan, `sonnet` for general-purpose
- **ALWAYS** commit every file immediately after creating or modifying it
- **ALWAYS** complete the Task Intake checklist before writing any code (see CLAUDE.md)
- **ALWAYS** reference `CLAUDE_SKILLS.md` for available skills before starting

### Sequential Task Coordination

When the main agent decomposes work into multiple tasks:
1. Dispatch ONE subagent at a time — each performs one atomic, non-breaking task
2. After each branch is promoted: remove in-flight memories, update permanent memories
3. Full protocol: `.agents/skills/swarm-development/SKILL.md`

### Definition of Done

1. **Branch committed** with all changes
2. **Local tests pass** — coverage ratchet is a FAIL gate
3. **Branch promoted** — `just queue-promote <branch>` succeeded

### Remote — Dormant Fallback

Remote push and PRs are available as a fallback when CI validation or audit is required, or when collaborating with non-local agents. Use `just lifecycle ship`.

### Commit Rules

- Format: `type: short description` (e.g., `feat: add surface friction zone`)
- Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `ci`, `perf`, `style`, `build`

---

## Lint Stack

All C# lint is local (no GitHub Actions). Two tiers:

### Blocking (runs on every commit)

| Layer | Command | What it checks |
|-------|---------|----------------|
| 1 | Main branch guard | Blocks commits directly to `main` |
| 2 | Python ruff | Staged `.py` files |
| 3 | Engine hook | `.githooks/pre-commit-engine.sh` |
| 4 | C# line limit | 200-line limit per `.cs` file |
| 5 | C# syntax | Balanced braces, namespace presence (staged .cs files) |
| 6 | C# policy | Debug.Log*, FindObject, GUID refs, manifest orphans in runtime assemblies |
| — | Coverage ratchet | Per-module test count must not drop |
| — | Assert audit | Every test method must contain an assertion |

Run `just lint-fast` to invoke layers 5 and 6 manually on staged/changed files.

### Advisory (run explicitly, never block commits)

| Command | What it checks |
|---------|----------------|
| `just lint-csharp` | Full-repo syntax + dotnet format (requires dotnet SDK) |
| `just lint-policy` | Policy linter across all files |
| `just lint-assets` | Missing scripts, broken scene refs, layer audit (requires UNITY_PATH) |
| `just lint-deep` | lint-csharp + registry + policy + asset lint + assert audit |

### Why runtime logging is banned

Direct `UnityEngine.Debug.Log*` calls in runtime assemblies are blocked. Use `RuntimeLog.Log/LogWarning/LogError` from `R8EOX.Shared` instead.
Direct `Debug.Log*` is allowed in: `Assets/Scripts/Editor/`, `Assets/Scripts/Debug/`, `Assets/Tests/`.

### Why FindObjectOfType / GameObject.Find are banned

These APIs scan the entire scene graph. In runtime assemblies, use injected references (SerializeField or Initialize method) instead.
