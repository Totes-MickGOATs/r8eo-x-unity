#!/usr/bin/env python3
"""Test coverage report — tracks tested vs untested game systems.

No addons needed. Counts [Test] and [UnityTest] attributes in test files
and reports per-category coverage using the tiered approach.

Modes:
  (default)       Human-readable report to stdout
  --json          Machine-readable JSON to stdout
  --ci            CI mode: compare against baseline, fail on regression
  --save-baseline Write current snapshot to .coverage-baseline.json
"""

from __future__ import annotations

import argparse
import io
import json
import re
import sys
from pathlib import Path

# Force UTF-8 output on Windows
if sys.stdout and hasattr(sys.stdout, "buffer"):
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

PROJECT = Path(__file__).resolve().parent.parent.parent
TESTS_EDITMODE = PROJECT / "Assets" / "Tests" / "EditMode"
TESTS_PLAYMODE = PROJECT / "Assets" / "Tests" / "PlayMode"
BASELINE_FILE = PROJECT / ".coverage-baseline.json"
MANIFEST_DIR = PROJECT / "resources" / "manifests"

# Regex to match [Test] and [UnityTest] attributes
TEST_ATTR_RE = re.compile(r"^\s*\[(Test|UnityTest)\]\s*$")


def load_categories_from_manifests() -> dict[str, dict]:
    """Build CATEGORIES from manifest tests fields.

    Maps module name -> { test_patterns, source_patterns }
    where test_patterns are built from tests.editmode + tests.playmode class names
    and source_patterns are derived from the manifest files[] paths.
    """
    categories: dict[str, dict] = {}

    for manifest_path in sorted(MANIFEST_DIR.glob("*.json")):
        data = json.loads(manifest_path.read_text(encoding="utf-8"))
        name = data.get("name") or data.get("system") or manifest_path.stem
        tests = data.get("tests", {})
        editmode = tests.get("editmode", [])
        playmode = tests.get("playmode", [])
        all_test_classes = editmode + playmode

        if not all_test_classes:
            continue  # Skip modules with no declared tests

        # Build test_patterns from class names (glob-friendly: "ClassName*")
        test_patterns = [f"{cls}*" for cls in all_test_classes]

        # Derive source_patterns from files[] entries
        # Group by directory prefix (e.g., Assets/Scripts/Vehicle/Physics/*.cs)
        source_dirs: set[str] = set()
        for file_path in data.get("files", []):
            p = Path(file_path)
            if p.suffix == ".cs":
                # Use forward slashes for glob consistency across platforms
                source_dirs.add(str(p.parent / "*.cs").replace("\\", "/"))
        source_patterns = sorted(source_dirs)

        categories[name] = {
            "test_patterns": test_patterns,
            "source_patterns": source_patterns,
        }

    return categories


# ── System categories loaded dynamically from manifests ───────────────────────
CATEGORIES = load_categories_from_manifests()


def _glob_test_files(pattern: str) -> list[Path]:
    """Find test files matching a glob pattern in both EditMode and PlayMode."""
    results: list[Path] = []
    for test_dir in (TESTS_EDITMODE, TESTS_PLAYMODE):
        if test_dir.exists():
            results.extend(test_dir.glob(f"{pattern}.cs"))
    return results


def count_test_attributes(test_file: Path) -> int:
    """Count [Test] and [UnityTest] attributes in a C# test file."""
    count = 0
    text = test_file.read_text(encoding="utf-8")
    for line in text.splitlines():
        if TEST_ATTR_RE.match(line):
            count += 1
    return count


def count_source_files(patterns: list[str]) -> int:
    """Count source files matching the given glob patterns."""
    files: set[Path] = set()
    for pattern in patterns:
        files.update(PROJECT.glob(pattern))
    return len(files)


def gather_snapshot() -> dict:
    """Build a coverage snapshot from actual test files."""
    categories = {}
    total_tests = 0

    for category, data in CATEGORIES.items():
        cat_tests = 0
        test_files_found: dict[str, int] = {}

        for pattern in data["test_patterns"]:
            matched = _glob_test_files(pattern)
            for tf in matched:
                if tf.name not in test_files_found:
                    n = count_test_attributes(tf)
                    test_files_found[tf.name] = n
                    cat_tests += n

        source_count = count_source_files(data["source_patterns"])

        categories[category] = {
            "test_count": cat_tests,
            "test_files": len(test_files_found),
            "source_files": source_count,
            "test_file_details": test_files_found,
        }
        total_tests += cat_tests

    return {
        "categories": categories,
        "total_tests": total_tests,
    }


def print_report(snap: dict) -> bool:
    """Print human-readable report. Returns True always (no target enforcement yet)."""
    print("=" * 72)
    print("TEST COVERAGE REPORT")
    print("=" * 72)

    for cat_name, cat in snap["categories"].items():
        src_info = f", {cat['source_files']} source files" if cat["source_files"] > 0 else ""
        print(
            f"\n  {cat_name} -- {cat['test_count']} tests "
            f"in {cat['test_files']} test files{src_info}"
        )
        for fname, count in cat["test_file_details"].items():
            print(f"    {fname:40s} {count} tests")

    print("\n" + "=" * 72)
    print(f"TOTAL: {snap['total_tests']} test methods")
    print("=" * 72)

    return True


def save_baseline(snap: dict) -> None:
    """Save snapshot as the coverage baseline."""
    from datetime import date

    baseline = {
        "generated": str(date.today()),
        "total_tests": snap["total_tests"],
        "categories": {
            name: {
                "test_count": cat["test_count"],
                "source_files": cat["source_files"],
            }
            for name, cat in snap["categories"].items()
        },
    }
    BASELINE_FILE.write_text(json.dumps(baseline, indent=2), encoding="utf-8")
    print(f"Baseline saved to {BASELINE_FILE}")


def ci_compare(snap: dict) -> int:
    """Compare against baseline. Fail on regression."""
    if not BASELINE_FILE.exists():
        print("No baseline found. Saving current snapshot as baseline.")
        save_baseline(snap)
        print_report(snap)
        return 0

    baseline = json.loads(BASELINE_FILE.read_text(encoding="utf-8"))
    print_report(snap)

    regressions: list[str] = []
    improvements: list[str] = []

    # Check total test count regression (ratchet)
    prev_total = baseline.get("total_tests", 0)
    curr_total = snap["total_tests"]
    if curr_total < prev_total:
        regressions.append(
            f"Total tests: {prev_total} -> {curr_total} "
            f"(lost {prev_total - curr_total} tests)"
        )
    elif curr_total > prev_total:
        improvements.append(
            f"Total tests: {prev_total} -> {curr_total} "
            f"(gained {curr_total - prev_total} tests)"
        )

    # Check per-category regressions
    prev_cats = baseline.get("categories", {})
    for cat_name, cat in snap["categories"].items():
        prev = prev_cats.get(cat_name, {})
        prev_count = prev.get("test_count", 0)
        curr_count = cat["test_count"]
        if curr_count < prev_count:
            regressions.append(
                f"{cat_name}: test count dropped {prev_count} -> {curr_count}"
            )
        elif curr_count > prev_count:
            improvements.append(
                f"{cat_name}: test count improved {prev_count} -> {curr_count}"
            )

    print("\n--- COVERAGE DELTA (vs baseline) ---")
    if improvements:
        for imp in improvements:
            print(f"  + {imp}")
    if regressions:
        print("\n!! REGRESSIONS:")
        for reg in regressions:
            print(f"::error::Coverage regression: {reg}")
            print(f"  !! {reg}")
        return 1

    if improvements:
        save_baseline(snap)
        print("Baseline auto-updated with improvements.")

    print("\n+ No regressions detected.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Test coverage report")
    parser.add_argument("--json", action="store_true", help="Output JSON")
    parser.add_argument("--ci", action="store_true", help="CI mode: compare vs baseline")
    parser.add_argument(
        "--save-baseline", action="store_true", help="Save current as baseline"
    )
    args = parser.parse_args()

    snap = gather_snapshot()

    if args.save_baseline:
        save_baseline(snap)
        print_report(snap)
        return 0
    if args.json:
        print(json.dumps(snap, indent=2, default=str))
        return 0
    if args.ci:
        return ci_compare(snap)

    print_report(snap)
    return 0


if __name__ == "__main__":
    sys.exit(main())
