#!/usr/bin/env python3
"""Resolve module test filters from changed file paths.

Given a list of changed file paths (stdin), determines which modules are
affected (including transitive dependents), and outputs the union of all
test class names to run.

Usage:
    git diff --name-only main..HEAD | uv run python scripts/tools/resolve_module_tests.py --format shell
    git diff --name-only main..HEAD | uv run python scripts/tools/resolve_module_tests.py --format json
    git diff --name-only main..HEAD | uv run python scripts/tools/resolve_module_tests.py --no-transitive
    git diff --name-only main..HEAD | uv run python scripts/tools/resolve_module_tests.py --editmode-only

Exit codes:
    0 — tests found
    2 — no matched modules (no tests needed)
    1 — error
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent.parent.parent
MANIFEST_DIR = PROJECT_ROOT / "resources" / "manifests"


def load_manifests() -> list[dict]:
    """Load all JSON manifests from the manifest directory."""
    manifests = []
    for json_file in sorted(MANIFEST_DIR.glob("*.json")):
        data = json.loads(json_file.read_text(encoding="utf-8"))
        manifests.append(data)
    return manifests


def _manifest_name(manifest: dict) -> str:
    """Return the canonical name for a manifest, supporting both 'name' and 'system' keys."""
    return manifest.get("name") or manifest.get("system") or ""


def build_reverse_deps(manifests: list[dict]) -> dict[str, set[str]]:
    """Build reverse dependency graph: module -> set of modules that depend on it.

    For every manifest with dependencies [a, b, c], we add the manifest's
    own name to rdeps[a], rdeps[b], rdeps[c].
    """
    # Initialise every module with an empty set
    rdeps: dict[str, set[str]] = {_manifest_name(m): set() for m in manifests}

    for manifest in manifests:
        owner = _manifest_name(manifest)
        for dep in manifest.get("dependencies", []):
            if dep in rdeps:
                rdeps[dep].add(owner)
            else:
                # dep not in manifests (shouldn't happen with a valid registry)
                rdeps[dep] = {owner}

    return rdeps


def find_module_for_file(file_path: str, manifests: list[dict]) -> str | None:
    """Return the module name that owns this file path, or None."""
    for manifest in manifests:
        if file_path in manifest.get("files", []):
            return _manifest_name(manifest)
    return None


def expand_with_dependents(modules: set[str], reverse_deps: dict[str, set[str]]) -> set[str]:
    """Transitively expand modules to include all modules that depend on them (BFS)."""
    visited: set[str] = set(modules)
    queue: list[str] = list(modules)

    while queue:
        current = queue.pop(0)
        for dependent in reverse_deps.get(current, set()):
            if dependent not in visited:
                visited.add(dependent)
                queue.append(dependent)

    return visited


def expand_with_dependencies(modules: set[str], manifests: list[dict]) -> set[str]:
    """Add direct dependencies of each module to the expansion set.

    Used so that changing a 'leaf' module (like editor) also runs
    the tests of the systems it depends on.
    """
    result = set(modules)
    # Build forward dep map: module -> set of direct dependencies
    dep_map: dict[str, set[str]] = {}
    for m in manifests:
        name = _manifest_name(m)
        dep_map[name] = set(m.get("dependencies", []))

    for mod in modules:
        result.update(dep_map.get(mod, set()))

    return result


def collect_tests(
    modules: set[str],
    manifests: list[dict],
) -> tuple[list[str], list[str]]:
    """Collect editmode and playmode test class names for the given module set.

    Returns deduplicated lists preserving first-seen order.
    """
    editmode_seen: set[str] = set()
    playmode_seen: set[str] = set()
    editmode: list[str] = []
    playmode: list[str] = []

    for manifest in manifests:
        if _manifest_name(manifest) not in modules:
            continue
        tests = manifest.get("tests", {})
        for cls in tests.get("editmode", []):
            if cls not in editmode_seen:
                editmode_seen.add(cls)
                editmode.append(cls)
        for cls in tests.get("playmode", []):
            if cls not in playmode_seen:
                playmode_seen.add(cls)
                playmode.append(cls)

    return editmode, playmode


def resolve(
    changed_files: list[str],
    manifests: list[dict],
    *,
    transitive: bool = True,
    editmode_only: bool = False,
    with_dependencies: bool = False,
) -> tuple[set[str], list[str], list[str]]:
    """Core resolution pipeline.

    Args:
        changed_files: List of changed file paths.
        manifests: List of loaded manifest dicts.
        transitive: Whether to expand to dependent modules.
        editmode_only: If True, return empty playmode list.
        with_dependencies: If True, also include direct dependencies of matched
            modules in the expansion. Enables leaf modules (e.g. editor) with
            empty test lists to pull in tests from the modules they depend on.

    Returns:
        (modules, editmode_tests, playmode_tests)
    """
    rdeps = build_reverse_deps(manifests)

    # Map each changed file to its owning module
    direct_modules: set[str] = set()
    for file_path in changed_files:
        module = find_module_for_file(file_path.strip(), manifests)
        if module is not None:
            direct_modules.add(module)

    if not direct_modules:
        return set(), [], []

    # Expand transitively if requested
    modules = expand_with_dependents(direct_modules, rdeps) if transitive else set(direct_modules)

    # Also expand with direct dependencies of matched modules if requested
    if with_dependencies:
        modules = expand_with_dependencies(modules, manifests)

    # Collect tests
    editmode, playmode = collect_tests(modules, manifests)

    if editmode_only:
        playmode = []

    return modules, editmode, playmode


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Resolve module test filters from changed file paths (stdin)."
    )
    parser.add_argument(
        "--format",
        choices=["shell", "json"],
        default="shell",
        help="Output format (default: shell)",
    )
    parser.add_argument(
        "--no-transitive",
        action="store_true",
        help="Skip transitive dependent expansion",
    )
    parser.add_argument(
        "--editmode-only",
        action="store_true",
        help="Only output EditMode filter (suppress playmode)",
    )
    parser.add_argument(
        "--with-dependencies",
        action="store_true",
        help=(
            "Also include direct dependencies of matched modules in the expansion. "
            "Enables leaf modules (e.g. editor) with no tests of their own to pull "
            "in tests from the systems they depend on."
        ),
    )
    args = parser.parse_args()

    try:
        changed_files = [line.rstrip("\n") for line in sys.stdin if line.strip()]
    except Exception as exc:
        print(f"ERROR reading stdin: {exc}", file=sys.stderr)
        return 1

    try:
        manifests = load_manifests()
    except Exception as exc:
        print(f"ERROR loading manifests: {exc}", file=sys.stderr)
        return 1

    modules, editmode, playmode = resolve(
        changed_files,
        manifests,
        transitive=not args.no_transitive,
        editmode_only=args.editmode_only,
        with_dependencies=args.with_dependencies,
    )

    if not modules:
        # No matched modules — no tests needed
        if args.format == "json":
            print(json.dumps({"modules": [], "editmode": [], "playmode": []}))
        else:
            print("MODULES=")
            print("EDITMODE_FILTER=")
            print("PLAYMODE_FILTER=")
        return 2

    if args.format == "json":
        print(
            json.dumps(
                {
                    "modules": sorted(modules),
                    "editmode": editmode,
                    "playmode": playmode,
                },
                indent=2,
            )
        )
    else:
        modules_str = ",".join(sorted(modules))
        editmode_str = "|".join(editmode)
        playmode_str = "|".join(playmode)
        print(f"MODULES={modules_str}")
        print(f"EDITMODE_FILTER={editmode_str}")
        print(f"PLAYMODE_FILTER={playmode_str}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
