---
description: Initialize a new project from this template — guides you and the user through complete setup
---

# Initialize New Project

You are helping the user set up a brand new game project created from the mcgoats-game-template.
Walk them through each step interactively, confirming completion before moving on.

**Phases 2–7 details:** [init-project-phases.md](./init-project-phases.md)

---

## Phase 1: Prerequisites

Check that required tools are installed. For each one, verify and report status:

1. **git** — `git --version` (need 2.x+)
2. **gh** — `gh auth status` (must be authenticated)
3. **uv** — `uv --version` (Python package manager)
4. **just** — `just --version` (command runner)

If any are missing, provide install instructions:
- git: https://git-scm.com/ or `winget install Git.Git`
- gh: `winget install GitHub.cli` or https://cli.github.com/
- uv: `curl -LsSf https://astral.sh/uv/install.sh | sh` or `winget install astral-sh.uv`
- just: `cargo install just` or `winget install Casey.Just`

---

## Phases Summary

| Phase | What Happens |
|-------|-------------|
| 1 (here) | Verify prerequisites (git, gh, uv, just) |
| 2 | Engine selection — Godot, Unity, or Unreal; run `tools/setup-engine.sh` |
| 3 | GitHub configuration — branch protection, MERGE_TOKEN secret, enable Actions |
| 4 | Local setup — Python dependencies, git hooks, test main-branch protection |
| 5 | Project identity — customize README, template-config, project-status, CLAUDE.md |
| 6 | Verification — `just --list`, `just python-lint`, `just validate-registry`, full branch workflow test |
| 7 | Next steps — `/dev:next-task`, read CLAUDE.md and branch-workflow skill |

All details for phases 2-7: [init-project-phases.md](./init-project-phases.md)

---

## Quick Reference

After setup completes, the user should be able to:
```bash
just --list                    # See all available recipes
just worktree-create <task>    # Start working on a feature
just lint-fast                 # Run blocking lint checks
just diagnose                  # Full health check
```
