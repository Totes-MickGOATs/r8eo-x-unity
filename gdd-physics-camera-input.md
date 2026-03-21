# GDD — Car Physics, Camera & Input

Part of the [Game Design Document](./GDD.md).

---

## 4. Car Physics Design

### 4.1 Buggy Characteristics

| Property | 1/10th Scale | 1/8th Scale |
|----------|-------------|-------------|
| Mass (sim units) | ~1.5 kg | ~3.5 kg |
| Wheelbase | Short (twitchy) | Medium |
| Power-to-weight | Very high | High |
| Top speed | ~70 km/h real | ~90 km/h real |
| Suspension travel | Long, soft | Long, moderate |
| Default grip | Low-medium | Medium |

### 4.2 Ground Physics

**Surface types and friction modifiers:**

| Surface | Friction | Notes |
|---------|----------|-------|
| Packed clay (dry) | 0.85 | Main racing surface; best grip |
| Loose dirt | 0.55 | Off-line, slippery, hard to recover |
| Wet clay | 0.65 | Reduced grip, more rotation |
| Rut / groove | 0.90 | Ideal racing line reward |
| Curb/edge | 0.40 | Destabilizing if hit wrong |

**Key behaviors:**
- Wheelspin on throttle application (especially out of corners)
- Snap oversteer if rear grip breaks suddenly (not gradual)
- Weight transfer visible in suspension animation and car body lean
- Differential behavior: open rear diff = inside wheel spins, car rotates

### 4.3 Air Physics (Key Differentiator)

When all four wheels leave the ground:

1. **Throttle -> Pitch control**
   - Blipping throttle UP: wheel spin inertia rotates nose DOWN (dive)
   - Lifting off throttle: wheel deceleration rotates nose UP (lift nose)

2. **Steering -> Roll control**
   - Steering input while airborne applies roll torque
   - Subtle effect — requires finesse

3. **Gyroscopic stabilization**
   - Spinning wheels resist sudden changes in orientation
   - Higher wheel speed = more gyro stability in air

4. **Landing**
   - Landing nose-first: compression -> possible bounce/flip
   - Landing flat: safe but may bottom out
   - Landing rear-first: wheelie, then nose slap -> loss of control
   - Ideal: slight nose-up or flat

---

## 6. Camera System

### 6.1 Chase Camera (Default)

- Cinemachine Virtual Camera — detached from car's rotation hierarchy
- Follows car position with smooth damped follow over world-space yaw
- Field of View: base + speed-scaled boost (`car.Rigidbody.linearVelocity.magnitude`)
- `camera_reset` action (C / right-stick click) snaps yaw back behind car

### 6.2 Camera Parameters

| Parameter | Value | Notes |
|-----------|-------|-------|
| Arm length | 2.5m | Distance behind car |
| Height offset | 0.8m | Above car pivot |
| Rotation lag | 0.15s | Smooth follow |
| Position lag | 0.08s | Tight, responsive |
| FOV speed boost | +15 deg max | At top speed |

### 6.3 Future Camera Modes

- **Cockpit** (driver's eye view, very low to ground)
- **TV cam** (fixed broadcast-style cameras, auto-switches)
- **Spectator** (free-roam for replay)

---

## 7. Input Design

All input abstracted to actions via the Unity Input System:

| Action | KB Default | Controller |
|--------|-----------|------------|
| `throttle` | W / Up Arrow | Right trigger |
| `brake_reverse` | S / Down Arrow | Left trigger |
| `steer_left` | A / Left Arrow | Left stick X |
| `steer_right` | D / Right Arrow | Left stick X |
| `air_pitch_up` | (throttle) | (throttle) |
| `air_pitch_down` | (brake) | (brake) |
| `camera_reset` | C | Right stick click |
| `pause` | Escape | Start |

**Note:** Air pitch reuses throttle/brake — same input, different physics effect when airborne.
