---
name: unity-build-distribution
description: Unity Build & Distribution
---


# Unity Build & Distribution

Use this skill when configuring build settings, setting up IL2CPP/Mono backends, integrating with Steam, or building CI/CD pipelines for Unity game distribution.

## IL2CPP vs Mono

| | Mono | IL2CPP |
|---|------|--------|
| Build time | Fast | Slow (C++ compilation) |
| Runtime perf | Baseline | 1.5-3x faster |
| Binary size | Smaller | Larger |
| Decompilation | Easy (ILSpy) | Hard (native) |
| Console support | No | Required |
| **Shipping?** | Dev builds only | **Mandatory for release** |

Always ship IL2CPP. Use Mono only for fast iteration during development.

## C++ Compiler Configuration

Set in Player Settings > Other Settings > C++ Compiler Configuration:

| Config | Use Case |
|--------|----------|
| Debug | Development builds with full debugging |
| Release | **Shipping builds** — optimized with crash diagnostics retained |
| Master | Maximum optimization — strips crash diagnostics, NOT recommended |

Use **Release** for shipping, not Master. Master removes diagnostic information needed to interpret crash reports. The performance difference is negligible.

## Build Profiles (Unity 6)

Unity 6 replaces the old Build Settings with named Build Profile assets:

- Create via **File > Build Profiles > Create New Profile**
- Profiles are `.asset` files — commit them to version control
- Each profile stores: platform, scenes list, scripting backend, defines, compression

Recommended profiles:

| Profile | Purpose | Defines |
|---------|---------|---------|
| `Demo.asset` | Steam demo build | `DEMO_BUILD` |
| `Beta.asset` | Beta testing | `BETA_BUILD` |
| `Release.asset` | Full shipping build | (none — release is default) |

Switch profiles via `BuildProfile.SetActiveBuildProfile()` or command line.

## Steam Store

- **Coming Soon page:** Create as early as possible — wishlists accumulate before launch
- **Store review:** Allow **7 business days** minimum for Valve review of store page changes
- **Demo:** Register as a **separate AppID** linked to the main game — shares store page but builds independently
- **Shared depots:** Demo can reference the main game's depots to avoid duplicating shared assets

## Memory Management

- **Incremental GC:** Enable in Player Settings (reduces GC spikes from 10ms+ to < 1ms)
- **Avoid per-frame allocations:** No `new` in Update, no LINQ in hot paths, no string concatenation in loops
- **Pool everything:** Object pools for bullets, particles, UI elements, anything instantiated frequently
- **Profile the built player, not the Editor:** Editor adds 2-5x memory overhead from inspectors, handles, editor scripts

## Crash Reporting

### Sentry for Unity

```csharp
// Auto-configures via SentryOptions ScriptableObject in Resources/
// IL2CPP symbolication: upload symbols in CI post-build
```

Key features:
- **Automatic IL2CPP symbolication** — upload debug symbols in CI
- **Breadcrumbs** — automatic breadcrumbs for scene loads, UI clicks, errors
- **Release health** — crash-free session rate, adoption tracking
- **Performance monitoring** — transaction tracing for load times

Upload symbols in CI:
```bash
sentry-cli debug-files upload --include-sources ./build/
```

## Demo Strategy

- **Separate build:** Demo is a separate AppID on Steam, linked to the main game
- **Content:** 1 track + 2-3 vehicles — enough to showcase core gameplay
- **Shared depots:** Reference the main game's shared asset depot to avoid duplicating engine/UI assets
- **Progress transfer:** Save demo career data to the same `persistentDataPath` location — main game reads it on first launch
- **Feature gates:** Use `#if DEMO_BUILD` preprocessor directives for demo-specific limits

## Versioning

- **SemVer** from git tags: `v1.2.3`
- **GameCI versioning:** Set to `Tag` — reads version from the latest git tag
- **Build metadata:** Append git SHA for traceability: `1.2.3+abc1234`

## Branch Management (Steam)

Use Steam branches (betas) for staged rollouts:

| Steam Branch | Purpose | Access |
|-------------|---------|--------|
| `nightly` | Automated CI builds | Internal only (password) |
| `beta` | Opt-in beta testing | Public beta signup |
| `public-beta` | Wide beta before release | Public, visible in dropdown |
| (default) | Stable release | Everyone |

Promotion flow: `nightly` -> `beta` -> `public-beta` -> `default`

**Rollback:** Set a previous build live on any branch via Steamworks dashboard. Instant, no rebuild needed.

## Hotfix Process

1. **Branch from the release tag:** `git checkout -b hotfix/1.2.1 v1.2.0`
2. **Minimal fix only** — no feature work, no refactoring
3. **CI builds and deploys to `nightly`** branch automatically
4. **Promote to `beta`** — 30-minute smoke test
5. **Promote to `default`** — monitor crash-free rate for 24 hours
6. **Tag and merge back:** `git tag v1.2.1` then merge hotfix branch into main


## Topic Pages

- [Build Size Optimization](skill-build-size-optimization.md)
- [Steam Integration](skill-steam-integration.md)
- [GameCI Pipeline](skill-gameci-pipeline.md)
- [Quality Presets](skill-quality-presets.md)
- [Managed Stripping Levels](skill-managed-stripping-levels.md)
- [Addressables](skill-addressables.md)
- [Startup Optimization](skill-startup-optimization.md)
- [Platform Specifics](skill-platform-specifics.md)

