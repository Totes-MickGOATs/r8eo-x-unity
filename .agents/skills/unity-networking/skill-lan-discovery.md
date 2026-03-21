# LAN Discovery

> Part of the `unity-networking` skill. See [SKILL.md](SKILL.md) for the overview.

## LAN Discovery

For local network games without Unity Gaming Services:

```csharp
public class LANDiscovery : MonoBehaviour
{
    [SerializeField] int _broadcastPort = 47777;
    [SerializeField] float _broadcastInterval = 1f;

    UdpClient _broadcastClient;
    UdpClient _listenClient;
    readonly List<ServerInfo> _discoveredServers = new();

    public struct ServerInfo
    {
        public string Name;
        public string Address;
        public int Port;
        public int PlayerCount;
        public int MaxPlayers;
    }

    // Host: broadcast presence
    public void StartBroadcasting(string serverName, int gamePort)
    {
        _broadcastClient = new UdpClient();
        _broadcastClient.EnableBroadcast = true;
        StartCoroutine(BroadcastLoop(serverName, gamePort));
    }

    IEnumerator BroadcastLoop(string name, int port)
    {
        var endpoint = new IPEndPoint(IPAddress.Broadcast, _broadcastPort);
        while (true)
        {
            string msg = $"GAME|{name}|{port}|{GetPlayerCount()}|{GetMaxPlayers()}";
            byte[] data = Encoding.UTF8.GetBytes(msg);
            _broadcastClient.Send(data, data.Length, endpoint);
            yield return new WaitForSeconds(_broadcastInterval);
        }
    }

    // Client: listen for broadcasts
    public void StartListening()
    {
        _listenClient = new UdpClient(_broadcastPort);
        _listenClient.EnableBroadcast = true;
        _listenClient.BeginReceive(OnReceive, null);
    }

    void OnReceive(IAsyncResult result)
    {
        var endpoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] data = _listenClient.EndReceive(result, ref endpoint);
        string msg = Encoding.UTF8.GetString(data);
        string[] parts = msg.Split('|');

        if (parts[0] == "GAME")
        {
            var server = new ServerInfo
            {
                Name = parts[1],
                Address = endpoint.Address.ToString(),
                Port = int.Parse(parts[2]),
                PlayerCount = int.Parse(parts[3]),
                MaxPlayers = int.Parse(parts[4])
            };
            // Add to discovered list (main thread via queue)
        }

        _listenClient.BeginReceive(OnReceive, null);
    }
}
```

## Common Patterns

### Lobby Ready-Check and Countdown

```csharp
public class LobbyController : NetworkBehaviour
{
    public NetworkList<bool> PlayerReady;
    public NetworkVariable<float> CountdownTimer = new(-1f);
    const float COUNTDOWN_DURATION = 5f;

    void Awake() => PlayerReady = new NetworkList<bool>();

    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(bool ready, ServerRpcParams rpcParams = default)
    {
        int index = GetPlayerIndex(rpcParams.Receive.SenderClientId);
        PlayerReady[index] = ready;

        if (AllPlayersReady())
            CountdownTimer.Value = COUNTDOWN_DURATION;
        else
            CountdownTimer.Value = -1f; // Cancel countdown
    }

    void Update()
    {
        if (!IsServer || CountdownTimer.Value < 0) return;

        CountdownTimer.Value -= Time.deltaTime;
        if (CountdownTimer.Value <= 0f)
        {
            CountdownTimer.Value = -1f;
            StartGame();
        }
    }

    bool AllPlayersReady()
    {
        if (PlayerReady.Count == 0) return false;
        foreach (bool ready in PlayerReady)
            if (!ready) return false;
        return true;
    }
}
```

### Object Spawn Pooling

```csharp
public class NetworkObjectPool : MonoBehaviour
{
    [SerializeField] GameObject _prefab;
    [SerializeField] int _preSpawnCount = 20;
    Queue<NetworkObject> _pool = new();

    void Start()
    {
        NetworkManager.Singleton.PrefabHandler.AddHandler(
            _prefab, new PooledPrefabHandler(this));

        for (int i = 0; i < _preSpawnCount; i++)
        {
            var obj = Instantiate(_prefab).GetComponent<NetworkObject>();
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }

    public NetworkObject Get()
    {
        if (_pool.Count == 0)
        {
            var obj = Instantiate(_prefab).GetComponent<NetworkObject>();
            return obj;
        }
        var pooled = _pool.Dequeue();
        pooled.gameObject.SetActive(true);
        return pooled;
    }

    public void Return(NetworkObject obj)
    {
        obj.gameObject.SetActive(false);
        _pool.Enqueue(obj);
    }
}
```

### NGO 2.x: Distributed Authority and Unified RPCs (Unity 6)

> **Unity 6 / NGO 2.x:** Two major additions:
>
> - **Distributed Authority topology:** A new session mode where clients share authority over objects without a dedicated server or host. Objects are owned by the client that spawns them. Ideal for peer-to-peer casual games. Enable via `NetworkManager.NetworkConfig.NetworkTopology = NetworkTopologyTypes.DistributedAuthority`.
> - **Unified `[Rpc]` attribute:** Replaces the separate `[ServerRpc]` / `[ClientRpc]` attributes with a single `[Rpc(SendTo.Server)]`, `[Rpc(SendTo.Everyone)]`, `[Rpc(SendTo.NotMe)]`, etc. The old attributes still work but are deprecated. The method name suffix convention (`ServerRpc`/`ClientRpc`) is no longer required with the unified attribute.

### Host Migration Considerations

NGO does not have built-in host migration. Options:

1. **Dedicated server**: Avoids the problem entirely
2. **Relay with reconnection**: When host drops, a new host creates a relay and all clients rejoin
3. **State checkpointing**: Periodically save game state so a new host can restore it
4. **Use a different transport**: Some community transports support migration

For most games, **dedicated server** or **graceful session end on host disconnect** is the pragmatic choice.
