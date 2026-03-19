---
name: unity-system-manifest
description: Registers new C# files in resources/manifests/<system>.json (files array + tests.editmode/playmode list) and validates with just validate-registry. Use when creating any new script, adding a new game system, or fixing 'orphan file' CI failures. Trigger phrases: 'add to manifest', 'register file', 'new system', 'validate registry'. Key capabilities: exact JSON manifest format, ACTIVE/DEPRECATED/EXPERIMENTAL status, dependency declarations, test class name mapping for pre-push gating via resolve_module_tests.py. Do NOT use for modifying physics logic or editing game scripts unrelated to registration.
---
# Unity System Manifest

## Critical

- **Never commit without running `just validate-registry` first** — validation failures block CI (`Lint & Preflight` job).
- `files` paths must be relative to project root (e.g., `Assets/Scripts/Vehicle/RCCar.cs`), not absolute.
- `tests.editmode` entries are **class names only** — no `.cs`, no path. Validator maps `ClassName` → `Assets/Tests/EditMode/ClassName.cs`.
- If `status` is `DEPRECATED`, `replaced_by` must be non-empty or validation errors.
- No file may appear in two manifests — duplicate ownership is a hard error.
- Always add the `.asmdef` file to the manifest alongside scripts.

## Instructions

**Step 1 — Identify the owning system**

Find the manifest for the system you're modifying:
```
resources/manifests/<system>.json
```
Examples: `gameflow.json`, `vehicle.json`, `camera.json`. If no manifest exists, go to Step 2b.

**Verify:** `ls resources/manifests/` — confirm the file exists before editing.

---

**Step 2a — Add a file to an existing manifest**

Append to the `files` array. Example adding a new config script to `vehicle.json`:
```json
"files": [
  "Assets/Scripts/Vehicle/RCCar.cs",
  "Assets/Scripts/Vehicle/Config/NewConfig.cs"
]
```
If the file has a corresponding test class, add it:
```json
"tests": {
  "editmode": ["ExistingTests", "NewConfigTests"],
  "playmode": []
}
```

**Verify:** The file path actually exists on disk before adding.

---

**Step 2b — Create a new system manifest**

Create `resources/manifests/<system>.json` with this exact structure:
```json
{
  "name": "system_name",
  "description": "One-line description of what this system does.",
  "status": "ACTIVE",
  "files": [
    "Assets/Scripts/SystemName/MainClass.cs",
    "Assets/Scripts/SystemName/R8EOX.SystemName.asmdef"
  ],
  "dependencies": ["core"],
  "replaced_by": "",
  "tests": {
    "editmode": ["MainClassTests"],
    "playmode": []
  }
}
```

Status values: `ACTIVE` (in use) · `DEPRECATED` (replaced — set `replaced_by`) · `EXPERIMENTAL` (WIP, not integrated)

**Verify:** `name` matches the filename stem exactly (e.g., `"name": "gameflow"` ↔ `gameflow.json`).

---

**Step 3 — Declare dependencies**

List system names (not class names) this system calls into:
```json
"dependencies": ["core", "input"]
```
Dependencies drive transitive test expansion in `resolve_module_tests.py` — a change in `input` also runs tests for every system that lists `input` as a dependency.

**Verify:** Each dependency name matches an existing manifest filename stem in `resources/manifests/`.

---

**Step 4 — Run validation**

```bash
just validate-registry
```

Expected clean output:
```
=== Registry Validation: resources/manifests ===
  [ACTIVE] vehicle (20 files)
  ...
Validation: CLEAN
```

**Verify:** Exit code 0 before committing. Warnings are non-blocking; errors are.

---

**Step 5 — Commit the manifest change**

Commit the manifest alongside the script file(s) it registers:
```
git add resources/manifests/vehicle.json Assets/Scripts/Vehicle/Config/NewConfig.cs
git commit -m "feat: add NewConfig to vehicle manifest"
```

## Examples

**User says:** "I just created `Assets/Scripts/Camera/FollowTarget.cs` — add it to the manifest."

Actions:
1. Read `resources/manifests/camera.json`
2. Append `"Assets/Scripts/Camera/FollowTarget.cs"` to `files`
3. If a test class `FollowTargetTests.cs` exists in `Assets/Tests/EditMode/`, add `"FollowTargetTests"` to `tests.editmode`
4. Run `just validate-registry` — confirm `CLEAN`
5. Commit both files together

Result: `camera.json` has the new file registered; CI `Lint & Preflight` passes.

---

**User says:** "Create a new manifest for the `Track` system."

Create `resources/manifests/track.json`:
```json
{
  "name": "track",
  "description": "Track layout, spawn points, and surface zone management.",
  "status": "ACTIVE",
  "files": [
    "Assets/Scripts/Track/TrackManager.cs",
    "Assets/Scripts/Track/R8EOX.Track.asmdef"
  ],
  "dependencies": ["core"],
  "replaced_by": "",
  "tests": {
    "editmode": ["TrackManagerTests"],
    "playmode": []
  }
}
```
Run `just validate-registry` → `CLEAN` → commit.

## Common Issues

**`[vehicle] Missing file: Assets/Scripts/Vehicle/NewConfig.cs`**
The file path in `files` doesn't exist on disk. Check spelling and that the file was actually created. Paths are case-sensitive on Linux CI even if macOS is forgiving.

**`[vehicle] Duplicate ownership: Assets/Scripts/Vehicle/RCCar.cs (also claimed by core)`**
Two manifests list the same file. Remove the entry from the manifest that doesn't own it.

**`[vehicle] WARNING: Test class not found: NewConfigTests`**
The class name in `tests.editmode` has no matching file at `Assets/Tests/EditMode/NewConfigTests.cs`. Either create the test file or remove the entry. Warnings don't block CI but disable pre-push test gating for that class.

**`[track] DEPRECATED but no replaced_by`**
Set `"replaced_by": "new_system_name"` or change `status` back to `ACTIVE`.

**`Validation: 2 error(s)`** (generic CI failure)
Run `just validate-registry` locally — it prints per-system error lines. Fix all errors before pushing; CI will reject the PR otherwise.