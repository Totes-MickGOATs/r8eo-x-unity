---
description: init-project detail phases 2-7 — engine, GitHub, local setup, identity, verification, next steps
---

# Initialize New Project — Phases 2–7

Part of the [init-project command](./init-project.md).

---

## Phase 2: Engine Selection

Ask the user which game engine they're using:
- **Godot** (GDScript) — fully supported with CI, linting, testing, skills
- **Unity** (C#) — basic CI stubs, MCP integration
- **Unreal** (C++) — empty stubs, placeholder CI

Run the setup script:
```bash
bash tools/setup-engine.sh <engine>
```

Verify: `.mcp.json` exists, `justfile.engine` exists, `engine/` directory is gone, `.github/template-config.json` has correct ENGINE value.

---

## Phase 3: GitHub Configuration

### Branch Protection

```bash
gh api repos/{owner}/{repo}/branches/main/protection \
  --method PUT \
  --input - <<'EOF'
{
  "required_status_checks": { "strict": true, "contexts": ["Lint & Preflight"] },
  "enforce_admins": true,
  "required_pull_request_reviews": { "required_approving_review_count": 0 },
  "restrictions": null
}
EOF
```

### MERGE_TOKEN Secret (CRITICAL for auto-merge queue)

1. Go to https://github.com/settings/tokens → "Generate new token (classic)"
2. Name: `MERGE_TOKEN for <repo-name>`. Scopes: `repo` (full) + `workflow`
3. Set as repo secret: `gh secret set MERGE_TOKEN`

**Why:** GitHub Actions workflows triggered by GITHUB_TOKEN cannot trigger other workflows. The auto-merge queue needs MERGE_TOKEN to trigger CI on the merge commit.

### Enable GitHub Actions

```bash
gh api repos/{owner}/{repo}/actions/permissions
```

If disabled, enable in Settings > Actions > General.

---

## Phase 4: Local Setup

```bash
uv sync                                # Install Python dependencies
git config core.hooksPath .githooks    # Configure git hooks path
chmod +x .githooks/* .claude/hooks/* .claude/*.sh
git config core.hooksPath             # Verify: should output .githooks
```

Test main branch protection:
```bash
# This SHOULD fail with "Direct commit to main blocked"
echo "test" > /tmp/test-hook.txt
git add /tmp/test-hook.txt 2>/dev/null || true
git commit -m "test: hook check" 2>&1 || echo "Hook correctly blocked commit on main"
git reset HEAD /tmp/test-hook.txt 2>/dev/null || true
```

---

## Phase 5: Project Identity

Guide the user to customize these files:
1. **README.md** — Replace template text with game name, description, features
2. **`.github/template-config.json`** — Set `GAME_NAME` to the actual game name
3. **`.ai/knowledge/status/project-status.md`** — Initialize phase tracking
4. **`.ai/knowledge/architecture/coding-standards.md`** — Add project-specific conventions
5. **`cliff.toml`** — In `postprocessors`, replace `https://github.com/YOUR_ORG/YOUR_REPO` with actual URL
6. **`CLAUDE.md`** — Fill in engine-specific sections, add project overview

---

## Phase 6: Verification

```bash
just --list                 # List available recipes (should show base + engine recipes)
just python-lint            # Run Python lint (should pass on clean template)
just validate-registry      # Validate the system registry
```

Then test the full branch workflow end-to-end:

```bash
just worktree-create test-setup
echo "# Test" >> README.md
git add README.md
git commit -m "test: verify template setup"
git push -u origin feat/test-setup
gh pr create --base main --title "test: verify template setup" --body "Testing template workflow"
just watch-ci               # Watch CI — should pass
just worktree-cleanup test-setup
```

---

## Phase 7: Next Steps

Tell the user:
- Run `/dev:next-task` to see what to work on first
- Read `CLAUDE.md` for the complete workflow reference
- Read `.agents/skills/branch-workflow/SKILL.md` for detailed git workflow
- Start building: `just worktree-create first-feature`

---

## Troubleshooting

| Issue | Solution |
|-------|---------|
| "Permission denied" on hooks | `chmod +x .githooks/* .claude/hooks/* .claude/*.sh` |
| CI not triggering | Check GitHub Actions is enabled in repo Settings > Actions > General |
| Auto-merge stuck | Verify MERGE_TOKEN secret has correct scopes (`repo` + `workflow`) |
| `just` not found | Ensure it's in PATH, try opening a new terminal |
| `uv` not found | `curl -LsSf https://astral.sh/uv/install.sh | sh` |
| Pre-commit blocks on main | Intentional! Use `just worktree-create <task>` to work on a feature branch |
| git hooks not running | `git config core.hooksPath .githooks` |
| `jq` not found during engine setup | `apt install jq` or `winget install jqlang.jq` |
| Engine setup already ran | If `engine/` directory is gone, already run. Check `template-config.json`. |
