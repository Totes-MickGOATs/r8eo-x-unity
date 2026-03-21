---
name: unity-networking
description: Unity Networking
---


# Unity Networking

Use this skill when implementing multiplayer networking with Netcode for GameObjects, including RPCs, state synchronization, network spawning, and lobby/relay setup.

## Package Setup

```json
// Packages/manifest.json
"com.unity.netcode.gameobjects": "2.1.1",
"com.unity.transport": "2.3.0"
```

For Unity Gaming Services (relay, lobby):
```json
"com.unity.services.relay": "1.1.1",
"com.unity.services.lobby": "1.2.2",
"com.unity.services.authentication": "3.3.3"
```

## Scene Management

### NetworkSceneManager

Server controls scene loading for all clients:

```csharp
// Server-side scene loading
public class GameFlowManager : NetworkBehaviour
{
    public void LoadGameScene()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }

    void OnEnable()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
    }

    void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode mode)
    {
        Debug.Log($"Client {clientId} finished loading {sceneName}");
    }
}
```



## Topic Pages

- [NetworkManager](skill-networkmanager.md)
- [LAN Discovery](skill-lan-discovery.md)

