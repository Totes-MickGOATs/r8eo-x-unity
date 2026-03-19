---
name: vehicle-config-so
description: Creates or modifies ScriptableObject configs in Assets/Scripts/Vehicle/Config/ using the [CreateAssetMenu(menuName = "R8EOX/...")] pattern with [SerializeField] private + public readonly property style. Use when adding tunable motor, suspension, traction, or wheel inertia parameters. Trigger phrases: 'add config', 'new preset', 'tuning ScriptableObject', 'motor config', 'suspension settings', 'wheel inertia config'. Key capabilities: [Header]/[Tooltip] field annotations, AnimationCurve fields with inline keyframes, null-coalescing fallback constants in MonoBehaviours. Do NOT use for runtime per-frame state, temporary values, or non-tuning data.
---
# Vehicle Config ScriptableObject

## Critical

- **Never store per-frame or runtime-mutated state** in a config SO — these are tuning-time constants only.
- **All fields must be `[SerializeField] private`** with a matching `public T Name => _name;` property — never `public` fields.
- **No bare numeric literals** — every default value must be traceable to the physics spec in `.ai/knowledge/architecture/adr-001-physics-model.md`.
- **Namespace:** always `R8EOX.Vehicle.Config`.
- **MonoBehaviour references** must include `const` fallback defaults and null-coalescing properties — the config field is optional by design.

## Instructions

### Step 1 — Create the config class

File: `Assets/Scripts/Vehicle/Config/<Name>Config.cs`

```csharp
using UnityEngine;

namespace R8EOX.Vehicle.Config
{
    [CreateAssetMenu(fileName = "New<Name>Config", menuName = "R8EOX/<Display Name>")]
    public class <Name>Config : ScriptableObject
    {
        [Header("<Group Label>")]
        [Tooltip("<Units and meaning, e.g. Spring stiffness in N/m>")]
        [SerializeField] private float _fieldName = 75f;

        // AnimationCurve fields: initialize inline with representative keyframes
        [Header("Response Curve")]
        [SerializeField] private AnimationCurve _gripCurve = new AnimationCurve(
            new Keyframe(0f,    0.3f),
            new Keyframe(0.15f, 0.8f),
            new Keyframe(0.4f,  1.0f),
            new Keyframe(1.0f,  0.7f)
        );

        /// <summary>Field name in units.</summary>
        public float FieldName    => _fieldName;
        public AnimationCurve GripCurve => _gripCurve;
    }
}
```

**Verify:** File compiles without errors (`/editor:compile-check`). `Assets > Create > R8EOX > <Display Name>` menu item appears.

### Step 2 — Follow existing naming conventions per domain

| Config | fileName | menuName | Primary fields |
|--------|----------|----------|----------------|
| Motor | `NewMotorPreset` | `R8EOX/Motor Preset` | `_engineForceMax`, `_maxSpeed`, `_brakeForce`, `_throttleRampUp` |
| Suspension | `NewSuspensionConfig` | `R8EOX/Suspension Config` | `_springStrength`, `_springDamping`, `_restDistance`, `_maxSpringForce` |
| Traction | `NewTractionConfig` | `R8EOX/Traction Config` | `_gripCoeff`, `_gripCurve`, `_zTraction` |
| Wheel Inertia | *(omit fileName)* | `R8EOX/Wheel Inertia Config` | `_wheelMoI`, `_gyroScale`, `_reactionScale` |

**Verify:** New config filename and menuName do not duplicate an existing entry in `Assets/Scripts/Vehicle/Config/`.

### Step 3 — Wire into a MonoBehaviour (using `RCAirPhysics.cs` as the reference)

```csharp
// ---- Config Reference ----
[Header("<Config Group>")] 
[Tooltip("<Config purpose>. Create via Assets > Create > R8EOX > <Display Name>")]
[SerializeField] private <Name>Config _<name>Config;

// ---- Fallback Defaults (used when no config asset assigned) ----
const float k_Default<Field> = <value>f;   // must match SO default

// ---- Properties (null-coalescing) ----
private float <Field> => _<name>Config != null ? _<name>Config.<Field> : k_Default<Field>;
```

**Verify:** MonoBehaviour compiles. In Play Mode with config field unassigned, `k_Default*` values drive behaviour identically to the SO defaults.

### Step 4 — Add to manifest

Add the new `.cs` file to `resources/manifests/R8EOX.Vehicle.Config.json` under `"files"`. Run `just validate-registry` — must exit 0.

### Step 5 — Write tests

Add an EditMode test in `Assets/Tests/EditMode/` that:
1. Instantiates the SO via `ScriptableObject.CreateInstance<NameConfig>()`
2. Asserts each property returns its hardcoded default
3. Verifies `AnimationCurve` has the expected number of keyframes

```csharp
[Test]
public void NameConfig_DefaultValues_MatchSpec()
{
    var cfg = ScriptableObject.CreateInstance<NameConfig>();
    Assert.AreEqual(75f, cfg.SpringStrength, 1e-4f);
    Object.DestroyImmediate(cfg);
}
```

Run `just test` → confirm GREEN before committing.

## Examples

**User says:** "Add a suspension config ScriptableObject with per-axle spring rates"

**Actions:**
1. Create `Assets/Scripts/Vehicle/Config/SuspensionConfig.cs` with `[CreateAssetMenu(fileName = "NewSuspensionConfig", menuName = "R8EOX/Suspension Config")]`
2. Add `[Header("Spring")] [Tooltip("Front spring stiffness N/m")] [SerializeField] private float _frontSpringStrength = 700f;` and rear counterpart
3. Expose `public float FrontSpringStrength => _frontSpringStrength;`
4. In `RCCar.cs`, add `[SerializeField] private SuspensionConfig _suspensionConfig;`, add `const float k_DefaultFrontSpring = 700f;`, add `private float FrontSpring => _suspensionConfig != null ? _suspensionConfig.FrontSpringStrength : k_DefaultFrontSpring;`
5. Update manifest, write test, run `just test` → GREEN, commit.

**Result:** Designer can create `Assets/Configs/B64Suspension.asset` from the menu and drag it into RCCar without changing code.

## Common Issues

**"Assets > Create > R8EOX > ... menu item missing"**
Check that `[CreateAssetMenu]` is present AND the class inherits `: ScriptableObject`. Recompile — menu only refreshes after domain reload.

**"NullReferenceException on config property access in play mode"**
Missing null-guard. Replace `_config.Field` with `_config != null ? _config.Field : k_DefaultField`.

**"CS0115: no suitable method to override" or namespace errors"**
Ensure `using UnityEngine;` is present and `namespace R8EOX.Vehicle.Config` wraps the class.

**`just validate-registry` fails with unknown file**
New `.cs` file is not listed in `resources/manifests/R8EOX.Vehicle.Config.json`. Add the relative path under `"files"` and re-run.

**AnimationCurve field shows flat line in Inspector**
Inline `new AnimationCurve(new Keyframe(...), ...)` only sets the default for new asset creation. If an existing asset was already created before the default changed, the stored asset value wins — recreate the asset.