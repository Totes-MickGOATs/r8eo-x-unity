# C# Lint Baseline — 2026-03-20

## Summary

| Category | Count |
|----------|-------|
| Debug.Log\* (runtime) | 13 |
| FindObject/Find/Resources.Load | 0 |
| Raw GUID in asmdef | 0 |
| String layer/tag/scene lookups | 0 |
| syntax-check portability issues | 3 |
| Manifest orphan files | 1 |
| Files over 200 lines | 1 (auto-generated, allowlisted) + 1 (RCCar.cs) |

---

## 1. Debug.Log\* in Runtime Assemblies

Scanned: `Assets/Scripts/Vehicle/`, `Assets/Scripts/Input/`, `Assets/Scripts/Camera/`, `Assets/Scripts/Core/`, `Assets/Scripts/GameFlow/`, `Assets/Scripts/Track/`, `Assets/Scripts/UI/`, `Assets/Scripts/Shared/`

| File | Line | Type | Snippet |
|------|------|------|---------|
| `Assets/Scripts/Vehicle/RCCar.cs` | 139 | Log | `Debug.Log($"[RCCar] Motor={_motorPreset} engine=...")` |
| `Assets/Scripts/Vehicle/RCCar.cs` | 190 | Log | `Debug.Log($"[esc] throttle=...")` — inside timer guard |
| `Assets/Scripts/Vehicle/RaycastWheel.cs` | 158 | Log | `Debug.Log($"[suspension] wheel={name} ...")` |
| `Assets/Scripts/Vehicle/RaycastWheel.cs` | 159 | Log | `Debug.Log($"[grip] wheel={name} ...")` |
| `Assets/Scripts/Vehicle/Drivetrain.cs` | 125 | Log | `Debug.Log($"[drivetrain] totalForce=...")` |
| `Assets/Scripts/GameFlow/SceneBootstrapper.cs` | 36 | Log | `Debug.Log("[SceneBootstrapper] No GameFlowManager found. Creating standalone manager.")` |
| `Assets/Scripts/Track/SurfaceZone.cs` | 36 | LogWarning | `Debug.LogWarning($"[SurfaceZone] Collider on '{name}' must be a trigger. Setting isTrigger=true.")` |
| `Assets/Scripts/Track/SurfaceZone.cs` | 41 | LogError | `Debug.LogError($"[SurfaceZone] No SurfaceConfig assigned on '{name}'.")` |
| `Assets/Scripts/UI/UIManager.cs` | 79 | LogWarning | `Debug.LogWarning($"[UIManager] No ScreenRegistry assigned. Cannot show '{screenId}'.")` |
| `Assets/Scripts/UI/UIManager.cs` | 85 | LogWarning | `Debug.LogWarning($"[UIManager] Screen '{screenId}' not found in registry.")` |
| `Assets/Scripts/UI/UIManager.cs` | 97 | LogWarning | `Debug.LogWarning($"[UIManager] Overlay '{screenId}' not found in registry.")` |
| `Assets/Scripts/UI/UIManager.cs` | 105 | LogError | `Debug.LogError($"[UIManager] Overlay prefab '{screenId}' has no IScreen component.")` |
| `Assets/Scripts/UI/UIManager.cs` | 156 | LogError | `Debug.LogError($"[UIManager] Prefab '{screenId}' has no IScreen component.")` |

**Notes:**
- `RCCar.cs:190` and `RaycastWheel.cs:158-159` and `Drivetrain.cs:125` appear to be telemetry/debug diagnostic logs gated by a flag or timer — candidates for `#if UNITY_EDITOR` or a conditional compile guard.
- `SurfaceZone.cs` and `UIManager.cs` LogError/LogWarning calls are legitimate runtime error signals, but should use a project-level logger abstraction rather than `Debug.*` directly if a logging policy is desired.
- `SceneBootstrapper.cs:36` is informational startup logging.

---

## 2. FindObjectOfType / GameObject.Find / Resources.Load

**None found** in runtime assemblies.

All occurrences of `FindObjectOfType` and `GameObject.Find` are in `Assets/Scripts/Editor/` (exempted).

---

## 3. Raw GUID References in .asmdef Files

**Verified clean on 2026-03-20** — All 12 `.asmdef` files use assembly-name references. No `"GUID:"` prefixes present in any `"references"` array.

Files checked:
- `Assets/Scripts/Vehicle/R8EOX.Vehicle.asmdef` — references: `R8EOX.Vehicle.Physics`, `R8EOX.Input`, `R8EOX.Core` ✓
- `Assets/Scripts/Vehicle/Physics/R8EOX.Vehicle.Physics.asmdef` — references: (none) ✓
- `Assets/Scripts/Input/R8EOX.Input.asmdef` — references: `Unity.InputSystem` ✓
- `Assets/Scripts/Camera/R8EOX.Camera.asmdef` — references: `Unity.InputSystem` ✓
- `Assets/Scripts/Core/R8EOX.Core.asmdef` — references: (none) ✓
- `Assets/Scripts/GameFlow/R8EOX.GameFlow.asmdef` — references: `R8EOX.Core` ✓
- `Assets/Scripts/Track/R8EOX.Track.asmdef` — references: `R8EOX.Core` ✓
- `Assets/Scripts/UI/R8EOX.UI.asmdef` — references: `R8EOX.Core`, `R8EOX.GameFlow` ✓
- `Assets/Scripts/Shared/R8EOX.Shared.asmdef` — references: (none) ✓
- `Assets/Scripts/Debug/R8EOX.Debug.asmdef` — references: `R8EOX.Vehicle`, `R8EOX.Input`, `Unity.InputSystem` ✓
- `Assets/Scripts/Debug/Audit/R8EOX.Debug.Audit.asmdef` — references: `R8EOX.Debug` ✓
- `Assets/Scripts/Editor/R8EOX.Editor.asmdef` — references: `R8EOX.Vehicle`, `R8EOX.Input`, `R8EOX.Camera`, `R8EOX.Debug`, `R8EOX.Core`, `R8EOX.Track`, `R8EOX.GameFlow`, `R8EOX.Shared` ✓

The policy linter rule `GUID_ASMDEF` will enforce this going forward.

---

## 4. String Layer/Tag/Scene Lookups

**None found** in runtime assemblies.

The only `LayerMask.NameToLayer` call is in `Assets/Scripts/Editor/Builders/RCBuggyBuilder.cs:27` which is in the exempted Editor assembly.

---

## 5. syntax-check-csharp.sh Portability Notes

Script path: `scripts/tools/syntax-check-csharp.sh`

**3 portability issues found:**

1. **`grep -o` for single-char counting (lines 29–30, 38–39):**
   `grep -o '{' "$file" | wc -l` — This construct uses POSIX `grep -o`, which is portable (GNU and BSD). No issue here by itself, but the intent (counting chars) could be done more robustly with `tr -cd '{' < "$file" | wc -c`. Low priority.

2. **`grep -n ';;' "$file" | grep -v ';;[[:space:]]*//' | head -3 | grep -q .` (line 71):**
   The POSIX character class `[[:space:]]` is portable, so this is fine on both macOS and Linux. However, the pipeline chains `grep -n` with `grep -v` and then `head -3 | grep -q .` — this can produce a SIGPIPE-related exit-code issue when `set -euo pipefail` is active and `head` terminates the pipe early before all grep output is consumed. On macOS (BSD grep), this reliably causes a non-zero exit from the upstream `grep -v`, which `set -e` will catch and abort the entire script. **This is a live macOS portability bug.**

3. **`find Assets/Scripts Assets/Tests -name '*.cs' -print0` (line 82):**
   Uses relative paths `Assets/Scripts` and `Assets/Tests` — the script must be run from the repository root. If invoked from any other working directory, the `find` will silently find no files. This is a fragility issue rather than a GNU-specific construct, but it affects portability of invocation context. The staged-files path (default mode, lines 87-89) similarly relies on `git diff` relative paths being passed directly to `check_file`, which uses them as-is with `[[ -f "$file" ]]` — this will silently skip files if the cwd is not the repo root.

**Summary:** The script has no GNU-only constructs (`grep -P` is not used; `sed -i` is not used). The main issue is the `set -euo pipefail` + `head` SIGPIPE bug on macOS (item 2) and the implicit cwd dependency (item 3).

---

## 6. Manifest Orphan Files

**1 orphan file found:**

| File | Reason |
|------|--------|
| `Assets/Scripts/Input/R8EOXInputActions.cs` | Auto-generated by Unity Input System (`com.unity.inputsystem:InputActionCodeGenerator v1.8.2`) from `R8EOXInputActions.inputactions`. The `.inputactions` source file is registered in `resources/manifests/input.json`, but the generated `.cs` output is not. |

**Note:** This file is auto-generated and should either be added to `input.json` with a note marking it as auto-generated, or explicitly excluded from manifest coverage checks via an allowlist.

---

## 7. Files Over 200 Lines

| File | Line Count | Notes |
|------|-----------|-------|
| `Assets/Scripts/Input/R8EOXInputActions.cs` | 580 | Auto-generated by Unity Input System — already allowlisted in `syntax-check-csharp.sh` |
| `Assets/Scripts/Vehicle/RCCar.cs` | 255 | **Violates 200-line policy.** Excess: 55 lines. |

**`RCCar.cs` decomposition candidates:**
- The debug logging timer (`_debugLogTimer`) and its associated log output at line 190 could be extracted to a diagnostic helper or removed behind a compile flag (~5 lines).
- Motor initialization logging at lines 139-140 could be conditional on `Debug.isDebugBuild`.
- These changes alone would not bring the file under 200 lines; more substantial refactoring (e.g., separating input smoothing or ESC logic into a dedicated class) would be needed.

---

## Allowlist Recommendations

| Violation | File | Recommendation |
|-----------|------|----------------|
| Debug.Log (telemetry) | `RCCar.cs:139,190`, `RaycastWheel.cs:158-159`, `Drivetrain.cs:125` | Wrap in `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` or extract to a `VehicleTelemetryLogger` helper. Until then, add to a `lint-allowlist.txt` as `debug-log:telemetry`. |
| Debug.LogWarning/Error (guard clauses) | `SurfaceZone.cs:36,41`, `UIManager.cs:79,85,97,105,156` | These are legitimate runtime error guards. Long-term: introduce a `ILogger` abstraction. Short-term: allowlist as `debug-log:guard-clause`. |
| Debug.Log (startup) | `SceneBootstrapper.cs:36` | Wrap in `#if UNITY_EDITOR`. Allowlist as `debug-log:startup`. |
| Manifest orphan | `R8EOXInputActions.cs` | Add to `input.json` with `"auto-generated": true` annotation, or add to a manifest-check exclusion list. |
| File over 200 lines | `RCCar.cs` (255 lines) | File a refactoring task. Until done, add to `syntax-check-csharp.sh` allowlist alongside `R8EOXInputActions.cs`. |
| syntax-check SIGPIPE bug | `scripts/tools/syntax-check-csharp.sh:71` | Replace `head -3 \| grep -q .` with `grep -qm1 .` to avoid SIGPIPE under `set -euo pipefail` on macOS. |
