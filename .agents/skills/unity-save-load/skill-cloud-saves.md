# Cloud Saves

> Part of the `unity-save-load` skill. See [SKILL.md](SKILL.md) for the overview.

## Cloud Saves

### Steam Cloud (via Steamworks.NET)

```csharp
public class SteamCloudSave
{
    public static void SaveToCloud(string fileName, byte[] data)
    {
        if (!SteamRemoteStorage.FileWrite(fileName, data, data.Length))
        {
            Debug.LogError("Failed to write to Steam Cloud");
        }
    }

    public static byte[] LoadFromCloud(string fileName)
    {
        if (!SteamRemoteStorage.FileExists(fileName))
            return null;

        int fileSize = SteamRemoteStorage.GetFileSize(fileName);
        byte[] data = new byte[fileSize];
        SteamRemoteStorage.FileRead(fileName, data, fileSize);
        return data;
    }
}
```

### Conflict Resolution Pattern

When local and cloud saves diverge (e.g., played offline):

```csharp
public class SaveConflictResolver
{
    public enum Resolution { UseLocal, UseCloud, UseMostRecent }

    public static GameSaveData Resolve(
        GameSaveData localSave,
        GameSaveData cloudSave,
        Resolution strategy = Resolution.UseMostRecent)
    {
        if (localSave == null) return cloudSave;
        if (cloudSave == null) return localSave;

        return strategy switch
        {
            Resolution.UseLocal => localSave,
            Resolution.UseCloud => cloudSave,
            Resolution.UseMostRecent =>
                System.DateTime.Parse(localSave.saveTimestamp) >
                System.DateTime.Parse(cloudSave.saveTimestamp)
                    ? localSave : cloudSave,
            _ => cloudSave
        };
    }
}
```

## Auto-Save Pattern

```csharp
public class AutoSaveManager : MonoBehaviour
{
    [SerializeField] private float autoSaveIntervalSeconds = 300f; // 5 minutes
    private float _timeSinceLastSave;

    private void Update()
    {
        _timeSinceLastSave += Time.unscaledDeltaTime;

        if (_timeSinceLastSave >= autoSaveIntervalSeconds)
        {
            _timeSinceLastSave = 0f;
            PerformAutoSave();
        }
    }

    private void PerformAutoSave()
    {
        var data = GatherSaveData();
        SaveManager.Instance.Save(data, "autosave");
        Debug.Log("Auto-save completed");
    }

    // Also save on application pause/quit
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) PerformAutoSave();
    }

    private void OnApplicationQuit()
    {
        PerformAutoSave();
    }

    private GameSaveData GatherSaveData() { /* collect current game state */ return new GameSaveData(); }
}
```

