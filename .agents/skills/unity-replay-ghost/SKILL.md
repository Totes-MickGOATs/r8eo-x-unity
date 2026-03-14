# Unity Replay & Ghost System for Racing

> Lazy-loaded reference for recording, storing, and playing back vehicle state for ghost cars and replay systems.

---

## Architecture Decision: State Recording

**Use state recording, NOT input recording.**

| Approach | Determinism Required | PhysX Compatible | Recommended |
|----------|---------------------|------------------|-------------|
| Input recording | Yes — identical physics each playback | No — PhysX is non-deterministic across frames/platforms | NO |
| State recording | No — replays actual positions | Yes — records what happened, not what caused it | YES |

PhysX (Unity's physics engine) is not deterministic: floating-point order-of-operations varies by frame timing, platform, and even CPU load. Input replay will diverge within seconds. Always record transform state.

---

## Recording Format

### Frame Structure

Record at `FixedUpdate` rate (50Hz default):

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GhostFrame
{
    public float time;           // 4 bytes — elapsed race time
    public Vector3 position;     // 12 bytes — world position
    public Quaternion rotation;  // 16 bytes — world rotation
    public Vector3 velocity;     // 12 bytes — for interpolation quality
    public float steerAngle;     // 4 bytes — front wheel visual angle
    // Total: 48 bytes per frame
}
```

### Raw Data Size

- 50 frames/sec x 180 sec (3-min lap) = 9,000 frames
- 48 bytes x 9,000 = 432 KB per lap (uncompressed)

---

## Storage Pipeline

### Write Path

```csharp
public void SaveGhost(string filePath, GhostFrame[] frames, GhostMetadata meta)
{
    using var fileStream = File.Create(filePath);

    // Write version header
    fileStream.WriteByte(GhostFileVersion);

    // Write metadata as JSON sidecar prefix
    byte[] metaBytes = JsonUtility.ToJson(meta).ToUtf8Bytes();
    using var writer = new BinaryWriter(fileStream);
    writer.Write(metaBytes.Length);
    writer.Write(metaBytes);

    // Compress frame data
    using var deflate = new DeflateStream(fileStream, CompressionLevel.Optimal);
    using var bw = new BinaryWriter(deflate);

    bw.Write(frames.Length);
    foreach (var f in frames)
    {
        bw.Write(f.time);
        bw.Write(f.position.x); bw.Write(f.position.y); bw.Write(f.position.z);
        bw.Write(f.rotation.x); bw.Write(f.rotation.y); bw.Write(f.rotation.z); bw.Write(f.rotation.w);
        bw.Write(f.velocity.x); bw.Write(f.velocity.y); bw.Write(f.velocity.z);
        bw.Write(f.steerAngle);
    }
}
```

### Metadata Sidecar

```csharp
[Serializable]
public struct GhostMetadata
{
    public string trackId;
    public string vehicleId;
    public float lapTime;
    public string playerName;
    public string dateRecorded; // ISO 8601
    public int frameCount;
    public float recordingHz;
}
```

### File Extension

Use `.ghost` for ghost files. Store in `Application.persistentDataPath/ghosts/`.

---

## Quantization

Reduce per-frame size from 48 bytes to ~14 bytes for storage efficiency.

### Quaternion: Smallest-Three Encoding (29 bits)

A unit quaternion has 4 components but only 3 degrees of freedom. Drop the largest component and reconstruct it:

1. Find the index of the largest absolute component (2 bits).
2. Encode the remaining 3 components as 9-bit signed integers (3 x 9 = 27 bits).
3. Total: 2 + 27 = 29 bits for full rotation.
4. Decode: reconstruct the dropped component from `sqrt(1 - x^2 - y^2 - z^2)`.

### Position: Bounded Encoding (50 bits)

For a 200x200m track with 20m height range:

| Axis | Range | Bits | Precision |
|------|-------|------|-----------|
| X | 0-200 m | 18 bits | ~0.76 mm |
| Y | -5 to 15 m | 14 bits | ~1.22 mm |
| Z | 0-200 m | 18 bits | ~0.76 mm |
| **Total** | | **50 bits** | Sub-millimeter |

### Quantized Frame Total

| Field | Bits | Bytes |
|-------|------|-------|
| Position | 50 | 6.25 |
| Rotation | 29 | 3.625 |
| Steer angle | 8 | 1 |
| Velocity magnitude | 16 | 2 |
| Padding/alignment | 9 | 1.125 |
| **Total** | **112** | **14 bytes** |

---

## Delta Encoding

Further reduce size by encoding differences between frames:

### Strategy

- **Absolute keyframe** every 10 frames (full quantized frame).
- **Delta frames** between keyframes: signed difference from previous frame.
- Position deltas at 50Hz are tiny (vehicle moves <1m per frame at 50 m/s) — 8-12 bits per axis.
- Rotation deltas are even smaller — 6-8 bits per component.

### Delta Frame Size

- Position delta: 3 x 10 bits = 30 bits
- Rotation delta: 3 x 7 bits = 21 bits
- Steer delta: 6 bits
- Velocity delta: 10 bits
- Total delta: ~67 bits = ~8.4 bytes

### Compression Pipeline Summary

| Stage | Size (3-min, 50Hz) | Ratio |
|-------|---------------------|-------|
| Raw (48 bytes/frame) | 432 KB | 1.00x |
| Quantized (14 bytes/frame) | 126 KB | 0.29x |
| Delta encoded | ~85 KB | 0.20x |
| DeflateStream compressed | ~75 KB | 0.17x |

---

## Ghost Playback

### Component Architecture

The ghost car is a visual-only entity with no physics simulation:

```
GhostCar (GameObject)
  ├── MeshRenderer (vehicle body, transparent material)
  ├── GhostCarPlayback (MonoBehaviour — drives transform)
  ├── WheelVisuals (4x wheel mesh transforms, no WheelColliders)
  └── NO Rigidbody, NO Colliders
```

### Transform Interpolation

```csharp
public class GhostCarPlayback : MonoBehaviour
{
    private GhostFrame[] _frames;
    private float _playbackTime;
    private int _currentIndex;

    void Update()
    {
        _playbackTime += Time.deltaTime;

        // Binary search for bracketing frames
        int idx = FindFrameIndex(_playbackTime);
        if (idx >= _frames.Length - 1) return;

        GhostFrame a = _frames[idx];
        GhostFrame b = _frames[idx + 1];
        float t = (_playbackTime - a.time) / (b.time - a.time);

        // Hermite spline for position (uses velocity for tangents)
        transform.position = HermiteInterpolate(
            a.position, a.velocity,
            b.position, b.velocity, t);

        // Slerp for rotation
        transform.rotation = Quaternion.Slerp(a.rotation, b.rotation, t);
    }

    private static Vector3 HermiteInterpolate(
        Vector3 p0, Vector3 v0, Vector3 p1, Vector3 v1, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        float h00 = 2f * t3 - 3f * t2 + 1f;
        float h10 = t3 - 2f * t2 + t;
        float h01 = -2f * t3 + 3f * t2;
        float h11 = t3 - t2;
        float dt = 1f / 50f; // frame interval
        return h00 * p0 + h10 * dt * v0 + h01 * p1 + h11 * dt * v1;
    }

    private int FindFrameIndex(float time)
    {
        // Binary search into sorted frame array
        int lo = 0, hi = _frames.Length - 1;
        while (lo < hi - 1)
        {
            int mid = (lo + hi) / 2;
            if (_frames[mid].time <= time) lo = mid;
            else hi = mid;
        }
        return lo;
    }
}
```

### Ghost Material

- **Rendering mode:** Transparent or Fade.
- **Alpha:** 30-50% opacity. Enough to see the ghost car, transparent enough to not obscure the track.
- **No shadow casting:** Disable `MeshRenderer.shadowCastingMode`.
- **No colliders:** Ghost car must not interact with physics. No `Collider` components.
- **Distinct color:** Tint the ghost material (e.g., blue-white) to distinguish from real vehicles.

---

## Replay Camera System

### Cinemachine Virtual Cameras

Use Cinemachine virtual cameras with priority-based switching for replay mode:

| Camera | Cinemachine Body/Aim | Trigger |
|--------|----------------------|---------|
| **Chase** | Transposer + Composer | Default follow camera |
| **Bumper** | Hard lock to vehicle front | Player toggle |
| **Trackside** | Fixed position, Composer aim at vehicle | Proximity trigger zones |
| **Dolly** | Dolly Cart on SplinePath | Placed along scenic sections |
| **Free** | FreeLook (orbit) | Player right-stick control |
| **Overhead** | Fixed high position, look-at vehicle | Player toggle |

### Priority Switching

```csharp
// In replay mode, cycle cameras with button press
public void NextCamera()
{
    _activeCameraIndex = (_activeCameraIndex + 1) % _replayCameras.Length;
    foreach (var cam in _replayCameras)
        cam.Priority = 0;
    _replayCameras[_activeCameraIndex].Priority = 10;
}
```

### Trackside Camera Placement

Place `CinemachineVirtualCamera` objects along the track at interesting vantage points (corners, jumps, straights). Use trigger colliders around them — when the tracked vehicle enters the trigger, boost that camera's priority.

---

## Time Scrubbing

### Manual Playback Control

```csharp
// Time scrubbing — no Physics.Simulate needed
public float PlaybackTime
{
    get => _playbackTime;
    set
    {
        _playbackTime = Mathf.Clamp(value, 0f, _totalDuration);
        // Binary search snaps to correct frame instantly
        _currentIndex = FindFrameIndex(_playbackTime);
    }
}

// Playback speed control
public float PlaybackSpeed { get; set; } = 1.0f; // 0 = paused, 0.5 = half, 2 = double

void Update()
{
    _playbackTime += Time.deltaTime * PlaybackSpeed;
    // ... interpolation as above
}
```

### UI Controls

- Slider bar mapped to `PlaybackTime` (0 to `_totalDuration`)
- Play/Pause button toggles `PlaybackSpeed` between 0 and 1
- Speed buttons: 0.25x, 0.5x, 1x, 2x, 4x
- Frame-step buttons: advance `PlaybackTime` by `1/recordingHz`

---

## Async I/O

### Background Thread Save/Load

```csharp
public async Awaitable<GhostFrame[]> LoadGhostAsync(string filePath)
{
    // Move to background thread for file I/O
    await Awaitable.BackgroundThreadAsync();

    byte[] fileBytes = File.ReadAllBytes(filePath);
    // ... decompress and deserialize (same as sync, but off main thread)
    GhostFrame[] frames = DeserializeFrames(fileBytes);

    // Return to main thread before returning
    await Awaitable.MainThreadAsync();
    return frames;
}
```

- **Save:** Write ghost file on background thread immediately after lap completion.
- **Load:** Read and decompress on background thread during scene load or pre-race.
- **No frame drops:** File I/O never blocks the main thread.

---

## File Versioning

### Version Header

Every `.ghost` file starts with a version byte:

```
[version: 1 byte][metadata length: 4 bytes][metadata: N bytes][compressed frames]
```

### Migration on Load

```csharp
public GhostFrame[] LoadWithMigration(byte[] data)
{
    byte version = data[0];
    return version switch
    {
        1 => LoadV1(data),
        2 => LoadV2(data),
        _ => throw new InvalidDataException($"Unknown ghost version: {version}")
    };
}
```

- When adding fields to `GhostFrame`, bump the version number.
- Write a migration function that reads old format and fills new fields with defaults.
- Never break backward compatibility — old ghost files must always load.

---

## Anti-Cheat Validation

### Local Validation

| Check | Threshold | Action |
|-------|-----------|--------|
| Max position delta between frames | > 5m at 50Hz (~250 m/s) | Reject ghost as invalid |
| Ghost duration vs lap time | Differ by > 1 second | Reject ghost |
| Frame count vs expected | Off by > 10% from `duration * Hz` | Reject ghost |
| Position out of track bounds | Beyond terrain extents + margin | Reject ghost |

### Online Validation

- **Session nonce:** Generate a random nonce at race start, embed in ghost metadata. Server validates nonce was issued for that session.
- **Server-side replay:** For leaderboard-critical ghosts, server can replay the ghost data and validate positions against track geometry.
- **Signature:** HMAC-SHA256 of frame data with a session-specific key. Prevents post-hoc frame editing.

---

## Networking: Ghost Sharing

### Architecture

```
Player records ghost → Save to local .ghost file
                     → Upload to CDN-backed blob storage (S3/CloudFront)
                     → Attach blob URL to Steam leaderboard entry as UGC
                     → Other players download ghost when viewing leaderboard
```

### Steam UGC Integration

1. Player achieves a leaderboard time.
2. Upload ghost file as Steam UGC item (`SteamUGC.CreateItem`).
3. Attach UGC handle to leaderboard entry (`SteamUserStats.AttachLeaderboardUGC`).
4. When another player views the leaderboard, download the UGC ghost file.
5. Load and play back the ghost car locally.

### CDN Alternative (Non-Steam)

- Upload ghost file to S3 or equivalent blob storage via signed URL.
- Store blob URL in leaderboard database entry.
- Client downloads ghost file on demand.
- Cache downloaded ghosts locally to avoid re-downloading.

---

## Ghost Car Component Architecture

Summary of the ghost car GameObject hierarchy:

```
GhostCar
  ├── Body
  │     ├── MeshFilter (vehicle body mesh)
  │     └── MeshRenderer (transparent ghost material, no shadows)
  ├── WheelFL / WheelFR / WheelRL / WheelRR
  │     ├── MeshFilter (wheel mesh)
  │     └── MeshRenderer (ghost material)
  ├── GhostCarPlayback (MonoBehaviour)
  │     ├── Drives transform.position and transform.rotation
  │     ├── Drives wheel visual rotation (spin + steer)
  │     └── Handles time scrubbing and playback speed
  └── NO Rigidbody
      NO Colliders
      NO WheelColliders
      NO AudioSource (ghost is silent)
```

### Wheel Visuals

- Front wheels rotate around Y-axis by `steerAngle` from ghost data.
- All wheels spin around X-axis proportional to velocity magnitude: `spinRate = velocity.magnitude / wheelRadius * Time.deltaTime`.
- Wheel transforms are updated by `GhostCarPlayback`, not by a physics system.
