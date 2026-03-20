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

## Managed Stripping Levels

IL2CPP strips unused code to reduce binary size. Set in Player Settings > Other Settings:

| Level | Behavior | Risk |
|-------|----------|------|
| Minimal | Almost nothing stripped | Safe, large binary |
| **Low** | Conservative strip | **Safe default for shipping** |
| Medium | Aggressive strip | May break reflection |
| High | Maximum strip | Will break reflection-heavy code |

Use **Low** as the shipping default. If you need Medium/High, create a `link.xml` to preserve reflection-accessed types:

```xml
<linker>
    <assembly fullname="Assembly-CSharp">
        <type fullname="MyNamespace.SerializedClass" preserve="all"/>
    </assembly>
    <assembly fullname="UnityEngine.InputModule" preserve="all"/>
</linker>
```

Place `link.xml` in the `Assets/` root.

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

## Build Size Optimization

### Reading the Build Report

After every build, check the Editor Log for the build report:

**Edit > Preferences > General > Editor Log** (or `%LOCALAPPDATA%/Unity/Editor/Editor.log`)

The report lists asset categories by size. Focus on the largest first (usually Textures, then Audio, then Meshes).

### Texture Compression

| Format | Use Case | Quality | Size |
|--------|----------|---------|------|
| DXT1 | Opaque textures (no alpha) | Good | 4 bpp |
| DXT5 | Textures with alpha | Good | 8 bpp |
| **BC7** | **Best quality for both** | Excellent | 8 bpp |
| Crunch (DXT1) | Download size reduction | Lossy | Variable |
| Crunch (DXT5) | Download size reduction | Lossy | Variable |

- Use **BC7** for quality-critical textures (vehicle liveries, UI)
- Use **Crunch compression** at quality 70-85 for textures where download size matters more than load time
- Crunch adds decompression time at load but significantly reduces download/install size

### Audio Compression

| Format | Use Case | Memory |
|--------|----------|--------|
| **PCM** | Short SFX (< 1s, impacts, clicks) | High (uncompressed) |
| **ADPCM** | Repetitive SFX (engine loops, tire noise) | Medium (3.5:1) |
| **Vorbis** | Music, ambient, long audio | Low (streaming) |

- Set Load Type to **Streaming** for Vorbis music tracks
- Set Load Type to **Decompress On Load** for short PCM SFX
- Set Load Type to **Compressed In Memory** for ADPCM loops

## Addressables

### Group Strategy

Group assets by loading context, not by type:

| Group | Contents | Load When |
|-------|----------|-----------|
| `Persistent_Shared` | UI atlas, common materials, player vehicle | Boot, kept in memory |
| `Track_Outpost_Assets` | Outpost terrain, props, lighting | Entering Outpost track |
| `Track_Desert_Assets` | Desert terrain, props, lighting | Entering Desert track |
| `Vehicles_Common` | Shared vehicle parts, physics materials | Vehicle selection |

### Content Update Workflow

For post-release patches without requiring a full reinstall:

1. Build Addressables with **Build Remote Catalog** enabled
2. For patches: **Update a Previous Build** (do not do a clean build)
3. Upload changed bundles to CDN
4. Game checks remote catalog on launch, downloads only changed bundles

## Startup Optimization

1. **Boot scene is minimal:** Logo, loading bar, nothing else
2. **Async scene loading:** `SceneManager.LoadSceneAsync()` for all scene transitions
3. **Addressables preload:** Start loading persistent groups in boot scene
4. **Async upload buffer:** Set `QualitySettings.asyncUploadBufferSize` to 64-128 MB (default 4 MB is too low)
5. **Async upload time slice:** Set `QualitySettings.asyncUploadTimeSlice` to 4-8 ms

```csharp
// Boot scene MonoBehaviour
IEnumerator Start()
{
    // Preload persistent assets
    var handle = Addressables.LoadAssetsAsync<Object>("Persistent_Shared", null);
    yield return handle;

    // Load main menu
    yield return SceneManager.LoadSceneAsync("MainMenu");
}
```

## Steam Integration

### Facepunch.Steamworks (Recommended)

Use [Facepunch.Steamworks](https://github.com/Facepunch/Facepunch.Steamworks) — the C# native wrapper. Do NOT use Steamworks.NET (more boilerplate, C-style API).

```csharp
// Initialize in boot scene
void Awake()
{
    try
    {
        SteamClient.Init(YOUR_APP_ID);
    }
    catch (Exception e)
    {
        Debug.LogError($"Steam init failed: {e.Message}");
        // Game should still work without Steam for development
    }
}

void OnApplicationQuit()
{
    SteamClient.Shutdown();
}

void Update()
{
    SteamClient.RunCallbacks(); // Required every frame
}
```

### Steam Features

| Feature | API | Notes |
|---------|-----|-------|
| Achievements | `SteamUserStats.SetAchievement()` | Call `StoreStats()` after setting |
| Leaderboards | `SteamUserStats.FindOrCreateLeaderboardAsync()` | Async, cache the leaderboard reference |
| Cloud Save | Steam Auto-Cloud (dashboard config) | Zero code, syncs persistentDataPath |
| Steam Input | `SteamInput` API | Handles all controller types, action sets |
| Rich Presence | `SteamFriends.SetRichPresence()` | "Racing on Outpost Track" |

## SteamPipe Upload

### Depot Configuration

```vdf
// app_build.vdf
"AppBuild"
{
    "AppID" "YOUR_APP_ID"
    "Desc" "v1.2.3 build"
    "ContentRoot" "./build/"
    "BuildOutput" "./steam_output/"
    "Depots"
    {
        "YOUR_DEPOT_ID_WIN"
        {
            "FileMapping"
            {
                "LocalPath" "StandaloneWindows64/*"
                "DepotPath" "."
                "recursive" "1"
            }
        }
        "YOUR_DEPOT_ID_LINUX"
        {
            "FileMapping"
            {
                "LocalPath" "StandaloneLinux64/*"
                "DepotPath" "."
                "recursive" "1"
            }
        }
    }
}
```

### CI Automation

Use `steamcmd` in CI or the `game-ci/steam-deploy` GitHub Action (preferred). Store Steam credentials as GitHub Secrets.

## Steam Store

- **Coming Soon page:** Create as early as possible — wishlists accumulate before launch
- **Store review:** Allow **7 business days** minimum for Valve review of store page changes
- **Demo:** Register as a **separate AppID** linked to the main game — shares store page but builds independently
- **Shared depots:** Demo can reference the main game's depots to avoid duplicating shared assets

## GameCI Pipeline

### Build Matrix

```yaml
strategy:
  matrix:
    targetPlatform:
      - StandaloneWindows64
      - StandaloneLinux64
      - StandaloneOSX
```

### Key Configuration

```yaml
- uses: game-ci/unity-builder@v4
  with:
    targetPlatform: ${{ matrix.targetPlatform }}
    buildMethod: BuildScript.PerformBuild
    versioning: Tag              # SemVer from git tags
    # NEVER use: releaseBranch: default
    # ^^^ This silently fails — always use a named branch
```

**CRITICAL:** Never set `releaseBranch: default`. This is a known GameCI bug that causes builds to silently fail or produce wrong versions. Always use a named branch or omit the parameter.

### Library Cache

Cache the `Library/` folder to dramatically speed up builds:

```yaml
- uses: actions/cache@v4
  with:
    path: Library
    key: Library-${{ matrix.targetPlatform }}-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
    restore-keys: |
      Library-${{ matrix.targetPlatform }}-
```

### Steam Deploy

```yaml
- uses: game-ci/steam-deploy@v3
  with:
    appId: ${{ secrets.STEAM_APP_ID }}
    buildDescription: v${{ steps.version.outputs.version }}
    rootPath: build
    depot1Path: StandaloneWindows64
    depot2Path: StandaloneLinux64
```

## Platform Specifics

### Windows

- **Graphics APIs:** DX11 first, DX12 second in the list (DX12 still has edge-case issues on older hardware)
- **Default resolution:** 1920x1080 windowed, let player change
- **Anti-cheat:** Not needed for a racing sim — keep it simple

### Linux

- **Graphics API:** Vulkan primary, OpenGL ES fallback
- **Proton compatibility:** Test with Proton — many Linux users run Windows builds via Proton
- **Steam Deck:** Verified status requires controller support and 1280x800 UI scaling

### macOS

- **Code signing:** Required for distribution — use `codesign` in CI
- **Notarization:** Required for non-App Store distribution — `xcrun notarytool submit`
- **Universal Binary:** Build for both Intel and Apple Silicon (`UniversalArchitecture: true`)

## Quality Presets

| Setting | Low | Medium | High | Ultra |
|---------|-----|--------|------|-------|
| Texture Quality | Quarter | Half | Full | Full |
| Shadow Resolution | 512 | 1024 | 2048 | 4096 |
| Shadow Distance | 50 | 100 | 150 | 200 |
| Anti-Aliasing | Off | FXAA | SMAA | TAA |
| Ambient Occlusion | Off | SSAO Low | SSAO Med | SSAO High |
| LOD Bias | 0.5 | 1.0 | 1.5 | 2.0 |
| Particle Density | 25% | 50% | 75% | 100% |

### Auto-Detect

```csharp
int DetectQualityLevel()
{
    int vram = SystemInfo.graphicsMemorySize;
    if (vram >= 8192) return 3;      // Ultra
    if (vram >= 4096) return 2;      // High
    if (vram >= 2048) return 1;      // Medium
    return 0;                         // Low
}
```

Run auto-detect on first launch only. Save the result so the player's manual changes persist.

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
