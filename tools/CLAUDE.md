# tools/

Project setup and configuration scripts.

## Files

| File | Role |
|------|------|
| `setup-engine.sh` | Interactive engine setup: copies configs, hooks, CI, skills for chosen engine |
| `sync-template.sh` | Sync upstream template changes into the project |
| `audit-skill-usage.sh` | Scan git history for skill name mentions; report used vs unused skills |

## Relevant Skills

- `.agents/skills/branch-workflow/SKILL.md` -- uses worktree recipes after setup
