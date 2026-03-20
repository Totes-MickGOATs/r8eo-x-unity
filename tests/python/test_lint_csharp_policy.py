"""Tests for lint_csharp_policy.py — C# policy linter.

All tests use tmp_path / in-memory fixtures — no real project files are read.
"""

from __future__ import annotations

import json
import sys
import textwrap
from pathlib import Path
from unittest.mock import patch

# ---------------------------------------------------------------------------
# Import the module under test
# ---------------------------------------------------------------------------
SCRIPTS_TOOLS = Path(__file__).resolve().parent.parent.parent / "scripts" / "tools"
sys.path.insert(0, str(SCRIPTS_TOOLS))

import lint_csharp_policy as lcp  # noqa: E402

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_cs(tmp_path: Path, rel_path: str, code: str) -> Path:
    """Write code to a .cs file at tmp_path / rel_path, creating parents."""
    abs_path = tmp_path / rel_path
    abs_path.parent.mkdir(parents=True, exist_ok=True)
    abs_path.write_text(textwrap.dedent(code), encoding="utf-8")
    return abs_path


def _make_manifest_index(manifests: list[dict]) -> dict[str, dict]:
    return lcp._build_manifest_index(manifests)


def _empty_index() -> dict[str, dict]:
    return {}


# ---------------------------------------------------------------------------
# 1. Debug.Log rule fires in runtime assembly
# ---------------------------------------------------------------------------

def test_debug_log_fires_in_runtime(tmp_path: Path) -> None:
    rel = "Assets/Scripts/Vehicle/TestScript.cs"
    _make_cs(tmp_path, rel, 'class T { void M() { Debug.Log("x"); } }')
    violations = lcp.lint_file(rel, tmp_path / rel, _empty_index())
    debug_v = [v for v in violations if v.rule == "DEBUG_LOG"]
    assert len(debug_v) >= 1, "Expected DEBUG_LOG violation in runtime assembly"
    assert debug_v[0].file == rel


# ---------------------------------------------------------------------------
# 2. Debug.Log exempted in Editor/
# ---------------------------------------------------------------------------

def test_debug_log_exempt_editor(tmp_path: Path) -> None:
    rel = "Assets/Scripts/Editor/Foo.cs"
    _make_cs(tmp_path, rel, 'class F { void M() { Debug.Log("x"); } }')
    violations = lcp.lint_file(rel, tmp_path / rel, _empty_index())
    assert not any(v.rule == "DEBUG_LOG" for v in violations)


# ---------------------------------------------------------------------------
# 3. Debug.Log exempted in Debug/
# ---------------------------------------------------------------------------

def test_debug_log_exempt_debug_folder(tmp_path: Path) -> None:
    rel = "Assets/Scripts/Debug/Bar.cs"
    _make_cs(tmp_path, rel, 'class B { void M() { Debug.Log("x"); } }')
    violations = lcp.lint_file(rel, tmp_path / rel, _empty_index())
    assert not any(v.rule == "DEBUG_LOG" for v in violations)


# ---------------------------------------------------------------------------
# 4. Debug.Log exempted in Tests/
# ---------------------------------------------------------------------------

def test_debug_log_exempt_tests(tmp_path: Path) -> None:
    rel = "Assets/Tests/EditMode/Test.cs"
    _make_cs(tmp_path, rel, 'class T { void M() { Debug.Log("x"); } }')
    violations = lcp.lint_file(rel, tmp_path / rel, _empty_index())
    assert not any(v.rule == "DEBUG_LOG" for v in violations)


# ---------------------------------------------------------------------------
# 5. FindObjectOfType rule fires in runtime assembly
# ---------------------------------------------------------------------------

def test_find_object_fires_in_runtime(tmp_path: Path) -> None:
    rel = "Assets/Scripts/Core/Manager.cs"
    _make_cs(tmp_path, rel, "class M { void F() { var x = FindObjectOfType<Foo>(); } }")
    violations = lcp.lint_file(rel, tmp_path / rel, _empty_index())
    fobj = [v for v in violations if v.rule == "FIND_OBJECT"]
    assert len(fobj) >= 1, "Expected FIND_OBJECT violation"


# ---------------------------------------------------------------------------
# 6. Resources.Load rule fires in runtime assembly
# ---------------------------------------------------------------------------

def test_resources_load_fires_in_runtime(tmp_path: Path) -> None:
    rel = "Assets/Scripts/UI/Loader.cs"
    _make_cs(tmp_path, rel, 'class L { void F() { Resources.Load("foo"); } }')
    violations = lcp.lint_file(rel, tmp_path / rel, _empty_index())
    fobj = [v for v in violations if v.rule == "FIND_OBJECT"]
    assert len(fobj) >= 1, "Expected FIND_OBJECT violation for Resources.Load"


# ---------------------------------------------------------------------------
# 7. Allowlisted file (all) returns no violations
# ---------------------------------------------------------------------------

def test_allowlisted_file_skipped(tmp_path: Path) -> None:
    # RCCar.cs is in POLICY_ALLOWLIST with "all"
    rel = "Assets/Scripts/Vehicle/RCCar.cs"
    _make_cs(
        tmp_path, rel,
        'class R { void M() { Debug.Log("x"); var y = FindObjectOfType<Foo>(); } }',
    )
    violations = lcp.lint_file(rel, tmp_path / rel, _empty_index())
    blocking = [v for v in violations if v.rule not in ("NO_TESTS", "ORPHAN_FILE")]
    assert blocking == [], f"Expected no blocking violations for allowlisted file, got {blocking}"


# ---------------------------------------------------------------------------
# 8. Changed-file --staged mode returns correct file list (mock git)
# ---------------------------------------------------------------------------

def test_get_staged_files(tmp_path: Path) -> None:
    fake_output = "Assets/Scripts/Vehicle/RCCar.cs\nAssets/Scripts/Core/Foo.cs\n"
    with patch("subprocess.run") as mock_run:
        mock_run.return_value.stdout = fake_output
        mock_run.return_value.returncode = 0
        result = lcp.get_staged_files(tmp_path)
    assert result == [
        "Assets/Scripts/Vehicle/RCCar.cs",
        "Assets/Scripts/Core/Foo.cs",
    ]


# ---------------------------------------------------------------------------
# 9. Changed-file --all mode returns correct file list (mock git)
# ---------------------------------------------------------------------------

def test_get_all_files(tmp_path: Path) -> None:
    fake_output = "Assets/Scripts/Vehicle/RCCar.cs\nAssets/Scripts/Track/SurfaceZone.cs\n"
    with patch("subprocess.run") as mock_run:
        mock_run.return_value.stdout = fake_output
        mock_run.return_value.returncode = 0
        result = lcp.get_all_files(tmp_path)
    assert result == [
        "Assets/Scripts/Vehicle/RCCar.cs",
        "Assets/Scripts/Track/SurfaceZone.cs",
    ]


# ---------------------------------------------------------------------------
# 10. Manifest orphan rule fires for unregistered runtime .cs file
# ---------------------------------------------------------------------------

def test_orphan_file_fires(tmp_path: Path) -> None:
    rel = "Assets/Scripts/Vehicle/NewUnregistered.cs"
    _make_cs(tmp_path, rel, "class N {}")
    # Empty manifest index — file not owned by anything
    violations = lcp.check_orphan(rel, {})
    assert len(violations) == 1
    assert violations[0].rule == "ORPHAN_FILE"


# ---------------------------------------------------------------------------
# 11. Manifest orphan allowlisted — R8EOXInputActions.cs not flagged
# ---------------------------------------------------------------------------

def test_orphan_allowlisted_input_actions(tmp_path: Path) -> None:
    rel = "Assets/Scripts/Input/R8EOXInputActions.cs"
    violations = lcp.check_orphan(rel, {})
    assert violations == [], "R8EOXInputActions.cs should be allowlisted as orphan"


# ---------------------------------------------------------------------------
# 12. GUID in asmdef rule fires
# ---------------------------------------------------------------------------

def test_guid_in_asmdef_fires(tmp_path: Path) -> None:
    rel = "Assets/Scripts/Vehicle/R8EOX.Vehicle.asmdef"
    content = json.dumps({
        "name": "R8EOX.Vehicle",
        "references": ["GUID:abc123def456abc123def456abc12345"],
    })
    abs_path = tmp_path / rel
    abs_path.parent.mkdir(parents=True, exist_ok=True)
    abs_path.write_text(content, encoding="utf-8")
    violations = lcp.check_asmdef(rel, content)
    assert len(violations) >= 1
    assert violations[0].rule == "GUID_ASMDEF"


# ---------------------------------------------------------------------------
# 13. Clean asmdef (assembly name refs) — no violations
# ---------------------------------------------------------------------------

def test_clean_asmdef_no_violations() -> None:
    rel = "Assets/Scripts/Vehicle/R8EOX.Vehicle.asmdef"
    content = json.dumps({
        "name": "R8EOX.Vehicle",
        "references": ["R8EOX.Input", "R8EOX.Core"],
    })
    violations = lcp.check_asmdef(rel, content)
    assert violations == []


# ---------------------------------------------------------------------------
# 14. STRING_LOOKUP rule fires in runtime assembly
# ---------------------------------------------------------------------------

def test_string_lookup_fires_in_runtime(tmp_path: Path) -> None:
    rel = "Assets/Scripts/GameFlow/Boot.cs"
    _make_cs(
        tmp_path, rel,
        'class B { void L() { SceneManager.LoadScene("MainScene"); } }',
    )
    violations = lcp.lint_file(rel, tmp_path / rel, _empty_index())
    sl = [v for v in violations if v.rule == "STRING_LOOKUP"]
    assert len(sl) >= 1, "Expected STRING_LOOKUP violation"


# ---------------------------------------------------------------------------
# 15. NO_TESTS advisory when manifest has zero editmode tests
# ---------------------------------------------------------------------------

def test_no_tests_advisory_when_empty_editmode(tmp_path: Path) -> None:
    rel = "Assets/Scripts/Camera/CameraRig.cs"
    _make_cs(tmp_path, rel, "class C {}")
    manifest = {
        "name": "camera",
        "files": [rel],
        "tests": {"editmode": [], "playmode": []},
    }
    index = _make_manifest_index([manifest])
    violations = lcp.check_no_tests(rel, index)
    assert len(violations) == 1
    assert violations[0].rule == "NO_TESTS"


# ---------------------------------------------------------------------------
# 16. NO_TESTS not fired when manifest has editmode tests
# ---------------------------------------------------------------------------

def test_no_tests_not_fired_when_tests_present(tmp_path: Path) -> None:
    rel = "Assets/Scripts/Vehicle/RCCar.cs"
    _make_cs(tmp_path, rel, "class R {}")
    manifest = {
        "name": "vehicle",
        "files": [rel],
        "tests": {"editmode": ["RCCarTests"], "playmode": []},
    }
    index = _make_manifest_index([manifest])
    violations = lcp.check_no_tests(rel, index)
    assert violations == []


# ---------------------------------------------------------------------------
# 17. filter_lintable keeps only .cs and .asmdef
# ---------------------------------------------------------------------------

def test_filter_lintable() -> None:
    files = [
        "Assets/Scripts/Vehicle/RCCar.cs",
        "Assets/Scripts/Vehicle/R8EOX.Vehicle.asmdef",
        "Assets/Scripts/Vehicle/R8EOX.Vehicle.asmdef.meta",
        "README.md",
        "resources/manifests/vehicle.json",
    ]
    result = lcp.filter_lintable(files)
    assert result == [
        "Assets/Scripts/Vehicle/RCCar.cs",
        "Assets/Scripts/Vehicle/R8EOX.Vehicle.asmdef",
    ]


# ---------------------------------------------------------------------------
# 18. run_lint returns blocking and advisory separately
# ---------------------------------------------------------------------------

def test_run_lint_separates_blocking_from_advisory(tmp_path: Path) -> None:
    # Create a manifest dir so _load_manifests works
    manifest_dir = tmp_path / "resources" / "manifests"
    manifest_dir.mkdir(parents=True)
    camera_manifest = {
        "name": "camera",
        "files": ["Assets/Scripts/Camera/CameraRig.cs"],
        "tests": {"editmode": [], "playmode": []},
    }
    (manifest_dir / "camera.json").write_text(
        json.dumps(camera_manifest), encoding="utf-8"
    )

    # File with debug log (blocking) in camera assembly with no tests (advisory)
    rel = "Assets/Scripts/Camera/CameraRig.cs"
    (tmp_path / rel).parent.mkdir(parents=True, exist_ok=True)
    (tmp_path / rel).write_text('class C { void M() { Debug.Log("x"); } }', encoding="utf-8")

    blocking, advisory = lcp.run_lint([rel], tmp_path)
    assert any(v.rule == "DEBUG_LOG" for v in blocking), "Expected DEBUG_LOG in blocking"
    assert any(v.rule == "NO_TESTS" for v in advisory), "Expected NO_TESTS in advisory"
