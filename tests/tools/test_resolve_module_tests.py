"""Tests for resolve_module_tests.py — module-based test resolver.

These tests use in-memory fixture manifests (not the real manifest files).
"""

import sys
from pathlib import Path

# Add the scripts/tools directory to the path so we can import the module
SCRIPTS_TOOLS = Path(__file__).resolve().parent.parent.parent / "scripts" / "tools"
sys.path.insert(0, str(SCRIPTS_TOOLS))

import resolve_module_tests as rmt  # noqa: E402  (must come after sys.path insert)

# ---------------------------------------------------------------------------
# Fixture manifests (minimal, in-memory)
# ---------------------------------------------------------------------------

FIXTURE_MANIFESTS = [
    {
        "name": "core",
        "files": ["Assets/Scripts/Core/SurfaceType.cs"],
        "dependencies": [],
        "tests": {"editmode": [], "playmode": []},
    },
    {
        "name": "input",
        "files": ["Assets/Scripts/Input/InputMath.cs"],
        "dependencies": [],
        "tests": {
            "editmode": ["InputMathTests", "InputProcessingTests", "ZeroInputTests"],
            "playmode": [],
        },
    },
    {
        "name": "camera",
        "files": ["Assets/Scripts/Camera/CameraController.cs"],
        "dependencies": [],
        "tests": {"editmode": [], "playmode": []},
    },
    {
        "name": "track",
        "files": ["Assets/Scripts/Track/SurfaceZone.cs"],
        "dependencies": ["core"],
        "tests": {"editmode": [], "playmode": []},
    },
    {
        "name": "vehicle",
        "files": ["Assets/Scripts/Vehicle/RCCar.cs"],
        "dependencies": ["input", "core"],
        "tests": {
            "editmode": ["SuspensionMathTests", "DrivetrainMathTests"],
            "playmode": ["VehicleIntegrationTests"],
        },
    },
    {
        "name": "debug",
        "files": ["Assets/Scripts/Debug/ContractDebugger.cs"],
        "dependencies": ["vehicle", "input"],
        "tests": {
            "editmode": ["ContractDebuggerTests"],
            "playmode": [],
        },
    },
    {
        "name": "gameflow",
        "files": ["Assets/Scripts/GameFlow/GameFlowManager.cs"],
        "dependencies": ["core"],
        "tests": {
            "editmode": ["GameFlowStateMachineTests"],
            "playmode": [],
        },
    },
    {
        "name": "ui",
        "files": ["Assets/Scripts/UI/UIManager.cs"],
        "dependencies": ["core", "gameflow"],
        "tests": {
            "editmode": ["UIManagerTests"],
            "playmode": [],
        },
    },
    {
        "name": "editor",
        "files": ["Assets/Scripts/Editor/SceneSetup.cs"],
        "dependencies": ["vehicle", "input", "camera", "debug"],
        "tests": {"editmode": [], "playmode": []},
    },
]


# ---------------------------------------------------------------------------
# Helper: build reverse deps from fixture manifests
# ---------------------------------------------------------------------------

def _reverse_deps() -> dict[str, set[str]]:
    return rmt.build_reverse_deps(FIXTURE_MANIFESTS)


# ---------------------------------------------------------------------------
# Tests: build_reverse_deps
# ---------------------------------------------------------------------------

class TestBuildReverseDeps:
    def test_core_has_expected_dependents(self):
        rdeps = _reverse_deps()
        assert rdeps["core"] == {"vehicle", "track", "gameflow", "ui"}

    def test_input_has_expected_dependents(self):
        rdeps = _reverse_deps()
        assert rdeps["input"] == {"vehicle", "debug", "editor"}

    def test_leaf_module_has_empty_reverse_deps(self):
        rdeps = _reverse_deps()
        # ui has no dependents
        assert rdeps["ui"] == set()

    def test_camera_has_editor_as_dependent(self):
        rdeps = _reverse_deps()
        assert rdeps["camera"] == {"editor"}


# ---------------------------------------------------------------------------
# Tests: find_module_for_file
# ---------------------------------------------------------------------------

class TestFindModuleForFile:
    def test_finds_input_module(self):
        result = rmt.find_module_for_file("Assets/Scripts/Input/InputMath.cs", FIXTURE_MANIFESTS)
        assert result == "input"

    def test_finds_vehicle_module(self):
        result = rmt.find_module_for_file("Assets/Scripts/Vehicle/RCCar.cs", FIXTURE_MANIFESTS)
        assert result == "vehicle"

    def test_unknown_file_returns_none(self):
        result = rmt.find_module_for_file("Assets/Scripts/Unknown/Foo.cs", FIXTURE_MANIFESTS)
        assert result is None

    def test_editor_file_finds_editor_module(self):
        result = rmt.find_module_for_file("Assets/Scripts/Editor/SceneSetup.cs", FIXTURE_MANIFESTS)
        assert result == "editor"


# ---------------------------------------------------------------------------
# Tests: expand_with_dependents
# ---------------------------------------------------------------------------

class TestExpandWithDependents:
    def test_input_expands_to_include_vehicle_debug_editor(self):
        rdeps = _reverse_deps()
        result = rmt.expand_with_dependents({"input"}, rdeps)
        assert result >= {"input", "vehicle", "debug", "editor"}

    def test_vehicle_expands_to_include_debug_editor(self):
        rdeps = _reverse_deps()
        result = rmt.expand_with_dependents({"vehicle"}, rdeps)
        assert result >= {"vehicle", "debug", "editor"}

    def test_core_expands_to_all_dependents(self):
        rdeps = _reverse_deps()
        result = rmt.expand_with_dependents({"core"}, rdeps)
        assert result >= {"core", "vehicle", "track", "gameflow", "ui", "debug", "editor"}

    def test_leaf_module_does_not_expand(self):
        rdeps = _reverse_deps()
        result = rmt.expand_with_dependents({"ui"}, rdeps)
        assert result == {"ui"}

    def test_empty_set_stays_empty(self):
        rdeps = _reverse_deps()
        result = rmt.expand_with_dependents(set(), rdeps)
        assert result == set()


# ---------------------------------------------------------------------------
# Tests: collect_tests
# ---------------------------------------------------------------------------

class TestManifestName:
    def test_name_key_preferred(self):
        assert rmt._manifest_name({"name": "foo", "system": "bar"}) == "foo"

    def test_system_key_fallback(self):
        assert rmt._manifest_name({"system": "bar"}) == "bar"

    def test_empty_manifest_returns_empty_string(self):
        assert rmt._manifest_name({}) == ""


class TestExpandWithDependencies:
    def test_editor_module_expands_to_include_dependencies(self):
        result = rmt.expand_with_dependencies({"editor"}, FIXTURE_MANIFESTS)
        assert result >= {"editor", "vehicle", "input", "camera", "debug"}

    def test_leaf_module_with_no_deps_stays_singleton(self):
        result = rmt.expand_with_dependencies({"input"}, FIXTURE_MANIFESTS)
        assert result == {"input"}

    def test_multiple_modules_union_of_their_deps(self):
        result = rmt.expand_with_dependencies({"editor", "vehicle"}, FIXTURE_MANIFESTS)
        assert "editor" in result
        assert "vehicle" in result
        assert "input" in result  # vehicle dep
        assert "core" in result   # vehicle dep
        assert "camera" in result  # editor dep
        assert "debug" in result   # editor dep

    def test_does_not_transitively_follow_deps(self):
        """Only direct deps are added, not transitive deps-of-deps."""
        # debug depends on [vehicle, input]; vehicle depends on [input, core]
        # expand_with_dependencies(debug) should add vehicle + input but NOT core
        result = rmt.expand_with_dependencies({"debug"}, FIXTURE_MANIFESTS)
        assert "vehicle" in result
        assert "input" in result
        # core is a dep-of-dep, not direct — should NOT be included
        assert "core" not in result

    def test_empty_set_stays_empty(self):
        result = rmt.expand_with_dependencies(set(), FIXTURE_MANIFESTS)
        assert result == set()


class TestCollectTests:
    def test_vehicle_module_includes_suspension_tests(self):
        editmode, playmode = rmt.collect_tests({"vehicle"}, FIXTURE_MANIFESTS)
        assert "SuspensionMathTests" in editmode
        assert "VehicleIntegrationTests" in playmode

    def test_input_module_editmode_only(self):
        editmode, playmode = rmt.collect_tests({"input"}, FIXTURE_MANIFESTS)
        assert "InputMathTests" in editmode
        assert playmode == []

    def test_multiple_modules_union(self):
        editmode, playmode = rmt.collect_tests({"input", "debug"}, FIXTURE_MANIFESTS)
        assert "InputMathTests" in editmode
        assert "ContractDebuggerTests" in editmode

    def test_empty_modules_returns_empty(self):
        editmode, playmode = rmt.collect_tests(set(), FIXTURE_MANIFESTS)
        assert editmode == []
        assert playmode == []

    def test_no_duplicates_in_output(self):
        editmode, playmode = rmt.collect_tests({"vehicle", "input"}, FIXTURE_MANIFESTS)
        assert len(editmode) == len(set(editmode))
        assert len(playmode) == len(set(playmode))


# ---------------------------------------------------------------------------
# Tests: resolve (integration — stdin → output)
# ---------------------------------------------------------------------------

class TestResolve:
    """Integration tests for the full resolve pipeline."""

    def test_input_file_triggers_input_vehicle_debug_editor(self):
        """Changing an input file should resolve to input + all dependents."""
        changed = ["Assets/Scripts/Input/InputMath.cs"]
        modules, editmode, playmode = rmt.resolve(changed, FIXTURE_MANIFESTS, transitive=True)
        assert "input" in modules
        assert "vehicle" in modules
        assert "debug" in modules
        assert "editor" in modules

    def test_vehicle_file_triggers_vehicle_debug_editor(self):
        changed = ["Assets/Scripts/Vehicle/RCCar.cs"]
        modules, editmode, playmode = rmt.resolve(changed, FIXTURE_MANIFESTS, transitive=True)
        assert "vehicle" in modules
        assert "debug" in modules
        assert "editor" in modules

    def test_core_file_triggers_all_dependents(self):
        changed = ["Assets/Scripts/Core/SurfaceType.cs"]
        modules, editmode, playmode = rmt.resolve(changed, FIXTURE_MANIFESTS, transitive=True)
        assert "core" in modules
        assert "vehicle" in modules
        assert "track" in modules
        assert "gameflow" in modules
        assert "ui" in modules
        assert "debug" in modules
        assert "editor" in modules

    def test_unknown_file_returns_empty(self):
        changed = ["Assets/Scripts/Unknown/Foo.cs"]
        modules, editmode, playmode = rmt.resolve(changed, FIXTURE_MANIFESTS, transitive=True)
        assert modules == set()
        assert editmode == []
        assert playmode == []

    def test_no_transitive_flag_limits_to_direct_module(self):
        changed = ["Assets/Scripts/Input/InputMath.cs"]
        modules, editmode, playmode = rmt.resolve(changed, FIXTURE_MANIFESTS, transitive=False)
        assert "input" in modules
        assert "vehicle" not in modules
        assert "debug" not in modules

    def test_editmode_only_excludes_playmode(self):
        changed = ["Assets/Scripts/Vehicle/RCCar.cs"]
        modules, editmode, playmode = rmt.resolve(
            changed, FIXTURE_MANIFESTS, transitive=True, editmode_only=True
        )
        assert playmode == []
        assert len(editmode) > 0

    def test_editor_file_without_with_dependencies_returns_no_tests(self):
        """Editor has empty test list; without --with-dependencies no tests are returned."""
        changed = ["Assets/Scripts/Editor/SceneSetup.cs"]
        modules, editmode, playmode = rmt.resolve(
            changed, FIXTURE_MANIFESTS, transitive=True, with_dependencies=False
        )
        # Editor is matched but has no tests; its reverse-deps (none) add nothing
        assert "editor" in modules
        assert editmode == []
        assert playmode == []

    def test_editor_file_with_with_dependencies_includes_vehicle_tests(self):
        """With --with-dependencies, editor changes pull in vehicle + debug + input tests."""
        changed = ["Assets/Scripts/Editor/SceneSetup.cs"]
        modules, editmode, playmode = rmt.resolve(
            changed, FIXTURE_MANIFESTS, transitive=True, with_dependencies=True
        )
        assert "editor" in modules
        assert "vehicle" in modules
        assert "debug" in modules
        assert "input" in modules
        assert "SuspensionMathTests" in editmode
        assert "ContractDebuggerTests" in editmode
        assert "InputMathTests" in editmode
        assert "VehicleIntegrationTests" in playmode

    def test_with_dependencies_does_not_duplicate_tests(self):
        """When vehicle is already in the expansion, --with-dependencies should not duplicate its tests."""
        # vehicle depends on input and core; debug depends on vehicle and input
        # Changing a debug file expands to include editor (reverse dep of debug)
        # with_dependencies on editor adds vehicle+input+camera+debug (already present)
        changed = ["Assets/Scripts/Debug/ContractDebugger.cs"]
        modules, editmode, playmode = rmt.resolve(
            changed, FIXTURE_MANIFESTS, transitive=True, with_dependencies=True
        )
        assert len(editmode) == len(set(editmode)), "Duplicate EditMode tests found"
        assert len(playmode) == len(set(playmode)), "Duplicate PlayMode tests found"

    def test_with_dependencies_is_additive_to_transitive_expansion(self):
        """--with-dependencies adds deps; transitive dependent expansion still applies."""
        # Changing an input file: transitive expansion includes vehicle, debug, editor
        # with_dependencies: editor's deps (vehicle, input, camera, debug) — already present
        # vehicle's deps (input, core) — core is new
        changed = ["Assets/Scripts/Input/InputMath.cs"]
        modules_plain, _, _ = rmt.resolve(
            changed, FIXTURE_MANIFESTS, transitive=True, with_dependencies=False
        )
        modules_with, _, _ = rmt.resolve(
            changed, FIXTURE_MANIFESTS, transitive=True, with_dependencies=True
        )
        # with_dependencies is a superset of the plain result
        assert modules_plain.issubset(modules_with)
