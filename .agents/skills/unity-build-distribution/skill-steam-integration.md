# Steam Integration

> Part of the `unity-build-distribution` skill. See [SKILL.md](SKILL.md) for the overview.

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

