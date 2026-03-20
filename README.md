# R8EO-X (Unity)

AI-powered realistic RC racing simulation — Unity edition. Built with the mcgoats-game-template infrastructure for CI/CD, auto-merge queues, TDD enforcement, and Claude Code integration.

---

## First Time? Start Here

This project is designed to work with **Claude Code** (Anthropic's AI coding assistant). It will guide you through setup and development.

### Option A: Interactive AI Setup (Recommended)

Open a terminal in this project directory and run:

```bash
claude
```

Then type:

```
/setup
```

Claude will guide you through everything step-by-step: checking prerequisites, configuring GitHub branch protection, verifying your CI pipeline, and getting your Unity project ready for development.

### Option B: Manual Setup

If you prefer to set things up yourself, follow the checklist in **[SETUP.md](SETUP.md)**.

### Already Set Up?

Once your project is configured, here are the key commands to know:

| What you want to do | Tell Claude |
|---------------------|-------------|
| Start a new feature | `"Create a feature branch for <description>"` |
| Fix a bug | `"Fix this bug: <description>"` |
| Run tests | `/dev:run-tests` |
| Review code quality | `/dev:review-code` |
| Ship your changes | `"Push and create a PR"` |
| Check CI status | `/ci:fix-ci` (if CI is failing) |

All development happens on feature branches — Claude handles the branching, commits, PRs, and CI for you.

---

## What You Get

- **3-layer main branch protection** — Claude Code PreToolUse hook + git pre-commit hook + GitHub branch protection
- **Auto-merge queue** — FIFO squash-merge triggered by `ready-to-merge` label
- **CI pipeline** — C# compilation check + common lint
- **Post-merge safety net** — Full test suite runs after merge, auto-creates issues on failure
- **Claude Code integration** — Hooks, commands, statusline, subagent quality gates
- **TDD enforcement** — Red-Green-Commit workflow baked into project conventions
- **Worktree-based branching** — Isolated feature branches
- **Unity MCP integration** — UnityMCP for editor interaction from Claude Code

## Project Structure

```
├── Assets/                       # Unity project assets
├── Packages/                     # Unity package manifest
├── ProjectSettings/              # Unity project settings
├── CLAUDE.md                     # AI workflow rules
├── justfile                      # Task runner
├── .github/workflows/            # CI: auto-merge, lint, post-merge tests
├── .githooks/                    # Pre-commit (main branch guard), pre-push
├── .claude/                      # Claude Code hooks, commands, settings
├── .agents/skills/               # AI agent skills library
├── .ai/knowledge/                # Architecture docs, plans, status
├── scripts/tools/                # Python validation scripts
├── resources/manifests/          # System manifest files
└── tests/                        # Test directory
```

## Requirements

- **Unity 2022+** (URP recommended)
- **Rider** or **Visual Studio** for C# development
- **Python 3.14+** with [uv](https://docs.astral.sh/uv/)
- **just** task runner (`cargo install just` or via package manager)
- **gh** CLI (GitHub CLI)
- **Claude Code** CLI (for AI agent features)

## License

MIT — see [LICENSE](LICENSE).

Built by [McGOATs Games](https://github.com/Totes-MickGOATs).
