# Unity Replay & Ghost System -- RC Racing

> **Scope:** State-based replay recording, ghost playback, compression, storage,
> Cinemachine replay cameras, time scrubbing, anti-cheat validation, and networking
> for an RC racing simulator built on Unity PhysX backend.

Use this skill when implementing replay recording, ghost car playback, lap ghost storage/sharing, or replay camera systems. Covers the full pipeline from state capture through compressed storage to interpolated playback with Cinemachine cameras.

---

## Table of Contents

1. [Why State Recording, Not Input Recording](#1-why-state-recording-not-input-recording)
2. [GhostFrame Struct](#2-ghostframe-struct)
3. [Recording Pipeline](#3-recording-pipeline)
4. [Compression Pipeline](#4-compression-pipeline)
5. [Storage Format](#5-storage-format)
6. [Ghost Playback](#6-ghost-playback)
7. [Ghost Visual Representation](#7-ghost-visual-representation)
8. [Cinemachine Replay Cameras](#8-cinemachine-replay-cameras)
9. [Time Scrubbing](#9-time-scrubbing)
10. [Async I/O](#10-async-io)
11. [File Versioning](#11-file-versioning)
12. [Anti-Cheat Validation](#12-anti-cheat-validation)
13. [Networking and Distribution](#13-networking-and-distribution)
14. [Performance Budget](#14-performance-budget)
15. [Testing Strategy](#15-testing-strategy)

---

## 1. Why State Recording, Not Input Recording

PhysX is **non-deterministic** across runs. Floating-point accumulation order varies
with frame timing, thread scheduling, and solver iteration counts. Replaying the
same input sequence produces divergent results within seconds -- unacceptable for a
ghost that must match the original lap exactly.

**State recording** captures the vehicle transform and key dynamic values each
tick. Playback reconstructs the visual path without re-simulating physics. This
trades storage size for correctness -- a worthwhile tradeoff given the compression
pipeline below.

| Approach | Determinism Required | PhysX Compatible | Recommended |
|----------|---------------------|------------------|-------------|
| Input recording | Yes -- identical physics each playback | No -- PhysX non-deterministic | NO |
| State recording | No -- replays actual positions | Yes -- records what happened | YES |

### What We Do NOT Record

- Raw input axes (non-deterministic replay)
- Full PhysX solver state (massive, version-coupled)
- Other vehicles state (ghost is single-vehicle)
- Audio/VFX triggers (derived from playback state at runtime)

---

## 2. GhostFrame Struct

Each frame snapshot is a fixed-size, blittable struct for cache-friendly storage
and fast serialization.

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GhostFrame
{
    public float Time;            //  4 bytes -- elapsed since race start
    public Vector3 Position;      // 12 bytes -- world-space center of mass
    public Quaternion Rotation;   // 16 bytes -- world-space orientation
    public float Speed;           //  4 bytes -- longitudinal speed (m/s)
    public float SteerAngle;      //  4 bytes -- front wheel steer angle (radians)
    public float ThrottleBrake;   //  4 bytes -- [-1, 1] combined throttle/brake
    public uint Flags;            //  4 bytes -- bitfield: wheels grounded, boost, drift
}
// Total: 48 bytes per frame
```

### Flags Bitfield Layout

| Bits  | Field              | Description                          |
|-------|--------------------|--------------------------------------|
| 0-3   | WheelsGrounded     | One bit per wheel (FL, FR, RL, RR)   |
| 4     | IsBoosting         | Boost/nitro active                   |
| 5     | IsDrifting         | Drift state active                   |
| 6     | IsAirborne         | All four wheels off ground           |
| 7-15  | SurfaceType        | Current surface ID (dirt, asphalt)   |
| 16-31 | Reserved           | Future use (jumps, damage, etc.)     |

### Size at 50 Hz Recording Rate

| Duration | Frames | Raw Size |
|----------|--------|----------|
| 1 min    | 3,000  | 144 KB   |
| 3 min    | 9,000  | 432 KB   |
| 5 min    | 15,000 | 720 KB   |

50 Hz is sufficient for smooth RC car playback. RC vehicles change direction
rapidly but 20 ms resolution captures all meaningful motion when combined with
Hermite interpolation on playback.

---

## 3. Recording Pipeline

### Recorder Component

```csharp
public class GhostRecorder : MonoBehaviour
{
    private const float RecordInterval = 1f / 50f; // 50 Hz
    private float _timeSinceLastRecord;
    private readonly List<GhostFrame> _frames = new(16_000);

    private void FixedUpdate()
    {
        _timeSinceLastRecord += Time.fixedDeltaTime;
        if (_timeSinceLastRecord < RecordInterval) return;
        _timeSinceLastRecord -= RecordInterval;

        _frames.Add(CaptureFrame());
    }

    private GhostFrame CaptureFrame()
    {
        var rb = _rigidbody;
        return new GhostFrame
        {
            Time       = _raceTimer.ElapsedTime,
            Position   = rb.position,
            Rotation   = rb.rotation,
            Speed      = Vector3.Dot(rb.linearVelocity, transform.forward),
            SteerAngle = _steering.CurrentAngle,
            ThrottleBrake = _motor.ThrottleBrakeAxis,
            Flags      = PackFlags()
        };
    }
}
```

### Key Design Decisions

- **FixedUpdate, not Update:** Consistent intervals independent of frame rate.
- **Pre-allocated list:** `new List<GhostFrame>(16_000)` avoids GC during recording.
  16,000 frames covers 5+ minutes at 50 Hz.
- **Accumulator pattern:** `_timeSinceLastRecord` handles FixedUpdate rates that
  do not evenly divide 50 Hz (e.g., 0.02s FixedUpdate = 50 Hz, perfect match;
  0.016s = every ~1.25 calls).
- **Record in FixedUpdate, not LateUpdate:** Captures physics state at the moment
  it is computed, not after rendering interpolation.

---

## 4. Compression Pipeline

Three-stage pipeline: quantization, delta encoding, DEFLATE compression.

### Stage 1: Quantization

Reduce floating-point precision where full 32-bit range is unnecessary.

| Field        | Raw      | Quantized             | Savings |
|--------------|----------|-----------------------|---------|
| Position     | 3x float | 3x float (keep full)  | 0%      |
| Rotation     | 4x float | Smallest-three uint32 | 25%     |
| Speed        | float    | int16 (x100)          | 50%     |
| SteerAngle   | float    | int16 (x10000)        | 50%     |
| ThrottleBrake| float    | int8 (x127)           | 75%     |
| Time         | float    | uint16 delta (ms)     | 50%     |

**Smallest-three quaternion encoding:** Drop the largest component (2 bits to
identify which), encode the remaining three as 10-bit signed integers. Total:
32 bits instead of 128 bits. Reconstruction: `w = sqrt(1 - x*x - y*y - z*z)`.

### Stage 2: Delta Encoding

After quantization, store the first frame as absolute, then store differences
from the previous frame for all subsequent frames. RC vehicles change position
smoothly, so deltas are small integers that compress extremely well.

```csharp
for (int i = frames.Length - 1; i > 0; i--)
{
    quantized[i].PositionX -= quantized[i - 1].PositionX;
    quantized[i].PositionY -= quantized[i - 1].PositionY;
    quantized[i].PositionZ -= quantized[i - 1].PositionZ;
    // ... repeat for all fields
}
```

### Stage 3: DEFLATE Compression

Feed the delta-encoded byte stream through `DeflateStream`. The small, repetitive
deltas yield excellent compression ratios.

### Compression Results (Typical 3-Minute Lap)

| Stage              | Size    | Ratio  |
|--------------------|---------|--------|
| Raw (48B x 9000)   | 432 KB  | 1.0x   |
| Quantized          | ~280 KB | 0.65x  |
| Delta-encoded      | ~280 KB | (same size, better entropy) |
| DEFLATE            | ~75 KB  | 0.17x  |

75 KB per ghost is small enough for local storage of hundreds of ghosts and
practical for network transfer.

---

## 5. Storage Format

### Binary File (.r8ghost)

```
+----------------------------------+
| Magic: "R8GH" (4 bytes)         |
| Version: int32                   |
| HeaderSize: int32                |
| FrameCount: int32                |
| RecordHz: uint16                 |
| Duration: float                  |
| Reserved: byte[18]               |  <-- 40-byte fixed header
+----------------------------------+
| Compressed frame data            |  <-- DeflateStream payload
| (quantized + delta-encoded)      |
+----------------------------------+
```

Written with `BinaryWriter` wrapping a `FileStream`:

```csharp
await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write,
    FileShare.None, bufferSize: 8192, useAsync: true);
using var bw = new BinaryWriter(fs);

// Header
bw.Write(Encoding.ASCII.GetBytes("R8GH"));
bw.Write(FileVersion);       // int32
bw.Write(HeaderSize);        // int32
bw.Write(frameCount);        // int32
bw.Write((ushort)recordHz);  // uint16
bw.Write(duration);          // float
bw.Write(new byte[18]);      // reserved

// Compressed payload
await using var deflate = new DeflateStream(fs, CompressionLevel.Optimal);
deflate.Write(deltaEncodedBytes);
```

### JSON Metadata Sidecar (.r8ghost.meta.json)

Stored alongside the binary file for tooling, leaderboard display, and debugging
without parsing the binary.

```json
{
    "version": 3,
    "trackId": "outpost_rally_v2",
    "vehicleId": "buggy_stock",
    "playerName": "SpeedDemon",
    "sessionNonce": "a1b2c3d4e5f6",
    "lapTime": 62.417,
    "recordedAt": "2026-03-14T10:30:00Z",
    "frameCount": 3121,
    "recordHz": 50,
    "compressionRatio": 0.17,
    "checksum": "sha256:abcdef1234567890...",
    "tags": ["personal_best", "weekly_challenge_12"]
}
```

### File Organization

```
<persistentDataPath>/
  ghosts/
    <trackId>/
      <vehicleId>/
        <timestamp>_<lapTime>.r8ghost
        <timestamp>_<lapTime>.r8ghost.meta.json
    leaderboard/
      <trackId>/
        rank_001.r8ghost
        rank_001.r8ghost.meta.json
```

---

## 6. Ghost Playback

### Playback Controller

The ghost vehicle is driven **kinematically** -- no Rigidbody, no physics simulation.
Position and rotation are set directly each frame.

```csharp
public class GhostPlayback : MonoBehaviour
{
    private GhostFrame[] _frames;
    private float _playbackTime;
    private int _currentIndex;
    private float _playbackSpeed = 1f;

    private void Update()
    {
        _playbackTime += Time.deltaTime * _playbackSpeed;
        _currentIndex = FindFrameIndex(_playbackTime);

        var (pos, rot) = InterpolateFrame(_currentIndex, _playbackTime);
        transform.SetPositionAndRotation(pos, rot);
    }
}
```

### Frame Lookup: Binary Search

Frames are sorted by time (recording order guarantees this). Use binary search
to find the bracketing frames for the current playback time.

```csharp
private int FindFrameIndex(float time)
{
    int lo = 0, hi = _frames.Length - 1;
    while (lo < hi)
    {
        int mid = lo + (hi - lo) / 2;
        if (_frames[mid].Time < time)
            lo = mid + 1;
        else
            hi = mid;
    }
    return Mathf.Max(0, lo - 1);
}
```

Binary search is O(log n) -- for 15,000 frames, that is ~14 comparisons per frame.
This is chosen over linear scan because time scrubbing (Section 9) can jump
anywhere in the timeline.

### Position Interpolation: Cubic Hermite

Linear interpolation produces visible "cornering" artifacts on RC cars that change
direction rapidly. Cubic Hermite interpolation uses the velocity (approximated
from neighboring frames) to produce smooth curves.

```csharp
private Vector3 HermitePosition(int index, float t)
{
    var p0 = _frames[index].Position;
    var p1 = _frames[index + 1].Position;
    float dt = _frames[index + 1].Time - _frames[index].Time;

    // Approximate tangents from neighboring frames
    var m0 = (index > 0)
        ? (p1 - _frames[index - 1].Position) / (2f * dt)
        : (p1 - p0) / dt;
    var m1 = (index + 2 < _frames.Length)
        ? (_frames[index + 2].Position - p0) / (2f * dt)
        : (p1 - p0) / dt;

    m0 *= dt;
    m1 *= dt;

    float t2 = t * t;
    float t3 = t2 * t;

    return (2f * t3 - 3f * t2 + 1f) * p0
         + (t3 - 2f * t2 + t) * m0
         + (-2f * t3 + 3f * t2) * p1
         + (t3 - t2) * m1;
}
```

### Rotation Interpolation: Quaternion.Slerp

Spherical linear interpolation between the two bracketing frames rotations.
Hermite is unnecessary for rotation -- Slerp handles the 50 Hz sample rate well
for RC car yaw/pitch/roll transitions.

```csharp
var rotation = Quaternion.Slerp(
    _frames[index].Rotation,
    _frames[index + 1].Rotation,
    t
);
```

### Derived State on Playback

Wheel spin, suspension compression, dust VFX, and engine audio are NOT stored
in the ghost. They are **derived at playback time** from the interpolated state:

- **Wheel spin:** Computed from `Speed` and wheel circumference
- **Steer visual:** Read directly from `SteerAngle`
- **Dust/dirt VFX:** Triggered when `SurfaceType` is dirt AND speed > threshold
- **Engine audio pitch:** Mapped from `Speed` and `ThrottleBrake`

This keeps GhostFrame small and avoids coupling the format to VFX/audio systems.

---

## 7. Ghost Visual Representation

### Material Setup

The ghost vehicle uses a **transparent override material** so the player can
distinguish it from their live car.

```csharp
public static Material CreateGhostMaterial(Material baseMaterial)
{
    var ghost = new Material(baseMaterial);
    ghost.SetFloat("_Surface", 1f);        // Transparent
    ghost.SetFloat("_Blend", 0f);          // Alpha blend
    ghost.SetFloat("_AlphaClip", 0f);      // No alpha clip
    ghost.color = new Color(
        ghost.color.r,
        ghost.color.g,
        ghost.color.b,
        0.35f  // 30-50% alpha range, 35% is a good default
    );
    ghost.renderQueue = (int)RenderQueue.Transparent;
    ghost.SetOverrideTag("RenderType", "Transparent");
    ghost.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    return ghost;
}
```

### Physics Stripping

The ghost vehicle must NOT interact with physics:

- **No Rigidbody** -- remove or disable on instantiation
- **No Colliders** -- disable all colliders on the ghost prefab
- **Layer assignment** -- put on a dedicated `Ghost` layer excluded from all
  collision matrix rows

```csharp
private void StripPhysics(GameObject ghostRoot)
{
    if (ghostRoot.TryGetComponent<Rigidbody>(out var rb))
        Destroy(rb);

    foreach (var col in ghostRoot.GetComponentsInChildren<Collider>())
        col.enabled = false;

    SetLayerRecursive(ghostRoot, LayerMask.NameToLayer("Ghost"));
}
```

### Visual Polish

- Slight color tint shift (toward blue/cyan) to reinforce "ghost" identity
- Optional pulsing alpha (sinusoidal, 0.25-0.45 range, 1 Hz)
- Disable shadow casting: `renderer.shadowCastingMode = ShadowCastingMode.Off`
- Ghost does not receive shadows either for a clean ethereal look

### Ghost Car Component Hierarchy

```
GhostCar (GameObject)
  +-- Body
  |     +-- MeshFilter (vehicle body mesh)
  |     +-- MeshRenderer (transparent ghost material, no shadows)
  +-- WheelFL / WheelFR / WheelRL / WheelRR
  |     +-- MeshFilter (wheel mesh)
  |     +-- MeshRenderer (ghost material)
  +-- GhostPlayback (MonoBehaviour)
  |     +-- Drives transform.position and transform.rotation
  |     +-- Drives wheel visual rotation (spin + steer)
  |     +-- Handles time scrubbing and playback speed
  +-- NO Rigidbody
      NO Colliders
      NO WheelColliders
      NO AudioSource (ghost is silent)
```

### Wheel Visuals

- Front wheels rotate around Y-axis by `SteerAngle` from ghost data.
- All wheels spin around X-axis proportional to speed: `spinRate = Speed / wheelRadius * Time.deltaTime`.
- Wheel transforms are updated by `GhostPlayback`, not by a physics system.

---

## 8. Cinemachine Replay Cameras

During replay playback, the player can switch between multiple camera angles.
Each is a `CinemachineCamera` (v3+) with different body/aim configurations.

### Camera Definitions

| Camera     | Body          | Aim              | Priority | Description                          |
|------------|---------------|------------------|----------|--------------------------------------|
| Chase      | Transposer    | Composer         | 10       | Behind-and-above, classic TV angle   |
| Bumper     | None (hard)   | None (hard)      | 10       | Mounted on front bumper, FPV feel    |
| Trackside  | Fixed rail    | Composer (track) | 10       | Stationary cameras at track edges    |
| Dolly      | Dolly Cart    | Composer         | 10       | Follows a CinemachinePath alongside  |
| Free       | Orbital       | Composer         | 10       | Player-controlled orbit around car   |
| Overhead   | Transposer    | Hard look-at     | 10       | Top-down bird eye view               |

### Priority Switching

All cameras share priority 10 (inactive). The active camera gets priority 20.
Cinemachine brain handles the blend transition.

```csharp
public class ReplayCameraDirector : MonoBehaviour
{
    [SerializeField] private CinemachineCamera[] _cameras;
    private int _activeCameraIndex;

    public void SwitchCamera(int index)
    {
        _cameras[_activeCameraIndex].Priority = 10;
        _activeCameraIndex = index;
        _cameras[_activeCameraIndex].Priority = 20;
    }

    public void NextCamera()
    {
        SwitchCamera((_activeCameraIndex + 1) % _cameras.Length);
    }
}
```

### Trackside Camera Placement

Trackside cameras are placed at key track positions (corners, jumps, straights).
Each has a trigger zone -- when the ghost enters the zone, that trackside camera
gets priority (if trackside mode is active).

```csharp
public class TracksideZone : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _camera;
    [SerializeField] private float _radius = 15f;

    // Ghost is on a non-colliding layer, so use distance check instead
    public bool IsVehicleInZone(Vector3 vehiclePos)
    {
        return Vector3.Distance(transform.position, vehiclePos) < _radius;
    }
}
```

### Dolly Camera Setup

A `CinemachineSmoothPath` runs parallel to the track. The dolly camera rides
this path, matching the ghost progress along the track spline. This produces
cinematic side-tracking shots.

### Free Camera Controls

In free camera mode, the player uses right stick (or mouse) to orbit around
the ghost vehicle. `CinemachineInputAxisController` handles input mapping.
The orbit radius clamps between 2m and 15m -- appropriate for 1:10 scale RC cars.

---

## 9. Time Scrubbing

### Manual Playback Time Control

Time scrubbing sets `_playbackTime` directly from a UI slider. The binary search
in `FindFrameIndex` handles arbitrary time jumps efficiently.

```csharp
public void SetPlaybackTime(float time)
{
    _playbackTime = Mathf.Clamp(time, 0f, _totalDuration);
    _currentIndex = FindFrameIndex(_playbackTime);
    UpdateGhostTransform();
}

public void SetPlaybackSpeed(float speed)
{
    _playbackSpeed = Mathf.Clamp(speed, -2f, 4f);
}
```

### Supported Playback Modes

| Mode          | Speed    | Description                      |
|---------------|----------|----------------------------------|
| Play          | 1.0x     | Normal speed forward             |
| Slow-mo       | 0.25x    | Quarter speed for analysis       |
| Fast-forward  | 2.0-4.0x | Skip through straightaways       |
| Reverse       | -1.0x    | Play backwards through frames    |
| Pause         | 0.0x     | Frozen, scrub with slider        |
| Frame-step    | N/A      | Advance/retreat one frame (+/-1) |

### Why NOT Physics.Simulate

`Physics.Simulate()` would require full PhysX state reconstruction at every scrub
point, is computationally expensive, and still suffers from non-determinism.
Since we record state, scrubbing is a simple array lookup + interpolation --
instant and deterministic.

### Reverse Playback

Reverse playback uses the same `FindFrameIndex` binary search. The interpolation
parameter `t` is computed identically -- the frames are traversed in descending
order naturally as `_playbackTime` decreases.

---

## 10. Async I/O

### Save (Background Thread)

Recording data is serialized on a background thread to avoid frame hitches at
race completion.

```csharp
public async Awaitable SaveGhostAsync(string path, GhostFrame[] frames)
{
    // Compression on background thread
    await Awaitable.BackgroundThreadAsync();

    byte[] compressed = CompressFrames(frames);
    byte[] header = BuildHeader(frames.Length, duration);

    await using var fs = new FileStream(path, FileMode.Create,
        FileAccess.Write, FileShare.None, 8192, useAsync: true);
    await fs.WriteAsync(header);
    await fs.WriteAsync(compressed);

    // Write metadata sidecar
    string metaJson = BuildMetadataJson(frames);
    await File.WriteAllTextAsync(path + ".meta.json", metaJson);

    // Return to main thread for any callbacks
    await Awaitable.MainThreadAsync();
}
```

### Load (Background Thread)

```csharp
public async Awaitable<GhostFrame[]> LoadGhostAsync(string path)
{
    await Awaitable.BackgroundThreadAsync();

    await using var fs = new FileStream(path, FileMode.Open,
        FileAccess.Read, FileShare.Read, 8192, useAsync: true);
    using var br = new BinaryReader(fs);

    var header = ReadHeader(br);
    ValidateHeader(header);

    byte[] compressedData = br.ReadBytes((int)(fs.Length - fs.Position));
    GhostFrame[] frames = DecompressFrames(compressedData, header.FrameCount);

    await Awaitable.MainThreadAsync();
    return frames;
}
```

### Threading Rules

- **Never touch Unity APIs on background threads.** Transform, GameObject, etc.
  must be accessed on the main thread only.
- **Compression/decompression** is pure byte manipulation -- safe for background.
- **File I/O** with `useAsync: true` avoids blocking the thread pool.
- Use `Awaitable.MainThreadAsync()` to return to the main thread before updating
  any MonoBehaviour state.

---

## 11. File Versioning

### Header Version Field

The version is stored as an `int32` immediately after the magic bytes. This enables
forward-compatible file reading with room for many future versions.

```csharp
private const int CurrentFileVersion = 1;

private void ValidateHeader(GhostHeader header)
{
    if (header.Magic != "R8GH")
        throw new InvalidDataException("Not a valid ghost file");

    if (header.Version > CurrentFileVersion)
        throw new InvalidDataException(
            $"Ghost file version {header.Version} is newer than supported {CurrentFileVersion}");

    // Older versions: apply migration if needed
    if (header.Version < CurrentFileVersion)
        MigrateHeader(ref header);
}
```

### Migration Strategy

| Version | Changes                              | Migration                    |
|---------|--------------------------------------|------------------------------|
| 1       | Initial format                       | N/A                          |
| 2       | (future) Add suspension compression  | Default to 0 for new fields  |
| 3       | (future) Higher precision rotation   | Re-quantize on load          |

- **Older files always loadable:** Missing fields get sensible defaults.
- **Newer files rejected with clear error:** Players told to update the game.
- **Never delete fields:** Mark deprecated, zero-fill, handle in migration.

---

## 12. Anti-Cheat Validation

Ghost files are untrusted data. Validate before displaying on leaderboards or
sharing with other players.

### Position Delta Validation

```csharp
private bool ValidatePositionDeltas(GhostFrame[] frames, float maxDeltaPerFrame)
{
    // maxDeltaPerFrame: max physically possible distance at top speed in 1/50s
    // For RC car at 30 m/s: 30 * 0.02 = 0.6m, with margin: 1.0m
    for (int i = 1; i < frames.Length; i++)
    {
        float delta = Vector3.Distance(frames[i].Position, frames[i - 1].Position);
        if (delta > maxDeltaPerFrame)
            return false; // Teleportation detected
    }
    return true;
}
```

### Duration Validation

```csharp
private bool ValidateDuration(GhostFrame[] frames, float reportedLapTime)
{
    float actualDuration = frames[^1].Time - frames[0].Time;
    float tolerance = 0.1f; // 100ms tolerance for frame timing
    return Mathf.Abs(actualDuration - reportedLapTime) < tolerance;
}
```

### Session Nonce

Each race session generates a random nonce stored in the metadata sidecar.
The server cross-references the nonce with the session that produced it to
verify the ghost was recorded during a legitimate race, not fabricated offline.

```csharp
// Generated at race start
string sessionNonce = Guid.NewGuid().ToString("N")[..12];
```

### Checksum Integrity

The metadata sidecar includes a SHA-256 hash of the binary ghost file. Before
accepting a ghost for leaderboard display, verify the hash matches.

```csharp
using var sha256 = SHA256.Create();
byte[] fileBytes = await File.ReadAllBytesAsync(ghostPath);
string hash = Convert.ToHexString(sha256.ComputeHash(fileBytes)).ToLowerInvariant();

if (hash != metadata.Checksum)
    throw new InvalidDataException("Ghost file checksum mismatch -- file corrupted or tampered");
```

### Validation Checklist

| Check                  | Rejects                              |
|------------------------|--------------------------------------|
| Position delta < 1.0m  | Speed hacks, teleportation           |
| Duration +/- 100ms     | Time manipulation                    |
| Frame count matches Hz | Frame injection/deletion             |
| Session nonce valid    | Offline fabrication                   |
| SHA-256 checksum       | Binary tampering post-recording      |
| Track bounds check     | Shortcut exploits (optional)         |

---

## 13. Networking and Distribution

### CDN Blob Storage

Ghost files are small enough (~75 KB) for direct CDN hosting. Architecture:

1. **Upload:** Client POST to API gateway -> validate -> store in blob storage (S3/GCS/Azure Blob)
2. **Download:** Client GET from CDN edge -> decompress locally
3. **Metadata:** Stored in database, not CDN -- enables queries by track, vehicle, lap time
4. **Caching:** CDN TTL 24h for leaderboard ghosts, 1h for recent uploads

### API Endpoints

```
POST   /api/ghosts/upload          -- Upload ghost + metadata
GET    /api/ghosts/{trackId}/top   -- Get top N ghost metadata for a track
GET    /api/ghosts/{id}/download   -- Download ghost binary from CDN
DELETE /api/ghosts/{id}            -- Remove own ghost (soft delete)
```

### Steam UGC Attachment

For Steam distribution, ghosts can be attached to Workshop items or bundled
with replay packs:

```csharp
// Steam UGC ghost attachment
var updateHandle = SteamUGC.StartItemUpdate(appId, publishedFileId);
SteamUGC.SetItemContent(updateHandle, ghostDirectory);
SteamUGC.SetItemTitle(updateHandle, $"Ghost: {trackName} - {lapTime:F3}s");
SteamUGC.SubmitItemUpdate(updateHandle, "New personal best ghost");
```

Alternatively, attach ghost data directly to Steam leaderboard entries:

```csharp
// Attach UGC handle to leaderboard entry
SteamUserStats.AttachLeaderboardUGC(leaderboardHandle, ugcHandle);

// Other players download when viewing leaderboard
SteamRemoteStorage.UGCDownload(ugcHandle, priority: 0);
```

### Bandwidth Budget

| Scenario             | Size   | Transfer Time (1 Mbps) |
|----------------------|--------|------------------------|
| Single ghost         | 75 KB  | 0.6s                   |
| Top 10 leaderboard   | 750 KB | 6.0s                   |
| Replay pack (50)     | 3.75 MB| 30s                    |

All comfortably within RC sim player expectations. Pre-download top-3 ghosts
per track on game launch for instant availability.

---

## 14. Performance Budget

### Recording (During Race)

| Operation           | Cost             | Frequency    |
|---------------------|------------------|-------------|
| Capture frame       | ~0.01 ms         | 50 Hz       |
| List.Add            | Amortized O(1)   | 50 Hz       |
| Total per frame     | < 0.02 ms        | Negligible  |

### Playback (During Replay)

| Operation           | Cost             | Frequency    |
|---------------------|------------------|-------------|
| Binary search       | ~0.001 ms        | Per frame    |
| Hermite interp      | ~0.005 ms        | Per frame    |
| Slerp               | ~0.002 ms        | Per frame    |
| SetPositionAndRot   | ~0.01 ms         | Per frame    |
| Total per frame     | < 0.02 ms        | Negligible   |

### Save/Load (One-Time)

| Operation           | Cost             | Thread       |
|---------------------|------------------|-------------|
| Compress 3min ghost | ~15 ms           | Background   |
| File write          | ~5 ms            | Background   |
| Decompress + load   | ~10 ms           | Background   |

Zero impact on gameplay -- all heavy work is off the main thread.

---

## 15. Testing Strategy

### Unit Tests (EditMode)

- **GhostFrame packing:** Verify struct size is exactly 48 bytes
- **Flags bitfield:** Pack/unpack round-trip for all flag combinations
- **Quantization:** Round-trip accuracy within tolerance for all fields
- **Delta encoding:** Encode then decode, verify exact reconstruction
- **Compression:** Round-trip through full pipeline, verify frame equality
- **Binary search:** Edge cases (first frame, last frame, exact match, between frames)
- **Hermite interpolation:** Verify smoothness at midpoints, exact at endpoints
- **Anti-cheat validation:** Known-good and known-bad frame sequences
- **File versioning:** Load older version files, verify migration

### Integration Tests (PlayMode)

- **Record + playback round-trip:** Record a ghost, save, load, play back, verify
  positions match within interpolation tolerance
- **Ghost material:** Verify transparency, no colliders, correct layer
- **Camera switching:** Verify priority changes propagate to Cinemachine brain
- **Time scrubbing:** Jump to arbitrary times, verify correct frame displayed
- **Async I/O:** Save and load without main thread stalls (frame time monitoring)

### Performance Tests

- **Recording overhead:** Measure frame time with/without recorder active
- **Playback overhead:** Measure frame time with ghost playback active
- **Compression speed:** Benchmark compress/decompress for target file sizes
- **Memory allocation:** Verify zero GC allocations during recording/playback
  (use `Unity.Profiling.ProfilerRecorder` or manual `GC.GetTotalMemory` checks)

---

## Related Skills

| Skill | When to Use |
|-------|-------------|
| **`unity-camera-systems`** | Cinemachine setup, virtual camera configuration, camera blending |
| **`unity-save-load`** | Serialization patterns, persistent data, file I/O |
| **`unity-performance-optimization`** | Async patterns, memory management, compression |
