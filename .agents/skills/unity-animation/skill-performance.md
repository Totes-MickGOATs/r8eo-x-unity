# Performance

> Part of the `unity-animation` skill. See [SKILL.md](SKILL.md) for the overview.

## Performance

### Animator Culling Mode

| Mode | Behavior | Use For |
|------|----------|---------|
| Always Animate | Full update even when off-screen | Main character, audio-synced |
| Cull Update Transforms | Skip transform writes when invisible | Most NPCs |
| Cull Completely | Stop animation entirely when invisible | Background characters |

### Optimization Tips

- **Optimize Game Objects**: In model import, enables internal optimization of the bone hierarchy. Exposes only the bones you explicitly mark.
- **Write Defaults**: Disable per-state to prevent unexpected blending artifacts. Choose a consistent approach across the project (all on or all off).
- **Animator.keepAnimatorStateOnDisable**: Set to `true` if you need to preserve state when toggling the Animator.
- **State Machine Behaviours**: Use sparingly. `OnStateEnter`/`OnStateExit` run on the main thread and can cause GC if allocating.

```csharp
// State Machine Behaviour
public class AttackStateBehaviour : StateMachineBehaviour
{
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<CombatController>().OnAttackStart();
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<CombatController>().OnAttackEnd();
    }
}
```
