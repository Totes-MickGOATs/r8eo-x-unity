# scripts/

Source code organized by subsystem. Add subdirectories as your project grows.

## Subdirectories

| Dir | Contents |
|-----|----------|
| `core/` | Project registry: system manifests, validation |
| `tools/` | Build utilities, asset generation, benchmarking |

## Root Scripts

| File | Role |
|------|------|
| `setup-worktree-sync.sh` | Activates post-commit continuous sync for parallel subagents |
| `create-integration-branch.sh` | Creates an integration branch for multi-subtask coordination |
| `cleanup-worktree.sh` | Removes a worktree and its associated feature branch |
| `cleanup-batch.sh` | Batch cleanup for multiple completed worktrees |
| `finalize-batch.sh` | Finalizes a batch run: promotes branches, removes tags |
| `wait-for-merge.sh` | Polls until a branch is promoted into local main |

## Conventions

- Shell: `#!/usr/bin/env bash`, `set -euo pipefail`, 2-space indent
- Python: 4-space indent, type hints on public functions, run via `uv run python`

See `.ai/knowledge/architecture/coding-standards.md` for general standards.
