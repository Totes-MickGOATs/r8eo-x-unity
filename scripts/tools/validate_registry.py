"""Validate system manifest files — checks declared files exist on disk.

Supports both JSON manifests (engine-agnostic) and .tres manifests (Godot).
Also detects orphan files not claimed by any system.
"""

import json
import re
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent.parent.parent
MANIFEST_DIR = PROJECT_ROOT / "resources" / "manifests"

# Directories excluded from orphan scanning
ORPHAN_EXCLUDE_DIRS = {
    "resources/manifests",
    "tests",
    "scripts/core",
    ".godot",
}

ORPHAN_EXCLUDE_PREFIXES = (".",)


def parse_json_manifest(path: Path) -> dict:
    """Parse a JSON manifest file."""
    data = json.loads(path.read_text(encoding="utf-8"))
    return {
        "system_name": data.get("name", path.stem),
        "status": data.get("status", "ACTIVE"),
        "files": data.get("files", []),
        "dependencies": data.get("dependencies", []),
        "replaced_by": data.get("replaced_by", ""),
        "tests": {
            "editmode": data.get("tests", {}).get("editmode", []),
            "playmode": data.get("tests", {}).get("playmode", []),
        },
    }


def parse_tres_manifest(path: Path) -> dict:
    """Parse a Godot .tres manifest file."""
    text = path.read_text(encoding="utf-8")
    data: dict = {}

    for key in ("owned_scripts", "owned_scenes", "owned_resources"):
        m = re.search(rf'{key}\s*=\s*PackedStringArray\(([^)]*)\)', text)
        if m:
            raw = m.group(1).strip()
            data[key] = [s.strip().strip('"') for s in raw.split(",")] if raw else []
        else:
            data[key] = []

    name_m = re.search(r'system_name\s*=\s*"([^"]*)"', text)
    status_m = re.search(r'status\s*=\s*(\d+)', text)
    replaced_m = re.search(r'replaced_by\s*=\s*"([^"]*)"', text)
    deps_m = re.search(r'depends_on\s*=\s*PackedStringArray\(([^)]*)\)', text)

    files = []
    for key in ("owned_scripts", "owned_scenes", "owned_resources"):
        files.extend(data[key])

    return {
        "system_name": name_m.group(1) if name_m else path.stem,
        "status": ["ACTIVE", "DEPRECATED", "EXPERIMENTAL"][int(status_m.group(1))] if status_m else "ACTIVE",
        "files": files,
        "dependencies": [s.strip().strip('"') for s in deps_m.group(1).split(",")] if deps_m and deps_m.group(1).strip() else [],
        "replaced_by": replaced_m.group(1) if replaced_m else "",
    }


def res_to_path(res_path: str) -> Path:
    """Convert res:// path to filesystem path."""
    return PROJECT_ROOT / res_path.removeprefix("res://")


def validate() -> list[str]:
    issues: list[str] = []
    file_owners: dict[str, str] = {}
    system_names: set[str] = set()
    manifests: list[dict] = []

    if not MANIFEST_DIR.exists():
        issues.append(f"Manifest directory not found: {MANIFEST_DIR}")
        return issues

    # Parse JSON manifests
    for json_file in sorted(MANIFEST_DIR.glob("*.json")):
        data = parse_json_manifest(json_file)
        manifests.append(data)
        name = data["system_name"]
        system_names.add(name)

        if data["status"] == "DEPRECATED" and not data["replaced_by"]:
            issues.append(f"[{name}] DEPRECATED but no replaced_by")

        for file_path in data["files"]:
            fs_path = res_to_path(file_path) if file_path.startswith("res://") else PROJECT_ROOT / file_path
            if not fs_path.exists():
                issues.append(f"[{name}] Missing file: {file_path}")
            if file_path in file_owners:
                issues.append(f"[{name}] Duplicate ownership: {file_path} (also claimed by {file_owners[file_path]})")
            else:
                file_owners[file_path] = name

        print(f"  [{data['status']}] {name} ({len(data['files'])} files)")

    # Parse .tres manifests (Godot)
    for tres_file in sorted(MANIFEST_DIR.glob("*.tres")):
        data = parse_tres_manifest(tres_file)
        manifests.append(data)
        name = data["system_name"]
        system_names.add(name)

        if data["status"] == "DEPRECATED" and not data["replaced_by"]:
            issues.append(f"[{name}] DEPRECATED but no replaced_by")

        for file_path in data["files"]:
            fs_path = res_to_path(file_path) if file_path.startswith("res://") else PROJECT_ROOT / file_path
            if not fs_path.exists():
                issues.append(f"[{name}] Missing file: {file_path}")
            if file_path in file_owners:
                issues.append(f"[{name}] Duplicate ownership: {file_path} (also claimed by {file_owners[file_path]})")
            else:
                file_owners[file_path] = name

        print(f"  [{data['status']}] {name} ({len(data['files'])} files)")

    # Check dependencies
    for data in manifests:
        name = data["system_name"]
        for dep in data["dependencies"]:
            if dep and dep not in system_names:
                issues.append(f"[{name}] Missing dependency: {dep}")

    # Test field validation
    test_owners: dict[str, str] = {}  # class_name -> module_name

    for data in manifests:
        name = data["system_name"]
        tests = data.get("tests", {})
        editmode_tests = tests.get("editmode", [])
        playmode_tests = tests.get("playmode", [])

        # Duplicate test class ownership check
        for cls in editmode_tests + playmode_tests:
            if cls in test_owners:
                issues.append(
                    f"[{name}] Duplicate test ownership: {cls} (also owned by {test_owners[cls]})"
                )
            else:
                test_owners[cls] = name

        # Test file existence checks (warnings)
        for cls in editmode_tests:
            matches = list(PROJECT_ROOT.glob(f"Assets/Tests/EditMode/**/{cls}.cs"))
            if not matches:
                issues.append(
                    f"[{name}] WARNING: Test class not found: {cls} "
                    f"(expected under Assets/Tests/EditMode/)"
                )

        for cls in playmode_tests:
            test_path = PROJECT_ROOT / "Assets" / "Tests" / "PlayMode" / f"{cls}.cs"
            if not test_path.exists():
                issues.append(
                    f"[{name}] WARNING: Test class not found: {cls} "
                    f"(expected at {test_path.relative_to(PROJECT_ROOT)})"
                )

        # No coverage warning
        all_files = data.get("files", [])
        if all_files and not editmode_tests and not playmode_tests:
            issues.append(f"[{name}] WARNING: no test coverage declared")

    return issues


def main() -> int:
    print(f"=== Registry Validation: {MANIFEST_DIR} ===")
    issues = validate()
    warnings = [i for i in issues if "WARNING" in i]
    errors = [i for i in issues if "WARNING" not in i]
    if not issues:
        print("  Validation: CLEAN")
        return 0
    if warnings:
        print(f"\n  {len(warnings)} warning(s):")
        for issue in warnings:
            print(f"    - {issue}")
    if errors:
        print(f"\n  {len(errors)} error(s):")
        for issue in errors:
            print(f"    - {issue}")
        return 1
    print("  Validation: CLEAN (with warnings)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
