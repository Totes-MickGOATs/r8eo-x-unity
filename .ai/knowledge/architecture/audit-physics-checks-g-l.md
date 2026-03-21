# Physics Conformance Checks G–L

Part of the [Physics Conformance Audit](./audit-physics-conformance.md).

---

## G. Grip & Tire (8 checks)

| ID | Name | Analytical Prediction | Violation Looks Like |
|----|------|----------------------|---------------------|
| G1 | No grip when airborne | F = 0 at normal_load = 0 | Steering works in the air |
| G2 | Grip proportional to load | F_max = mu*F_normal | Grip ignores weight |
| G3 | Grip curve monotonic | increasing from baseline (0, 0.3) | Grip decreases with more slip |
| G4 | Surface modifier applied | grass < asphalt | Same grip on all surfaces |
| G5 | Lateral force limit | |F| <= mu*F_normal | Infinite cornering force |
| G6 | Combined grip circle | sqrt(F_lat^2+F_long^2) <= mu*F_normal | Combined force exceeds circle |
| G7 | Tire scrub during steering | slip angle increases with delta | No slip angle when turning |
| G8 | Load transfer in corners | outside wheels get more load | Equal load in corners |

---

## H. Drivetrain (6 checks)

| ID | Name | Analytical Prediction | Violation Looks Like |
|----|------|----------------------|---------------------|
| H1 | Open diff 50/50 | F_left = F_right on equal grip | Unequal torque on equal grip |
| H2 | Ball diff coupling | limited by preload + k*delta_omega | Diff locks completely or never |
| H3 | Spool locked | F_left = F_right always | Wheels spin independently with spool |
| H4 | One wheel airborne | all torque to grounded | Torque sent to spinning wheel |
| H5 | No negative torque positive throttle | F >= 0 when throttle > 0 | Motor brakes when accelerating |
| H6 | Torque = engine_force * throttle | linear mapping | Non-linear or wrong scale |

---

## I. Air Physics (6 checks)

| ID | Name | Analytical Prediction | Violation Looks Like |
|----|------|----------------------|---------------------|
| I1 | No ground forces airborne | F_susp = F_grip = 0 | Ground forces in the air |
| I2 | Gravity dominant | only gravity + air torques + gyro | Extra forces airborne |
| I3 | Pitch from throttle | throttle -> nose-up, brake -> nose-down | No air control or wrong direction |
| I4 | Roll from steering | steering -> roll torque | No roll control airborne |
| I5 | Gyro from wheel RPM | stabilization proportional to sum(omega_RPM) | No gyroscopic stabilization |
| I6 | Parabolic trajectory | x = v0*t, y = 0.5*g*t^2 | Non-parabolic flight path |

---

## J. Temporal (4 checks)

| ID | Name | Analytical Prediction | Violation Looks Like |
|----|------|----------------------|---------------------|
| J1 | Deterministic replay | same inputs -> same outputs | Replay diverges from original |
| J2 | Fixed timestep independence | results within epsilon at different rates | Different behaviour at different FPS |
| J3 | No frame-rate artifacts | interpolation doesn't create impossible states | Jitter or teleportation at low FPS |
| J4 | State continuity | no discontinuous jumps | Position or velocity jumps between frames |

---

## K. ESC/Motor (5 checks)

| ID | Name | Analytical Prediction | Violation Looks Like |
|----|------|----------------------|---------------------|
| K1 | Engine cutoff threshold | disengages at speed <= threshold | Motor engages at zero speed |
| K2 | Reverse state machine | requires stop->brake->release->brake | Instant reverse at speed |
| K3 | Coast drag != braking | no traction mode change | Coasting applies brake force |
| K4 | Max speed limiting | asymptotic approach to V_max | Speed exceeds or never reaches V_max |
| K5 | Throttle ramp | follows ramp rate curve | Instant throttle response |

---

## L. Compound Scenarios (13 checks)

| ID | Name | Analytical Prediction | Violation Looks Like |
|----|------|----------------------|---------------------|
| L1 | At rest on flat | forces balance, no drift | Car creeps at rest |
| L2 | On slope at rest | holds or slides correctly | Car floats on hill |
| L3 | Full throttle + brake | decelerates | Car accelerates while braking |
| L4 | Extreme steering at speed | understeer | Car turns impossibly tight |
| L5 | Jump landing | impact proportional to vertical velocity | Soft landing from height |
| L6 | Upside down | no ground forces | Ground forces when inverted |
| L7 | Ground->air transition | smooth force transition | Force spike at takeoff |
| L8 | Air->ground transition | damped landing | Bounce or explosion on landing |
| L9 | Half on ledge | tips correctly | Car balances impossibly |
| L10 | High-speed straight | V_max limiting | Infinite acceleration |
| L11 | Donuts | circular motion with lateral slip | No sustained circular motion |
| L12 | Weight transfer braking | front loads up | Equal weight under braking |
| L13 | Roll effect on grip | affects distribution | Roll has no effect on grip |
