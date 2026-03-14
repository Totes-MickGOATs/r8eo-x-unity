# Vehicle Physics Audit Results (2026-03-14)

Full audit of all vehicle subsystems comparing Unity implementation against Godot reference.

## CRITICAL Fixes Required

### C1: Grip curve starts at (0,0) — zero grip at zero slip
- **File:** `RaycastWheel.cs` line 52, `TractionConfig.cs` line 17
- **Problem:** At zero slip ratio, grip factor is 0. Car has no lateral resistance when barely sliding.
- **Fix:** Change first keyframe from `(0, 0)` to `(0, 0.3)` or similar baseline.

### C2: gripLoad excludes damping
- **File:** `SuspensionMath.cs` ComputeGripLoad, `RaycastWheel.cs` line 214
- **Problem:** Grip load uses raw spring force only, not the actual suspension force including damping.
- **Fix:** Compute gripLoad from `_suspensionForce` (damped) instead of raw spring force: `_gripLoad = Mathf.Clamp(_suspensionForce, 0f, _maxSpringForce)`

### C3: Longitudinal friction along CAR forward, not WHEEL forward
- **File:** `RaycastWheel.cs` line 252
- **Problem:** `_zForce = carRb.transform.forward * longForceMag` uses car body forward, not the steered wheel's forward.
- **Fix:** Change to `_zForce = transform.forward * longForceMag`

### C4: Ramp sliding fix — world-space component bug
- **File:** `RaycastWheel.cs` lines 255-259
- **Problem:** Subtracts world X/Z components from local-space force vectors. Fails when car is rotated.
- **Fix:** Apply cancellation as a proper vector: `Vector3 springHorizontal = new Vector3(_contactNormal.x * _suspensionForce, 0, _contactNormal.z * _suspensionForce); _xForce -= springHorizontal; // or add as separate counter-force`

### C5: Input — ghost brake from gamepad axis noise
- **Files:** `RCInput.cs` (detection, trigger reading), `InputManager.asset`
- **Problem:** CombinedTriggers on axis 2 reports non-zero on many gamepads. Single-frame detection locks mode. No startup grace period.
- **Fix:** Add 60-frame grace period, require 5+ consecutive frames of strong input to lock mode, increase trigger deadzone to 0.20.

### C6: Coast drag sets IsBraking=true
- **File:** `RCCar.cs` line 394-396, line 235
- **Problem:** `CurrentBrakeForce = _coastDrag` when coasting → `IsBraking = true` → traction jumps from 0.10 to 0.50.
- **Fix:** Set `CurrentBrakeForce = 0` during coast. Apply coast drag as a separate force or use a dedicated `IsCoasting` flag.

## MODERATE Fixes

### M1: Speed threshold dead zone (0.1 m/s)
- Lower `k_MinSpeedForGrip` to 0.01 or ramp-in lateral force near zero speed.

### M2: Dual "Horizontal" axis definitions
- Create dedicated `GamepadSteerX` axis for joystick, don't reuse keyboard "Horizontal".

### M3: No startup grace period for input
- Skip input polling for first 3+ frames (`if (Time.frameCount < 3) return`).

### M4: Steering deadzone too small
- Increase `_steerDeadzone` from 0.1 to 0.2, apply with symmetric remapping.

### M5: Engine cutoff uses velocity magnitude
- Change `_rb.velocity.magnitude >= _maxSpeed` to `Mathf.Abs(ForwardSpeed) >= _maxSpeed`.

### M6: Reverse ESC too easy to trigger
- Require brake-release-brake sequence, or add minimum brake threshold (`brakeIn > 0.1`).

### M7: Airborne-to-ground damping spike
- **Root cause of bouncing.** When transitioning from airborne to grounded, `_prevSpringLen = 0.28m` (full droop) creates a 66N damping spike.
- **Fix:** Set `_prevSpringLen = _springLen` on first ground contact after airborne.

### M8: No-tension clamp truncates rebound damping
- Consider split clamp: allow some rebound damping even when spring force is zero.
