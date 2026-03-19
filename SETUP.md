# Setup Guide

## Prerequisites

- **Git** with LFS support
- **GitHub CLI** (`gh`) — authenticated
- **uv** — Python package manager
- **just** — task runner
- **Unity 6 LTS** (set `UNITY_PATH` environment variable)

## Quick Setup

```bash
just setup
```

This installs Python dependencies, configures git hooks, and activates LFS.

## Manual Steps

1. **Clone**: `git clone <repo-url> && cd r8eo-x-unity`
2. **Install tools**: `brew install gh uv just` (macOS)
3. **Run setup**: `just setup`
4. **Set Unity path**: `export UNITY_PATH="/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity"`
5. **Verify**: `just check` (runs lint + registry validation)

## GitHub Configuration

- Enable branch protection on `main` (optional — local hooks enforce locally)
- All enforcement is local via git hooks — no GitHub Actions required

## Engine Selection

If starting from the template:

```bash
bash tools/setup-engine.sh unity
```

Or use the interactive setup: `/dev:init-project`
