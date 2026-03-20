---
description: Set up this Unity project — interactive guided walkthrough
---

# Project Setup

Welcome! This command will walk you through setting up the R8EO-X Unity project.

Run the full interactive setup:

$ARGUMENTS

If no arguments are provided, run `/dev:init-project` which handles all phases:

1. **Prerequisites** — verify git, gh, uv, just are installed
2. **GitHub Configuration** — branch protection, MERGE_TOKEN secret, auto-merge
3. **Local Setup** — git hooks, Python dependencies
4. **Unity Setup** — verify Unity version, check MCP server config
5. **Project Identity** — customize CLAUDE.md, config files
6. **Verification** — test the full branch workflow end-to-end
7. **Next Steps** — what to build first

Note: This project is already configured for Unity (engine setup was already run).
Skip the engine selection phase — go straight to GitHub and local configuration.

After setup is complete, remind the user:
- Type `/dev:next-task` to see what to work on first
- All code changes go through feature branches — just describe what you want to build
- Claude handles branching, commits, PRs, and CI automatically
- Unity MCP server (UnityMCP) lets Claude interact with the Unity Editor directly
