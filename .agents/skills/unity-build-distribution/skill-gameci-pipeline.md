# GameCI Pipeline

> Part of the `unity-build-distribution` skill. See [SKILL.md](SKILL.md) for the overview.

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

