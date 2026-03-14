# CI Learnings

Accumulated knowledge from CI failures and fixes. Claude Code references this
when diagnosing new failures to avoid repeating past mistakes.

## Known Patterns

### LFS + Headless Unity

- **Problem:** `lfs: false` checkout leaves binary files as 134-byte pointer stubs.
  Unity import step warns but `continue-on-error: true` lets tests proceed.
- **Fix (v1):** Selective `git lfs pull --include="path/to/needed/assets"` for minimal bandwidth.
- **Fix (v2):** Removed LFS pull entirely from test job. Test-only assets (fonts, icons)
  are only for the GUI panel which never renders in `-batchmode`. Saves bandwidth and
  eliminates LFS budget exhaustion risk. Full LFS pull (`lfs: true`) only happens in
  the build/export job (tags/manual dispatch).

### C# Formatting and Linting

- **Problem:** `dotnet format` and analyzers may disagree on line length, naming conventions,
  or style rules. Misaligned settings cause CI failures on code that looks correct locally.
- **Fix:** Ensure `.editorconfig` and analyzer severity settings are consistent between
  local IDE and CI. Use the same `dotnet format` version in CI as locally.
- **Lesson:** When physics code uses standard Pacejka coefficients (B, C, D, E), ensure
  naming rules allow single-letter uppercase variable names (configure analyzer exceptions
  or suppress specific diagnostics).

### Deleted Paths in Lint Commands

- **Problem:** A deleted directory still referenced in CI lint/format commands causes
  "directory not found" failures.
- **Fix:** Remove deleted paths from all lint/format commands (CI workflows, scripts, hooks).
- **Prevention:** When deleting directories, grep CI files for references to that path.

### Namespace Collisions

- **Problem:** A class name like `Physics` or `Debug` can collide with `UnityEngine.Physics`
  or `UnityEngine.Debug`. This cascades — every script using the colliding name fails.
- **Fix:** Use fully qualified names (`UnityEngine.Physics.Raycast(...)`) or choose
  non-colliding class names. Avoid naming classes after common UnityEngine types.
- **Prevention:** Always check for namespace collisions before naming new classes.

### Test Constants Drifting from Scene Values

- **Problem:** Test constants (e.g., `DEFAULT_TIRE_RADIUS = 0.3f`) don't match scene
  overrides (e.g., prefab sets tire radius to `0.166f`).
- **Fix:** Update test constants to match actual scene/prefab values. Tests should mirror
  runtime values, not just script field defaults.
- **Prevention:** Reference constants from a shared static class rather than duplicating
  magic numbers in test files.

### Unity Test Framework Headless CI Failures

- **Problem:** Tests that depend on display/screen APIs, scene loading, or LFS assets
  fail on headless Linux CI (`-batchmode -nographics`).
- **Fix:** Use `SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null` guard for
  display-dependent tests. Use setup helpers instead of runtime scene loading.
  Add null guards on asset load results for LFS-dependent tests.
- **Prevention:** Any test loading scenes with mesh assets must guard for null references.

### DLL/Native Plugin Pointer Check Must Run After LFS Fetch

- **Problem:** DLL/SO pointer check in lint job always fails because `lfs: false`
  means all native plugin files are 134-byte LFS pointers.
- **Fix:** Move native plugin validation to the build job only (which uses `lfs: true`).
  Lint job only checks for temp files and formatting.

### Unity Import Timeout

- **Problem:** `Unity -batchmode -importAssets` can hang indefinitely on CI, especially
  with LFS pointer stubs or large asset sets.
- **Fix:** `timeout-minutes: 5` on test import (with `continue-on-error: true`),
  `timeout-minutes: 15` on build import.

### Registry Dependency Name Mismatch

- **Problem:** A system manifest declares a dependency name that doesn't match any
  existing manifest. Registry validation fails with "Missing dependency."
- **Fix:** Verify dependency names match exactly against manifest filenames/IDs.
- **Prevention:** Run `just validate-registry` locally before pushing.

### Orphan Files After Refactoring

- **Problem:** New scripts added during refactoring not added to their system manifests.
  Registry validation fails with orphan file errors.
- **Fix:** Add files to their respective manifests in the same commit as creation.
- **Prevention:** When creating new files, always add them to the appropriate manifest
  in the same commit.

### Unity Hangs on Shutdown After Tests Complete

- **Problem:** Unity finishes all tests and logs results, but the process doesn't
  terminate — hangs indefinitely until the GitHub Actions step timeout kills it.
  Tests actually passed.
- **Root Cause:** Unity batchmode shutdown can hang when native plugins (LFS pointers)
  or certain asset imports leave background threads alive.
- **Fix:** Wrap Unity invocation with `timeout`. If exit code indicates timeout kill
  AND test results XML shows all tests passed, treat as success. Otherwise treat as
  real timeout failure.

### Event-Driven Auto-Merge (Replaced Polling)

- **Problem:** Polling-based auto-merge (`schedule: cron`) is slow and wasteful.
- **Fix:** Event-driven triggers:
  - CI auto-label job: adds `ready-to-merge` on CI pass, removes on fail
  - PR guard: strips `ready-to-merge` on new push (synchronize) before CI starts
  - Auto-merge: triggers on `pull_request: labeled` + `push: main` (after merge)
  - Queue ordering: FIFO by label application time
- **GOTCHA:** `GITHUB_TOKEN` label changes do NOT trigger `pull_request: labeled` events
  in other workflows (GitHub security limitation). Must use a PAT (`MERGE_TOKEN`) for
  the auto-label and PR guard jobs so that label changes trigger auto-merge.

### MERGE_TOKEN PAT Required for Event-Driven Auto-Merge

- **MERGE_TOKEN PAT required:** The `MERGE_TOKEN` repo secret (classic PAT with `repo`
  and `workflow` scopes) must be configured for the event-driven auto-merge pipeline.
  Without it, `GITHUB_TOKEN` label changes don't trigger cross-workflow events, breaking
  the auto-label to auto-merge chain.

### LFS Stub Asset Error Flood

- **Problem:** Tests complete quickly, but thousands of "Failed to load" lines from
  LFS stub assets saturate the output pipe, causing timeouts during the
  summary/shutdown phase (not during tests).
- **Fix:** Delete LFS stub files that cause import loops before running tests:
  `find . -name "*.ttf" -size -1k -type f -delete` (or equivalent for problematic
  asset types). Real assets are much larger than 134-byte stubs.

### Settings Default Mismatch

- **Problem:** Default values in a settings manager don't match defaults set by the
  system that writes settings at startup. Tests expecting one default get another.
- **Fix:** Align default values across all systems. Update tests to match.
- **Prevention:** Use a single source of truth for default values (shared constants).

### LFS Stubs Break Asset-Dependent Tests

- **Problem:** Tests that assert on mesh counts, component properties, or asset data
  fail on CI because LFS stubs produce null references instead of real assets.
  One null cascades into many test failures.
- **Fix:** Add LFS stub guards: check for null asset references and skip gracefully
  on CI. Tests pass locally with real assets, skip on CI without false failures.
- **Prevention:** Any test loading assets with binary data (meshes, textures, audio)
  must guard for null/missing data.
