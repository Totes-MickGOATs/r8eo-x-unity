#!/usr/bin/env python3
"""Assert audit — verify every test method body contains at least one assertion.

Reads C# file paths from stdin (one per line) or scans Assets/Tests/ with --all.

Output: exit 0 if all methods have assertions, exit 1 with diagnostics if any are missing.
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

PROJECT = Path(__file__).resolve().parent.parent.parent
TESTS_DIR = PROJECT / "Assets" / "Tests"

# Attributes that mark a method as a test
TEST_ATTR_RE = re.compile(r"^\s*\[(Test|UnityTest|TestCase\s*\(.*?\))\]\s*$")

# Assertion prefixes we accept as valid assertions
ASSERTION_PREFIXES = (
    "Assert.",
    "Assume.",
    "StringAssert.",
    "CollectionAssert.",
    "LogAssert.",
)

# For UnityTest IEnumerators: yield return + method call counts as async assertion
YIELD_ASSERT_RE = re.compile(r"yield\s+return\s+\w+")


def _find_test_methods(lines: list[str]) -> list[tuple[int, str]]:
    """Find all test method declarations, returning (line_index, method_name) pairs.

    Args:
        lines: All lines of the C# file.

    Returns:
        List of (line_index_of_method_signature, method_name) tuples.
    """
    results: list[tuple[int, str]] = []
    pending_attr = False
    is_unity_test = False

    for i, line in enumerate(lines):
        stripped = line.strip()

        # Check for test attributes
        if re.match(r"^\[(Test|UnityTest|TestCase)", stripped):
            pending_attr = True
            is_unity_test = "UnityTest" in stripped
            continue

        if pending_attr:
            # Skip blank lines and additional attributes between [Test] and method sig
            if stripped == "" or stripped.startswith("["):
                if stripped.startswith("[") and "UnityTest" in stripped:
                    is_unity_test = True
                continue

            # This should be the method signature
            # Match: <modifiers> <return_type> <MethodName>(...)
            method_match = re.search(r"\b(\w+)\s*\(", stripped)
            if method_match:
                method_name = method_match.group(1)
                # Exclude common non-method patterns
                if method_name not in ("if", "for", "while", "switch", "foreach", "using"):
                    results.append((i, method_name, is_unity_test))  # type: ignore[arg-type]
            pending_attr = False
            is_unity_test = False

    return results  # type: ignore[return-value]


def _extract_method_body(lines: list[str], method_start: int) -> list[str]:
    """Extract the body lines of a method starting at method_start.

    Args:
        lines: All lines of the file.
        method_start: Index of the method signature line.

    Returns:
        Lines inside the method body (between the outermost braces).
    """
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
    """Return True if the body contains at least one assertion.

    Args:
        body: Lines inside the method body.
        is_unity_test: Whether this is a [UnityTest] IEnumerator method.

    Returns:
        True if an assertion was found.
    """
    for line in body:
        stripped = line.strip()
        for prefix in ASSERTION_PREFIXES:
            if prefix in stripped:
                return True
        # UnityTest async assertion pattern: yield return + method call
        if is_unity_test and YIELD_ASSERT_RE.search(stripped):
            return True
    return False


def audit_file(path: Path) -> list[str]:
    """Audit a single C# file for test methods missing assertions.

    Args:
        path: Path to the C# file.

    Returns:
        List of violation strings in format 'file:line:MethodName — missing assertion'.
    """
    violations: list[str] = []
    try:
        text = path.read_text(encoding="utf-8")
    except OSError as e:
        return [f"{path}: could not read file: {e}"]

    lines = text.splitlines()
    methods = _find_test_methods(lines)

    for entry in methods:
        line_idx, method_name, is_unity_test = entry  # type: ignore[misc]
        body = _extract_method_body(lines, line_idx)
        if not _body_has_assertion(body, is_unity_test):
            # line_idx is 0-based; report 1-based line number
            violations.append(f"{path}:{line_idx + 1}:{method_name} — missing assertion")

    return violations


def main() -> int:
    """Entry point for the assert audit tool.

    Returns:
        0 if all test methods have assertions, 1 if any are missing.
    """
    parser = argparse.ArgumentParser(description="Verify every [Test]/[UnityTest] method has at least one assertion.")
    parser.add_argument(
        "--all",
        action="store_true",
        help=f"Scan all test files under {TESTS_DIR}",
    )
    args = parser.parse_args()

    if args.all:
        file_paths = list(TESTS_DIR.rglob("*.cs"))
    else:
        raw = sys.stdin.read().strip()
        if not raw:
            # Nothing to check — exit cleanly
            return 0
        file_paths = [Path(p.strip()) for p in raw.splitlines() if p.strip()]

    all_violations: list[str] = []
    for file_path in file_paths:
        all_violations.extend(audit_file(file_path))

    if all_violations:
        for v in all_violations:
            print(v, file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
