# Animation Events

> Part of the `unity-animation` skill. See [SKILL.md](SKILL.md) for the overview.

## Animation Events

Call methods from specific frames in the animation:

1. Select the clip in the Animation window
2. Move the playhead to the desired frame
3. Click **Add Event** and select a method

```csharp
// Methods called by animation events (must be on same GameObject as Animator)
public class CombatAnimEvents : MonoBehaviour
{
    public void OnAttackHitFrame()
    {
        // Check for hits at the moment of impact
        Collider[] hits = Physics.OverlapSphere(
            _hitPoint.position, _hitRadius, _enemyLayer);
        foreach (var hit in hits)
            hit.GetComponent<IDamageable>()?.TakeDamage(_damage);
    }

    public void OnFootstep()
    {
        _audioSource.PlayOneShot(_footstepClips[Random.Range(0, _footstepClips.Length)]);
    }
}
```

