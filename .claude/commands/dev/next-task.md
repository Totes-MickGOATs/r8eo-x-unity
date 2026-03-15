---
description: Pick up the next task from the project backlog
---

# Next Task

1. Check for urgent items first:
   ```bash
   gh issue list --label "ci-failure" --state open --limit 3
   gh issue list --label "bug" --state open --limit 3
   ```

2. Check the project status:
   - Read `.ai/knowledge/status/project-status.md`
   - Identify the current phase and incomplete items

3. Check for in-flight work from other agents/sessions:
   - Read any `project_inflight_*.md` memory files
   - Check for active worktrees: `just worktree-list`
   - If in-flight work affects the systems you're about to touch, factor it into your task selection and approach

4. Check for open issues:
   ```bash
   gh issue list --state open --limit 10
   ```

5. Pick the highest-priority task. Priority order:
   1. CI failures (blocking all merges)
   2. Bugs (affecting functionality)
   3. Current-phase incomplete items
   4. Enhancements

6. Create a feature branch and begin work.
