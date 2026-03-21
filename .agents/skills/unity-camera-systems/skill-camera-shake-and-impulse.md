# Camera Shake and Impulse

> Part of the `unity-camera-systems` skill. See [SKILL.md](SKILL.md) for the overview.

## Camera Shake and Impulse

### Noise Profiles

Add `CinemachineBasicMultiChannelPerlin` to a camera for continuous shake:

```
CinemachineBasicMultiChannelPerlin:
  Noise Profile: 6D Shake       // Built-in preset
  Amplitude Gain: 0.5
  Frequency Gain: 1.0
```

Built-in profiles: Handheld Normal, Handheld Strong, 6D Shake, Wobble.

### Cinemachine Impulse (One-Shot Shake)

```csharp
// On the source of the shake (explosion, landing, hit):
[SerializeField] CinemachineImpulseSource _impulseSource;

void OnExplosion()
{
    _impulseSource.GenerateImpulse(); // Uses configured default velocity
}

void OnHeavyLanding(float impactForce)
{
    _impulseSource.GenerateImpulse(Vector3.down * impactForce);
}
```

On the camera, add `CinemachineImpulseListener`:

```
CinemachineImpulseListener:
  Channel Mask: 1               // Which impulse channels to react to
  Gain: 1.0
  Use 2D Distance: false
```

## Confiner and Dolly

### CinemachineConfiner

Keeps the camera within a bounding volume:

```
CinemachineConfiner:
  Bounding Volume: [Collider reference]    // Box, Sphere, or Composite
  Damping: 0.5
  Slow Speed: 0                            // Speed below which confiner relaxes
```

For 2D: use a PolygonCollider2D (on a non-trigger, non-physics layer).

### Dolly Track

Move the camera along a predefined path:

```
CinemachineCamera + CinemachineSplineDolly:
  Spline: [SplineContainer reference]
  Camera Position:
    Position Units: Normalized    // 0-1 along path
    Speed: 0                     // 0 = driven by code or automation
  Auto Dolly:
    Enabled: true
    Method: Nearest Point To Target  // Camera travels to closest point on path
```

```csharp
// Manual dolly control
CinemachineSplineDolly dolly = vcam.GetComponent<CinemachineSplineDolly>();
dolly.CameraPosition = Mathf.Lerp(dolly.CameraPosition, targetPosition, Time.deltaTime);
```

