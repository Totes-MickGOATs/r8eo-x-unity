#!/usr/bin/env python3
"""Validate YAML frontmatter in loader-consumed markdown files.

Checks that all slash-command files and skill files begin with YAML frontmatter
(a line containing exactly '---').

Scope:
    - .claude/commands/**/*.md  (excluding CLAUDE.md files)
    - .agents/skills/**/SKILL.md

Usage:
    uv run python scripts/tools/validate_frontmatter.py [--ci]

Options:
    --ci    Suppress header output; print only file paths (one per line)

Exit codes:
    0   All files have frontmatter
    1   One or more files are missing frontmatter
"""

import argparse
import sys
from pathlib import Path


def find_target_files(repo_root: Path) -> list[Path]:
    """Find all loader-consumed markdown files that require frontmatter."""
    files: list[Path] = []

    # .claude/commands/**/*.md — excluding CLAUDE.md
    commands_root = repo_root / ".claude" / "commands"
    if commands_root.exists():
        for md_file in sorted(commands_root.rglob("*.md")):
            if md_file.name != "CLAUDE.md":
                files.append(md_file)

    # .agents/skills/**/SKILL.md
    skills_root = repo_root / ".agents" / "skills"
    if skills_root.exists():
        for skill_file in sorted(skills_root.rglob("SKILL.md")):
            files.append(skill_file)

    return files


def has_frontmatter(path: Path) -> bool:
    """Return True if the file starts with a YAML frontmatter marker ('---')."""
    try:
        with path.open(encoding="utf-8") as fh:
            first_line = fh.readline().rstrip("\n")
        return first_line == "---"
    except OSError:
        return False


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate YAML frontmatter in loader-consumed markdown files."
    )
    parser.add_argument(
        "--ci",
        action="store_true",
        help="Suppress header output; print only file paths (one per line)",
    )
    args = parser.parse_args()

    repo_root = Path(__file__).resolve().parent.parent.parent
    files = find_target_files(repo_root)

    failures: list[Path] = []
    for f in files:
        if not has_frontmatter(f):
            failures.append(f)

    if not args.ci:
        print(f"validate_frontmatter: checked {len(files)} file(s)")

    if failures:
        if not args.ci:
            print(f"FAIL: {len(failures)} file(s) missing YAML frontmatter:\n")
        for path in failures:
            rel = path.relative_to(repo_root)
            print(str(rel))
        if not args.ci:
            print("\nAdd '---\\ndescription: ...\\n---\\n' at the top of each file above.")
        return 1

    if not args.ci:
        print("OK: all files have YAML frontmatter")
    return 0


if __name__ == "__main__":
    sys.exit(main())
