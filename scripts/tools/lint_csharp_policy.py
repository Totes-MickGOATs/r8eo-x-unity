#!/usr/bin/env python3
"""lint_csharp_policy.py — Policy linter for C# runtime assemblies.

Rules enforced:
  1. DEBUG_LOG    — Direct Debug.Log* in runtime assemblies
  2. FIND_OBJECT  — FindObjectOfType/GameObject.Find/Resources.Load in runtime assemblies
  3. GUID_ASMDEF  — Raw GUID references in .asmdef files
  4. STRING_LOOKUP — Layer/tag/scene string lookups in runtime assemblies
  5. ORPHAN_FILE  — Runtime .cs files not owned by any manifest
  6. NO_TESTS     — Production .cs files in manifests with zero editmode tests (advisory)

Usage:
  lint_csharp_policy.py --staged
  lint_csharp_policy.py --changed-against <ref>
  lint_csharp_policy.py --all

Exit codes:
  0 — no unallowlisted violations
  1 — at least one unallowlisted violation
  2 — script error (bad args, file not found, etc.)
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path
from typing import NamedTuple

# ---------------------------------------------------------------------------
# Allowlist
# Known baseline violations — allowlisted until they are migrated to RuntimeLog.
# Format: "relative/path/to/file": [line_numbers_to_skip] or "all" to skip all
#         violations in file. Use "orphan" to allow orphan-file rule only.
# Remove entries as each file is migrated to RuntimeLog.
# Tracked in: Logs/lint_baseline.md
# ---------------------------------------------------------------------------
POLICY_ALLOWLIST: dict[str, list[int] | str] = {
    # Debug.Log* baseline — tracked in Logs/lint_baseline.md
    # Remove entries as each file is migrated to RuntimeLog
    "Assets/Scripts/Vehicle/RCCar.cs": "all",
    "Assets/Scripts/Vehicle/RaycastWheel.cs": "all",
    "Assets/Scripts/Vehicle/Drivetrain.cs": "all",
    "Assets/Scripts/GameFlow/SceneBootstrapper.cs": "all",
    "Assets/Scripts/Track/SurfaceZone.cs": "all",
    "Assets/Scripts/UI/UIManager.cs": "all",
    # Manifest orphan allowlist
    "Assets/Scripts/Input/R8EOXInputActions.cs": "orphan",
}

# ---------------------------------------------------------------------------
# Runtime scope
# ---------------------------------------------------------------------------
RUNTIME_PATHS: tuple[str, ...] = (
    "Assets/Scripts/Vehicle/",
    "Assets/Scripts/Input/",
    "Assets/Scripts/Camera/",
    "Assets/Scripts/Core/",
    "Assets/Scripts/GameFlow/",
    "Assets/Scripts/Track/",
    "Assets/Scripts/UI/",
    "Assets/Scripts/Shared/",
)

EXEMPT_PATHS: tuple[str, ...] = (
    "Assets/Scripts/Editor/",
    "Assets/Scripts/Debug/",
    "Assets/Tests/",
)

# ---------------------------------------------------------------------------
# Regex patterns
# ---------------------------------------------------------------------------
RE_DEBUG_LOG = re.compile(
    r"\bDebug\.(Log|LogWarning|LogError|LogException|LogFormat|Assert)\b"
)
RE_FIND_OBJECT = re.compile(
    r"\b(FindObjectOfType|FindObjectsOfType|GameObject\.Find|Resources\.Load)\b"
)
RE_STRING_LOOKUP = re.compile(
    r"\b(LayerMask\.NameToLayer|GameObject\.FindWithTag|SceneManager\.LoadScene)\s*\(\s*\""
)
RE_GUID_VALUE = re.compile(r'"GUID:[0-9a-fA-F]+"')
RE_RAW_GUID = re.compile(r'"[0-9a-f]{32}"')


# ---------------------------------------------------------------------------
# Data types
# ---------------------------------------------------------------------------
class Violation(NamedTuple):
    rule: str
    file: str
    line: int
    message: str


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _repo_root() -> Path:
    """Return the repository root by walking up from this script."""
    here = Path(__file__).resolve()
    for parent in here.parents:
        if (parent / ".git").exists():
            return parent
    # Fallback: cwd
    return Path.cwd()


def _is_runtime_path(rel_path: str) -> bool:
    """Return True if the file is in a runtime assembly directory."""
    for exempt in EXEMPT_PATHS:
        if rel_path.startswith(exempt):
            return False
    for runtime in RUNTIME_PATHS:
        if rel_path.startswith(runtime):
            return True
    return False


def _is_allowlisted(rel_path: str, line_no: int, rule: str) -> bool:
    """Return True if this (file, line, rule) is covered by the allowlist."""
    if rel_path not in POLICY_ALLOWLIST:
        return False
    entry = POLICY_ALLOWLIST[rel_path]
    if entry == "orphan":
        return rule == "ORPHAN_FILE"
    if entry == "all":
        return True
    if isinstance(entry, list):
        return line_no in entry
    return False


def _load_manifests(repo_root: Path) -> list[dict]:
    """Load all manifest JSON files from resources/manifests/."""
    manifest_dir = repo_root / "resources" / "manifests"
    manifests = []
    if manifest_dir.is_dir():
        for mf in sorted(manifest_dir.glob("*.json")):
            try:
                manifests.append(json.loads(mf.read_text(encoding="utf-8")))
            except (json.JSONDecodeError, OSError):
                pass
    return manifests


def _build_manifest_index(manifests: list[dict]) -> dict[str, dict]:
    """Return a dict mapping each file path to its owning manifest."""
    index: dict[str, dict] = {}
    for m in manifests:
        for f in m.get("files", []):
            index[f] = m
    return index


# ---------------------------------------------------------------------------
# Rules
# ---------------------------------------------------------------------------

def check_line_rules(rel_path: str, lines: list[str]) -> list[Violation]:
    """Apply per-line rules (DEBUG_LOG, FIND_OBJECT, STRING_LOOKUP) to a .cs file."""
    violations: list[Violation] = []
    if not _is_runtime_path(rel_path):
        return violations

    for i, line in enumerate(lines, start=1):
        if RE_DEBUG_LOG.search(line) and not _is_allowlisted(rel_path, i, "DEBUG_LOG"):
            violations.append(Violation(
                "DEBUG_LOG", rel_path, i,
                "Direct Debug.Log in runtime assembly — use RuntimeLog.Log instead",
            ))
        if RE_FIND_OBJECT.search(line) and not _is_allowlisted(rel_path, i, "FIND_OBJECT"):
            violations.append(Violation(
                "FIND_OBJECT", rel_path, i,
                "FindObjectOfType/GameObject.Find/Resources.Load in runtime assembly"
                " — use injected references instead",
            ))
        if RE_STRING_LOOKUP.search(line) and not _is_allowlisted(rel_path, i, "STRING_LOOKUP"):
            violations.append(Violation(
                "STRING_LOOKUP", rel_path, i,
                "String-based layer/tag/scene lookup in runtime assembly"
                " — use named constants instead",
            ))
    return violations


def check_asmdef(rel_path: str, content: str) -> list[Violation]:
    """Check .asmdef file for raw GUID references."""
    violations: list[Violation] = []
    try:
        data = json.loads(content)
    except json.JSONDecodeError:
        return violations

    references = data.get("references", [])
    for ref in references:
        if not isinstance(ref, str):
            continue
        is_guid = RE_GUID_VALUE.search(f'"{ref}"') or RE_RAW_GUID.match(f'"{ref}"')
        if is_guid and not _is_allowlisted(rel_path, 0, "GUID_ASMDEF"):
            violations.append(Violation(
                "GUID_ASMDEF", rel_path, 0,
                f"Raw GUID reference in asmdef: {ref!r}"
                " — use assembly name references instead",
            ))
    return violations


def check_orphan(rel_path: str, manifest_index: dict[str, dict]) -> list[Violation]:
    """Check if a runtime .cs file is owned by any manifest."""
    if not rel_path.endswith(".cs"):
        return []
    if not _is_runtime_path(rel_path):
        return []
    if rel_path in manifest_index:
        return []
    if _is_allowlisted(rel_path, 0, "ORPHAN_FILE"):
        return []
    return [Violation(
        "ORPHAN_FILE", rel_path, 0,
        "Runtime .cs file is not owned by any manifest"
        " — add it to resources/manifests/<module>.json",
    )]


def check_no_tests(rel_path: str, manifest_index: dict[str, dict]) -> list[Violation]:
    """Advisory: runtime .cs file in a manifest with zero editmode tests."""
    if not rel_path.endswith(".cs"):
        return []
    if not _is_runtime_path(rel_path):
        return []
    manifest = manifest_index.get(rel_path)
    if manifest is None:
        return []
    editmode = manifest.get("tests", {}).get("editmode", [])
    if editmode:
        return []
    if _is_allowlisted(rel_path, 0, "NO_TESTS"):
        return []
    return [Violation(
        "NO_TESTS", rel_path, 0,
        f"File belongs to manifest '{manifest.get('name', '?')}'"
        " which has zero editmode tests — add test coverage",
    )]


# ---------------------------------------------------------------------------
# File-level linting
# ---------------------------------------------------------------------------

def lint_file(
    rel_path: str,
    abs_path: Path,
    manifest_index: dict[str, dict],
) -> list[Violation]:
    """Run all applicable rules on a single file."""
    violations: list[Violation] = []

    if rel_path.endswith(".asmdef"):
        try:
            content = abs_path.read_text(encoding="utf-8")
        except OSError as exc:
            print(f"lint_csharp_policy: cannot read {rel_path}: {exc}", file=sys.stderr)
            return violations
        violations.extend(check_asmdef(rel_path, content))
        return violations

    if rel_path.endswith(".cs"):
        try:
            lines = abs_path.read_text(encoding="utf-8").splitlines()
        except OSError as exc:
            print(f"lint_csharp_policy: cannot read {rel_path}: {exc}", file=sys.stderr)
            return violations
        violations.extend(check_line_rules(rel_path, lines))
        violations.extend(check_orphan(rel_path, manifest_index))
        violations.extend(check_no_tests(rel_path, manifest_index))

    return violations


# ---------------------------------------------------------------------------
# Changed-file selectors
# ---------------------------------------------------------------------------

def _run_git(args: list[str], cwd: Path) -> list[str]:
    """Run a git command and return stdout lines (empty on error)."""
    try:
        result = subprocess.run(
            ["git"] + args,
            cwd=str(cwd),
            capture_output=True,
            text=True,
        )
        return [line for line in result.stdout.splitlines() if line.strip()]
    except OSError:
        return []


def get_staged_files(repo_root: Path) -> list[str]:
    return _run_git(
        ["diff", "--cached", "--name-only", "--diff-filter=ACM"],
        repo_root,
    )


def get_changed_files(repo_root: Path, ref: str) -> list[str]:
    return _run_git(
        ["diff", "--name-only", "--diff-filter=ACM", ref, "HEAD"],
        repo_root,
    )


def get_all_files(repo_root: Path) -> list[str]:
    return _run_git(["ls-files"], repo_root)


def filter_lintable(files: list[str]) -> list[str]:
    """Keep only .cs and .asmdef files."""
    return [f for f in files if f.endswith(".cs") or f.endswith(".asmdef")]


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def run_lint(files: list[str], repo_root: Path) -> tuple[list[Violation], list[Violation]]:
    """Lint the given list of relative file paths. Returns (blocking, advisory)."""
    manifests = _load_manifests(repo_root)
    manifest_index = _build_manifest_index(manifests)
    lintable = filter_lintable(files)

    blocking: list[Violation] = []
    advisory: list[Violation] = []

    for rel_path in lintable:
        abs_path = repo_root / rel_path
        vs = lint_file(rel_path, abs_path, manifest_index)
        for v in vs:
            if v.rule == "NO_TESTS":
                advisory.append(v)
            else:
                blocking.append(v)

    return blocking, advisory


def _format_violation(v: Violation) -> str:
    loc = f"{v.file}:{v.line}" if v.line else v.file
    return f"POLICY [{v.rule}] {loc}: {v.message}"


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Policy linter for C# runtime assemblies",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--staged", action="store_true", help="Lint staged .cs/.asmdef files")
    group.add_argument(
        "--changed-against", metavar="REF", help="Lint files changed vs REF"
    )
    group.add_argument("--all", action="store_true", help="Lint all tracked files")

    args = parser.parse_args(argv)

    try:
        repo_root = _repo_root()
    except Exception as exc:  # noqa: BLE001
        print(f"lint_csharp_policy: error finding repo root: {exc}", file=sys.stderr)
        return 2

    if args.staged:
        files = get_staged_files(repo_root)
    elif args.changed_against:
        files = get_changed_files(repo_root, args.changed_against)
    else:
        files = get_all_files(repo_root)

    blocking, advisory = run_lint(files, repo_root)

    for v in advisory:
        print(_format_violation(v))
    for v in blocking:
        print(_format_violation(v))

    if blocking:
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
