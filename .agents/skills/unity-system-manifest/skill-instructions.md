# Instructions

> Part of the `unity-system-manifest` skill. See [SKILL.md](SKILL.md) for the overview.

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

