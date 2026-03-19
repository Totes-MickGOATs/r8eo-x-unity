#!/usr/bin/env python3
"""Unified pre-commit checks for C# files.

Runs module coverage ratchet and assert audit in a single process,
loading manifests once. Reads staged .cs file paths from stdin.

Usage:
    git diff --cached --name-only -- '*.cs' | uv run python scripts/tools/pre_commit_checks.py

Exit codes:
    0 — all checks passed
    1 — one or more checks failed (details on stdout)
"""

from __future__ import annotations

import json
import re as _re
import sys
from pathlib import Path

# ---------------------------------------------------------------------------
# Project paths
# ---------------------------------------------------------------------------
PROJECT = Path(__file__).resolve().parent.parent.parent
BASELINE_FILE = PROJECT / ".coverage-baseline.json"
MANIFEST_DIR = PROJECT / "resources" / "manifests"
TESTS_EDITMODE = PROJECT / "Assets" / "Tests" / "EditMode"
TESTS_PLAYMODE = PROJECT / "Assets" / "Tests" / "PlayMode"

# ---------------------------------------------------------------------------
# Shared manifest loading
# ---------------------------------------------------------------------------

def _load_manifests() -> list[dict]:
    """Load all JSON manifests from the manifest directory.

    Returns:
        List of parsed manifest dicts, sorted by filename.
    """
    manifests: list[dict] = []
    if not MANIFEST_DIR.exists():
        return manifests
    for json_file in sorted(MANIFEST_DIR.glob("*.json")):
        data = json.loads(json_file.read_text(encoding="utf-8"))
        manifests.append(data)
    return manifests


def _manifest_name(manifest: dict) -> str:
    """Return the canonical name for a manifest.

    Args:
        manifest: Parsed manifest dict.

    Returns:
        Name string, preferring 'name' over 'system' key.
    """
    return manifest.get("name") or manifest.get("system") or ""


# ---------------------------------------------------------------------------
# Check 1 — Coverage ratchet
# ---------------------------------------------------------------------------

_TEST_ATTR_RE = _re.compile(r"^\s*\[(Test|UnityTest)\]\s*$")


def _glob_test_files(pattern: str) -> list[Path]:
    results: list[Path] = []
    for test_dir in (TESTS_EDITMODE, TESTS_PLAYMODE):
        if test_dir.exists():
            results.extend(test_dir.glob(f"{pattern}.cs"))
    return results


def _count_test_attributes(test_file: Path) -> int:
    count = 0
    text = test_file.read_text(encoding="utf-8")
    for line in text.splitlines():
        if _TEST_ATTR_RE.match(line):
            count += 1
    return count


def _build_categories(manifests: list[dict]) -> dict[str, dict]:
    """Build test categories from manifest data.

    Args:
        manifests: Pre-loaded manifest list.

    Returns:
        Mapping of module name -> {test_patterns, source_patterns}.
    """
    categories: dict[str, dict] = {}
    for data in manifests:
        name = data.get("name") or data.get("system") or ""
        tests = data.get("tests", {})
        editmode = tests.get("editmode", [])
        playmode = tests.get("playmode", [])
        all_test_classes = editmode + playmode

        if not all_test_classes:
            continue

        test_patterns = [f"{cls}*" for cls in all_test_classes]
        source_dirs: set[str] = set()
        for file_path in data.get("files", []):
            p = Path(file_path)
            if p.suffix == ".cs":
                source_dirs.add(str(p.parent / "*.cs").replace("\\", "/"))

        categories[name] = {
            "test_patterns": test_patterns,
            "source_patterns": sorted(source_dirs),
        }
    return categories


def _gather_module_counts(categories: dict[str, dict]) -> dict[str, int]:
    """Count test methods per module.

    Args:
        categories: Module category map from _build_categories.

    Returns:
        Mapping of module name -> test count.
    """
    counts: dict[str, int] = {}
    for cat_name, data in categories.items():
        cat_tests = 0
        seen: set[str] = set()
        for pattern in data["test_patterns"]:
            for tf in _glob_test_files(pattern):
                if tf.name not in seen:
                    seen.add(tf.name)
                    cat_tests += _count_test_attributes(tf)
        counts[cat_name] = cat_tests
    return counts


def _resolve_modules_from_files(staged_paths: list[str], manifests: list[dict]) -> list[str]:
    """Map staged file paths to manifest module names.

    Args:
        staged_paths: List of staged .cs file paths (relative or absolute).
        manifests: Pre-loaded manifest list.

    Returns:
        List of module names that own the staged files.
    """
    matched: list[str] = []
    for manifest in manifests:
        name = _manifest_name(manifest)
        if not name:
            continue
        for file_entry in manifest.get("files", []):
            for staged in staged_paths:
                # Normalise to forward-slash relative paths for comparison
                staged_norm = staged.replace("\\", "/")
                if staged_norm.endswith(file_entry.replace("\\", "/").lstrip("/")):
                    if name not in matched:
                        matched.append(name)
                    break
    return matched


def run_coverage_ratchet(staged_paths: list[str], manifests: list[dict]) -> list[str]:
    """Check module coverage ratchet for staged files.

    Args:
        staged_paths: Staged .cs file paths.
        manifests: Pre-loaded manifest list.

    Returns:
        List of violation strings (empty = pass).
    """
    if not BASELINE_FILE.exists():
        return []  # No baseline yet — first-time commit, allow through

    baseline = json.loads(BASELINE_FILE.read_text(encoding="utf-8"))
    prev_cats: dict = baseline.get("categories", {})

    module_names = _resolve_modules_from_files(staged_paths, manifests)
    if not module_names:
        return []

    categories = _build_categories(manifests)
    counts = _gather_module_counts(categories)
    normalized = [m.strip().lower() for m in module_names if m.strip()]

    violations: list[str] = []
    for cat_name, curr_count in counts.items():
        cat_lower = cat_name.lower()
        if not any(cat_lower.startswith(mod) or mod in cat_lower for mod in normalized):
            continue
        prev = prev_cats.get(cat_name, {})
        prev_count = prev.get("test_count", 0)
        if curr_count < prev_count:
            violations.append(
                f"  Coverage regression — {cat_name}: {prev_count} -> {curr_count} "
                f"(lost {prev_count - curr_count} test(s))"
            )
    return violations


# ---------------------------------------------------------------------------
# Check 2 — Assert audit (inlined from assert_audit.py)
# ---------------------------------------------------------------------------

_ASSERTION_PREFIXES = (
    "Assert.",
    "Assume.",
    "StringAssert.",
    "CollectionAssert.",
    "LogAssert.",
)
_YIELD_ASSERT_RE = _re.compile(r"yield\s+return\s+\w+")
_TEST_METHOD_ATTR_RE = _re.compile(r"^\[(Test|UnityTest|TestCase)\b")


def _find_test_methods(lines: list[str]) -> list[tuple[int, str, bool]]:
    results: list[tuple[int, str, bool]] = []
    pending_attr = False
    is_unity_test = False
    for i, line in enumerate(lines):
        stripped = line.strip()
        if _TEST_METHOD_ATTR_RE.match(stripped):
            pending_attr = True
            is_unity_test = "UnityTest" in stripped
            continue
        if pending_attr:
            if stripped == "" or stripped.startswith("["):
                if stripped.startswith("[") and "UnityTest" in stripped:
                    is_unity_test = True
                continue
            m = _re.search(r"\b(\w+)\s*\(", stripped)
            if m:
                method_name = m.group(1)
                if method_name not in ("if", "for", "while", "switch", "foreach", "using"):
                    results.append((i, method_name, is_unity_test))
            pending_attr = False
            is_unity_test = False
    return results


def _extract_method_body(lines: list[str], method_start: int) -> list[str]:
    body: list[str] = []
    depth = 0
    in_body = False
    for line in lines[method_start:]:
        for ch in line:
            if ch == "{":
                depth += 1
                if depth == 1:
                    in_body = True
            elif ch == "}":
                depth -= 1
                if in_body and depth == 0:
                    return body
        if in_body and depth > 0:
            body.append(line)
    return body


def _body_has_assertion(body: list[str], is_unity_test: bool) -> bool:
    for line in body:
        stripped = line.strip()
        for prefix in _ASSERTION_PREFIXES:
            if prefix in stripped:
                return True
        if is_unity_test and _YIELD_ASSERT_RE.search(stripped):
            return True
    return False


def run_assert_audit(test_paths: list[str]) -> list[str]:
    """Run assert audit on the given test file paths.

    Args:
        test_paths: Paths to C# test files under Assets/Tests/.

    Returns:
        List of violation strings (empty = pass).
    """
    violations: list[str] = []
    for path_str in test_paths:
        path = Path(path_str)
        if not path.exists():
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except OSError as e:
            violations.append(f"  {path}: could not read file: {e}")
            continue
        lines = text.splitlines()
        methods = _find_test_methods(lines)
        for line_idx, method_name, is_unity_test in methods:
            body = _extract_method_body(lines, line_idx)
            if not _body_has_assertion(body, is_unity_test):
                violations.append(f"  {path}:{line_idx + 1}:{method_name} — missing assertion")
    return violations


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> int:
    """Run all pre-commit checks against staged .cs files from stdin.

    Returns:
        0 if all checks passed, 1 if any check failed.
    """
    raw = sys.stdin.read().strip()
    if not raw:
        return 0

    staged_paths = [p.strip() for p in raw.splitlines() if p.strip()]
    if not staged_paths:
        return 0

    # Load manifests ONCE — shared by all checks
    manifests = _load_manifests()

    failed = False

    # Check 1 — Coverage ratchet
    ratchet_violations = run_coverage_ratchet(staged_paths, manifests)
    if ratchet_violations:
        print("Coverage ratchet violations:")
        for v in ratchet_violations:
            print(v)
        failed = True

    # Check 2 — Assert audit (test files only)
    test_paths = [p for p in staged_paths if "Assets/Tests/" in p.replace("\\", "/")]
    if test_paths:
        audit_violations = run_assert_audit(test_paths)
        if audit_violations:
            print("Assert audit violations:")
            for v in audit_violations:
                print(v)
            failed = True

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
