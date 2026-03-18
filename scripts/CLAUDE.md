# scripts/

Source code organized by subsystem. Add subdirectories as your project grows.

## Subdirectories

| Dir | Contents |
|-----|----------|
| `core/` | Project registry: system manifests, validation |
| `tools/` | Build utilities, asset generation, benchmarking |

## Conventions

- Shell: `#!/usr/bin/env bash`, `set -euo pipefail`, 2-space indent
- Python: 4-space indent, type hints on public functions, run via `uv run python`

See `.ai/knowledge/architecture/coding-standards.md` for general standards.
