# Physics Conformance Audit -- Full Check Catalogue

93 checks across 12 categories validating that the RC car physics simulation produces physically correct results.

**Checks G–L:** See [audit-physics-checks-g-l.md](./audit-physics-checks-g-l.md) (Grip & Tire, Drivetrain, Air Physics, Temporal, ESC/Motor, Compound Scenarios)

---

## Tolerance Measurement

```
tolerance = |actual - expected| / |expected| x 100%
```

When `expected = 0`: tolerance is 0% if `actual = 0`, otherwise 100%.

## Tolerance Tiers

| Tier | Threshold | Weight |
|------|-----------|--------|
| Excellent | < 1% | 1.0 |
| Good | < 5% | 0.8 |
| Noticeable | < 15% | 0.5 |
| Poor | < 50% | 0.2 |
| Broken | >= 50% | 0.0 |

A check **passes** if tolerance < 50% (below Poor threshold).

## Conformance Score

Weighted average of all check weights divided by number of checks. Range: 0.0 (all broken) to 1.0 (all excellent).

## Integration Points

Each check calls `ConformanceRecorder.Record(category, checkId, checkName, expected, actual, metadata)`.

Checks are grouped into test classes per category, run via Unity Test Framework (EditMode for pure math, PlayMode for runtime behaviour).

---

## A. Geometric Fidelity (10 checks)

| ID | Name | Analytical Prediction | Violation Looks Like |
|----|------|----------------------|---------------------|
| A1 | Wheel contact patch vs raycast hit | contact patch center = hit point + normal x radius | Wheel visually floating or sunk |
| A2 | Wheel visual position vs physics position | visual = chassis + suspension offset | Wheel disconnected from chassis |
| A3 | Wheel rotation vs distance traveled | delta_theta = d/r, one rotation = 2*pi*0.166 = 1.043m | Wheel spinning too fast or too slow |
| A4 | Steering angle visual vs physics | visual yaw = Ackermann-corrected angle | Front wheels pointing wrong direction |
| A5 | Chassis ride height vs suspension state | height = rest - sum(compression)/4 | Car floating or buried |
| A6 | Suspension travel bounds | spring length in [0.032, 0.28]m | Suspension clips through chassis |
| A7 | Wheel center above terrain | center.y >= terrain + radius | Wheel below ground |
| A8 | Four-wheel coplanarity at rest on flat | all contact points within epsilon of plane | Car rocks on flat ground |
| A9 | Wheelbase consistency | front-rear = 0.28m +/- epsilon always | Chassis stretching or compressing |
| A10 | Track width consistency | left-right distance constant +/- epsilon | Axle width changing |

---

## B. Force Fidelity (12 checks)

| ID | Name | Analytical Prediction | Violation Looks Like |
|----|------|----------------------|---------------------|
| B1 | Suspension force direction | along axis, away from ground | Car pulled into ground |
| B2 | Suspension force sign | F >= 0 always | Suspension creates suction |
| B3 | Lateral grip opposes lateral velocity | sign(F) = -sign(v) | Car accelerates sideways |
| B4 | Longitudinal friction opposes motion | sign(F) = -sign(v) when braking | Car accelerates when braking |
| B5 | Motor force along wheel forward | F parallel to wheel.forward | Motor pushes sideways |
| B6 | Normal force sum at rest | sum(F) = mg = 14.715N | Car floats or sinks at rest |
| B7 | Per-wheel normal at rest | F approx mg/4 = 3.679N (+/-5%) | Uneven weight on flat ground |
| B8 | No forces exceed physical bounds | F_max approx mu*m*g approx 10.3N lateral | Impossible forces |
| B9 | No NaN or Infinity | all components finite | Simulation explosion |
| B10 | Force application at contact point | not wheel center | Torque artifacts |
| B11 | Net force = 0 at steady state | sum(F) = 0 when dv/dt = 0 | Car accelerates at constant speed |
| B12 | Force proportional to input | 50% throttle approx 50% force | Non-linear throttle response |

---

## C. Conservation Laws (6 checks)

| ID | Name | Analytical Prediction | Violation Looks Like |
|----|------|----------------------|---------------------|
| C1 | Differential force conservation | left + right = total | Force created or destroyed in diff |
| C2 | AWD center diff conservation | front + rear = total | Force created or destroyed |
| C3 | Kinetic energy accounting | delta_KE = work_motor - work_friction - work_drag | Energy appears from nowhere |
| C4 | Angular momentum of wheels | I*omega changes only from torques | Wheel speed jumps without torque |
| C5 | Gravity work on slopes | delta_PE + delta_KE = work_friction | Free energy on hills |
| C6 | Braking energy dissipation | KE_initial - KE_final = integral(F_brake * ds) | Car stops too fast or too slow |

---

## D. Kinematic Consistency (8 checks)

| ID | Name | Analytical Prediction | Violation Looks Like |
|----|------|----------------------|---------------------|
| D1 | Wheel RPM matches ground speed | RPM = v/(2*pi*0.166) | Wheels spinning at wrong speed |
| D2 | Slip ratio range | normal: 0-0.15, braking: 0.15-1.0 | Impossible slip values |
| D3 | Velocity = integral of acceleration dt | matches over timestep | Velocity jumps |
| D4 | Position = integral of velocity dt | matches over timestep | Teleportation |
| D5 | Turning radius matches steering | R = 0.28/tan(delta) | Wrong turning circle |
| D6 | Centripetal acceleration | a_c = v^2/R | Car turns too tight or too wide |
| D7 | Braking distance | d = v^2/(2*mu*g) | Stops in wrong distance |
| D8 | Free-fall acceleration | a = 9.81 m/s^2 airborne | Wrong gravity |

---

## E. Contact & Collision (6 checks)

| ID | Name | Analytical Prediction | Violation Looks Like |
|----|------|----------------------|---------------------|
| E1 | Raycast normal matches terrain | hit.normal approx surface normal | Forces applied in wrong direction |
| E2 | Contact point on surface | within 0.001m | Floating contact point |
| E3 | All 4 wheels grounded on flat | all raycasts hit at rest | Wheel floating on flat ground |
| E4 | Correct surface type | matches terrain material | Wrong grip on visible surface |
| E5 | No terrain penetration | chassis above terrain | Car clips through ground |
| E6 | Slope load redistribution | front bears more on downslope | Equal weight distribution on slopes |

---

## F. Suspension Specific (9 checks)

| ID | Name | Analytical Prediction | Violation Looks Like |
|----|------|----------------------|---------------------|
| F1 | Hooke's law compliance | F = k*compression + c*velocity | Non-linear spring where linear expected |
| F2 | Spring length bounds | in [0.032, 0.28]m | Spring clips through limits |
| F3 | Compression ratio bounds | in [0.0, 1.0] | Impossible compression values |
| F4 | Damping opposes velocity | sign(F_damp) = -sign(v_spring) | Damping amplifies oscillation |
| F5 | Bump stop engagement | at length <= 0.032m | No bump stop, chassis hits ground |
| F6 | Rest state equilibrium | F = mg/4 per wheel | Bouncing forever at rest |
| F7 | Oscillation decay | amplitude decreases each cycle | Oscillation grows or never decays |
| F8 | Landing damping | no spike > plausible bounds | Impossible force spikes on landing |
| F9 | Compression matches chassis | delta_compression approx delta_height/cos(angle) | Suspension disconnected from body |
