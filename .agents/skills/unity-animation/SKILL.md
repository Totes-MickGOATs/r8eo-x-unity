---
name: unity-animation
description: Unity Animation
---


# Unity Animation

Use this skill when working with Animator Controllers, Blend Trees, animation layers, IK, root motion, Timeline, or procedural tweening in Unity.

## Animation Clips

Animation clips store keyframe data for transforms, blendshapes, material properties, and more.

### Creating Clips

- **Animation window** (Ctrl+6): Record mode, add keyframes manually
- **Import from FBX/glTF**: Clips extracted from model files
- **Procedural**: Created at runtime via `AnimationClip` API

### Key Properties

| Property | Purpose |
|----------|---------|
| Loop Time | Whether clip loops |
| Root Transform Position/Rotation | Bake Into Pose for in-place animations |
| Curves | Custom float parameters driven by the clip |
| Events | Method calls at specific frames |

```csharp
// Runtime clip creation
AnimationClip clip = new AnimationClip();
clip.SetCurve("", typeof(Transform), "localPosition.x",
    AnimationCurve.Linear(0f, 0f, 1f, 5f));
```

## Animator Override Controller

Reuse the same state machine with different clips. Ideal for character variants:

```csharp
// Runtime clip swapping
AnimatorOverrideController overrideController =
    new AnimatorOverrideController(_animator.runtimeAnimatorController);

overrideController["DefaultIdle"] = customIdleClip;
overrideController["DefaultRun"] = customRunClip;
_animator.runtimeAnimatorController = overrideController;
```

Create via **Assets > Create > Animator Override Controller**, assign the original controller, then drag replacement clips.

## Animation Rigging Package

Advanced runtime rigging constraints:

```json
"com.unity.animation.rigging": "1.3.0"
```

### Key Constraints

| Constraint | Purpose |
|-----------|---------|
| Multi-Aim Constraint | Aim a bone at a target (head tracking, turret) |
| Two Bone IK Constraint | Classic IK chain (hand to grip, foot to ground) |
| Damped Transform | Smooth follow (tail, chain, hair) |
| Override Transform | Snap a bone to a target (hand on weapon grip) |
| Multi-Parent Constraint | Switch parent at runtime (pick up / put down) |

```
// Hierarchy for Rig setup:
Character
  +-- Rig (Rig component, RigBuilder reference)
  |     +-- HeadAimConstraint (Multi-Aim Constraint)
  |     +-- LeftHandIK (Two Bone IK Constraint)
  |     +-- RightHandGrip (Override Transform)
  +-- Model (Animator + Rig Builder)
```

## Procedural Animation (DOTween/LeanTween)

For UI animations, VFX, and procedural object movement:

```csharp
// DOTween examples (com.demigiant.dotween)
transform.DOMove(targetPos, 1f).SetEase(Ease.OutBounce);
transform.DORotate(new Vector3(0, 360, 0), 2f, RotateMode.FastBeyond360);
transform.DOScale(1.5f, 0.5f).SetLoops(-1, LoopType.Yoyo);

// Sequences
Sequence seq = DOTween.Sequence();
seq.Append(transform.DOMove(posA, 0.5f));
seq.Append(transform.DOMove(posB, 0.5f));
seq.Join(transform.DORotate(Vector3.up * 180, 0.5f)); // Parallel with previous
seq.AppendCallback(() => Debug.Log("Sequence complete"));

// UI
canvasGroup.DOFade(0f, 0.3f);
rectTransform.DOAnchorPos(new Vector2(100, 0), 0.5f);
```

### Easing Functions

Common choices: `Ease.OutQuad` (decelerate), `Ease.InOutCubic` (smooth), `Ease.OutBack` (overshoot), `Ease.OutElastic` (bounce).

## Humanoid vs Generic Rig

| | Humanoid | Generic |
|-|----------|---------|
| Retargeting | Yes -- share animations across models | No -- animations are model-specific |
| IK | Built-in IK goals (hands, feet, look-at) | Requires Animation Rigging |
| Performance | Slightly more expensive (muscle space) | Faster |
| Use For | Characters (bipedal) | Props, creatures, vehicles |

### Avatar Setup

For Humanoid rigs, configure the Avatar in the model import settings:
1. Set Animation Type to **Humanoid**
2. Click **Configure** to verify bone mapping
3. Resolve any red/missing bones
4. Set muscle limits if needed (e.g., restrict head rotation range)



## Topic Pages

- [Animator Controller](skill-animator-controller.md)
- [Timeline](skill-timeline.md)
- [Performance](skill-performance.md)
- [Animation Events](skill-animation-events.md)

