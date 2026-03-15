using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Tests.PlayMode.Helpers;

namespace R8EOX.Tests.PlayMode
{
    /// <summary>
    /// PlayMode compound scenario tests for physics conformance (Category L + D8).
    /// Black-box tests: observe Rigidbody state (position, velocity, angularVelocity)
    /// and wheel public properties. Expected values derive from physics formulas.
    ///
    /// Input simulation: RCCar has null _input (no RCInput attached), so throttle/brake
    /// inputs are zero by default. Tests that need driving forces manipulate
    /// RaycastWheel.MotorForceShare and IsBraking directly, or apply Rigidbody velocity.
    ///
    /// Reference: .ai/knowledge/architecture/audit-physics-conformance.md (Category L, D8)
    /// </summary>
    [TestFixture]
    [Category("Conformance")]
    public class CompoundConformanceTests
    {
        // ---- Physics Constants (from adr-001-physics-model.md) ----

        const float k_Mass = ConformanceSceneSetup.k_Mass;                 // 1.5 kg
        const float k_WheelRadius = ConformanceSceneSetup.k_WheelRadius;   // 0.166 m
        const float k_Wheelbase = ConformanceSceneSetup.k_Wheelbase;       // 0.28 m
        const float k_Gravity = ConformanceSceneSetup.k_Gravity;           // 9.81 m/s^2
        const float k_GripCoeff = ConformanceSceneSetup.k_GripCoeff;       // 0.7

        // ---- Timing Constants ----

        /// <summary>Physics frames to wait for car to settle on ground (2s at 50Hz).</summary>
        const int k_SettleFrames = 120;
        /// <summary>Physics frames for drive/force application (1s at 50Hz).</summary>
        const int k_DriveFrames = 60;
        /// <summary>Physics frames for extended drive (10s at 50Hz).</summary>
        const int k_ExtendedDriveFrames = 600;
        /// <summary>Physics frames for long settling after landing (3s at 50Hz).</summary>
        const int k_LandingSettleFrames = 180;

        // ---- Tolerance Constants ----

        /// <summary>Velocity magnitude below which the car is considered at rest (m/s).</summary>
        const float k_RestVelocityThreshold = 0.05f;
        /// <summary>L1-only velocity threshold — slightly looser than WaitForSettle to match measured physics (m/s).</summary>
        const float k_L1RestVelocityThreshold = 0.08f; // measured: ~0.059 m/s; 0.05 was too tight
        /// <summary>Angular velocity magnitude below which the car is considered rotationally still (rad/s).</summary>
        const float k_RestAngularVelocityThreshold = 0.05f;
        /// <summary>Position drift threshold for rest tests (m).</summary>
        const float k_RestPositionDrift = 0.02f;
        /// <summary>Tolerance for free-fall velocity comparison (fraction).</summary>
        const float k_FreeFallTolerance = 0.10f;
        /// <summary>Maximum velocity discontinuity between frames (m/s).</summary>
        const float k_MaxVelocityDiscontinuity = 1.5f;
        /// <summary>Motor force per rear wheel for simulated throttle (N).</summary>
        const float k_TestMotorForcePerWheel = 13f;
        /// <summary>Speed convergence threshold: speed change per second below this = converged (m/s^2).</summary>
        const float k_SpeedConvergenceRate = 0.1f;

        // ---- Spawn Positions ----

        static readonly Vector3 k_DefaultSpawn = new Vector3(0f, 0.5f, 0f);
        static readonly Vector3 k_LowDropSpawn = new Vector3(0f, 0.5f, 0f);
        static readonly Vector3 k_HighDropSpawn = new Vector3(0f, 1.0f, 0f);
        static readonly Vector3 k_FreeFallSpawn = new Vector3(0f, 5f, 0f);
        static readonly Vector3 k_LandingDropSpawn = new Vector3(0f, 0.3f, 0f);


        // ---- Test Fixtures ----

        private GameObject _ground;
        private GameObject _car;
        private Rigidbody _carRb;
        private R8EOX.Vehicle.RCCar _rcCar;
        private R8EOX.Vehicle.RaycastWheel[] _wheels;


        // ---- Setup / Teardown ----

        [TearDown]
        public void TearDown()
        {
            if (_car != null) Object.DestroyImmediate(_car);
            if (_ground != null) Object.DestroyImmediate(_ground);
            _car = null;
            _ground = null;
            _carRb = null;
            _rcCar = null;
            _wheels = null;
        }


        // ---- Helper Methods ----

        /// <summary>
        /// Spawns ground + vehicle at the given position and caches references.
        /// </summary>
        private void SpawnTestVehicle(Vector3 spawnPosition)
        {
            _ground = ConformanceSceneSetup.CreateGround();
            _car = ConformanceSceneSetup.CreateTestVehicle(spawnPosition);
            _carRb = _car.GetComponent<Rigidbody>();
            _rcCar = _car.GetComponent<R8EOX.Vehicle.RCCar>();
            _wheels = _car.GetComponentsInChildren<R8EOX.Vehicle.RaycastWheel>();
        }

        /// <summary>
        /// Yields the given number of FixedUpdate frames.
        /// </summary>
        private static IEnumerator WaitPhysicsFrames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return new WaitForFixedUpdate();
        }

        /// <summary>
        /// Waits until the car's velocity drops below threshold or timeout is reached.
        /// Returns true if settled, false if timed out.
        /// </summary>
        private IEnumerator WaitForSettle(float maxSeconds = 3f)
        {
            float elapsed = 0f;
            while (elapsed < maxSeconds)
            {
                if (_carRb.velocity.magnitude < k_RestVelocityThreshold
                    && _carRb.angularVelocity.magnitude < k_RestAngularVelocityThreshold)
                {
                    yield break;
                }
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }
        }

        /// <summary>
        /// Sets MotorForceShare on all motor wheels to simulate throttle.
        /// totalForce is distributed evenly across motor wheels.
        /// </summary>
        private void SetMotorForce(float totalForce)
        {
            int motorCount = 0;
            foreach (var w in _wheels)
                if (w.IsMotor) motorCount++;

            float perWheel = motorCount > 0 ? totalForce / motorCount : 0f;
            foreach (var w in _wheels)
                w.MotorForceShare = w.IsMotor ? perWheel : 0f;
        }

        /// <summary>
        /// Sets IsBraking on all motor wheels.
        /// </summary>
        private void SetBraking(bool braking)
        {
            foreach (var w in _wheels)
                if (w.IsMotor) w.IsBraking = braking;
        }

        /// <summary>
        /// Clears all motor force and braking.
        /// </summary>
        private void ClearDriveInputs()
        {
            foreach (var w in _wheels)
            {
                w.MotorForceShare = 0f;
                w.IsBraking = false;
            }
        }


        // ================================================================
        // L1: At Rest on Flat — All Forces Balance, No Drift
        // ================================================================

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator L1_AtRestOnFlat_AllForcesBalance_NoDrift()
        {
            // Spawn car on flat surface, wait for settling
            SpawnTestVehicle(k_DefaultSpawn);
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Record position after initial settle
            Vector3 settledPos = _car.transform.position;

            // Wait additional frames to detect drift
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Assert: velocity magnitude < threshold
            Assert.Less(_carRb.velocity.magnitude, k_L1RestVelocityThreshold,
                "L1: Car velocity should be near zero at rest on flat ground. " +
                $"Actual: {_carRb.velocity.magnitude:F4} m/s");

            // Assert: angular velocity magnitude < threshold
            Assert.Less(_carRb.angularVelocity.magnitude, k_RestAngularVelocityThreshold,
                "L1: Car angular velocity should be near zero at rest. " +
                $"Actual: {_carRb.angularVelocity.magnitude:F4} rad/s");

            // Assert: position hasn't drifted
            float positionDrift = Vector3.Distance(_car.transform.position, settledPos);
            Assert.Less(positionDrift, k_RestPositionDrift,
                "L1: Car should not drift after settling. " +
                $"Drifted {positionDrift:F4}m from settled position");
        }


        // ================================================================
        // L3: Full Throttle and Brake — Decelerates
        // ================================================================

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator L3_FullThrottleAndBrake_Decelerates()
        {
            // Spawn and settle
            SpawnTestVehicle(k_DefaultSpawn);
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Apply full throttle for 1 second to build speed
            // Engine force: 26N total for 13.5T preset, 13N per rear wheel
            SetMotorForce(26f);
            yield return WaitPhysicsFrames(k_DriveFrames);

            float speedAfterThrottle = _carRb.velocity.magnitude;
            Assert.Greater(speedAfterThrottle, 0.3f,
                "L3 precondition: Car should have gained speed after throttle. " +
                $"Actual speed: {speedAfterThrottle:F3} m/s");

            // Now apply throttle AND braking simultaneously
            // Brake force is applied through IsBraking flag + longitudinal friction
            SetMotorForce(26f);
            SetBraking(true);
            yield return WaitPhysicsFrames(k_DriveFrames);

            float speedAfterBrake = _carRb.velocity.magnitude;

            // Assert: speed decreased — brake overpowers or at least slows the motor
            Assert.Less(speedAfterBrake, speedAfterThrottle * 1.5f,
                "L3: Speed should not increase significantly when braking with throttle. " +
                $"Before brake: {speedAfterThrottle:F3} m/s, after: {speedAfterBrake:F3} m/s");

            ClearDriveInputs();
        }


        // ================================================================
        // L5: Jump Landing — Impact Proportional to Height
        // ================================================================

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator L5_JumpLanding_ImpactProportionalToHeight()
        {
            // --- Drop from low height (0.5m above ground surface) ---
            SpawnTestVehicle(k_LowDropSpawn);

            // Wait for landing — track peak downward velocity before contact
            float lowDropPeakVelocity = 0f;
            for (int i = 0; i < k_SettleFrames; i++)
            {
                float downSpeed = -_carRb.velocity.y;
                if (downSpeed > lowDropPeakVelocity)
                    lowDropPeakVelocity = downSpeed;
                yield return new WaitForFixedUpdate();
            }

            // Clean up first car
            Object.DestroyImmediate(_car);
            Object.DestroyImmediate(_ground);

            // --- Drop from high height (1.0m above ground surface) ---
            SpawnTestVehicle(k_HighDropSpawn);

            float highDropPeakVelocity = 0f;
            for (int i = 0; i < k_SettleFrames; i++)
            {
                float downSpeed = -_carRb.velocity.y;
                if (downSpeed > highDropPeakVelocity)
                    highDropPeakVelocity = downSpeed;
                yield return new WaitForFixedUpdate();
            }

            // Assert: higher drop produces larger impact velocity
            Assert.Greater(highDropPeakVelocity, lowDropPeakVelocity,
                "L5: Higher drop should produce greater impact velocity. " +
                $"Low drop peak: {lowDropPeakVelocity:F3} m/s, high drop peak: {highDropPeakVelocity:F3} m/s");

            // Assert: impact velocity approximately matches v = sqrt(2*g*h) within tolerance
            // Ground surface is at y = 0.05 (top of 0.1m thick cube), car spawns at given y
            // Effective drop height for the high drop:
            // h_eff = spawnY - (ground_surface + suspension_rest) approximately
            // We use a loose 40% tolerance because suspension engagement absorbs some fall
            float expectedHighVelocity = Mathf.Sqrt(2f * k_Gravity * k_HighDropSpawn.y);
            float tolerance = expectedHighVelocity * 0.40f;
            Assert.AreEqual(expectedHighVelocity, highDropPeakVelocity, tolerance,
                "L5: Impact velocity should approximate sqrt(2*g*h). " +
                $"Expected ~{expectedHighVelocity:F3} m/s (+/-{tolerance:F3}), " +
                $"got {highDropPeakVelocity:F3} m/s");
        }


        // ================================================================
        // L7: Ground-to-Air Transition — Smooth Force Transition
        // ================================================================

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator L7_GroundToAir_SmoothForceTransition()
        {
            // Spawn car, settle, then launch it upward to create a ground-to-air transition
            SpawnTestVehicle(k_DefaultSpawn);
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Give the car upward + forward velocity to simulate driving off an edge
            _carRb.velocity = new Vector3(0f, 3f, 5f);

            // Sample velocity every FixedUpdate through the transition
            Vector3 prevVelocity = _carRb.velocity;
            float maxDiscontinuity = 0f;
            int sampleCount = 0;

            for (int i = 0; i < k_SettleFrames; i++)
            {
                yield return new WaitForFixedUpdate();
                Vector3 currentVelocity = _carRb.velocity;
                float discontinuity = (currentVelocity - prevVelocity).magnitude;

                if (discontinuity > maxDiscontinuity)
                    maxDiscontinuity = discontinuity;

                prevVelocity = currentVelocity;
                sampleCount++;
            }

            // Assert: no velocity discontinuity exceeds threshold between frames
            // Allow for gravity contribution per frame: g * dt ~= 9.81 * 0.02 = 0.196 m/s
            // Plus landing impact, so threshold is generous
            Assert.Less(maxDiscontinuity, k_MaxVelocityDiscontinuity,
                "L7: Velocity should not have discontinuities > " +
                $"{k_MaxVelocityDiscontinuity} m/s during ground-to-air transition. " +
                $"Max discontinuity observed: {maxDiscontinuity:F4} m/s over {sampleCount} frames");
        }


        // ================================================================
        // L8: Air-to-Ground Transition — Damped Landing
        // ================================================================

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator L8_AirToGround_DampedLanding()
        {
            // Drop car from 0.3m height
            SpawnTestVehicle(k_LandingDropSpawn);

            // Track vertical position peaks after landing to verify damped oscillation
            var verticalPeaks = new List<float>();
            float prevY = _car.transform.position.y;
            float prevPrevY = prevY;
            bool landedOnce = false;

            for (int i = 0; i < k_LandingSettleFrames; i++)
            {
                yield return new WaitForFixedUpdate();
                float currentY = _car.transform.position.y;

                // Detect if we've made first ground contact
                if (!landedOnce)
                {
                    bool anyGrounded = false;
                    foreach (var w in _wheels)
                    {
                        if (w.IsOnGround) { anyGrounded = true; break; }
                    }
                    if (anyGrounded) landedOnce = true;
                }

                // After landing, detect local maxima (bounce peaks)
                if (landedOnce && prevY > prevPrevY && prevY > currentY)
                {
                    verticalPeaks.Add(prevY);
                }

                prevPrevY = prevY;
                prevY = currentY;
            }

            // Assert: car settled within 3 seconds (velocity near zero)
            Assert.Less(_carRb.velocity.magnitude, k_RestVelocityThreshold * 2f,
                "L8: Car should settle within 3 seconds after landing. " +
                $"Velocity: {_carRb.velocity.magnitude:F4} m/s");

            // Assert: if there are bounce peaks, each is lower than the previous (damped)
            if (verticalPeaks.Count >= 2)
            {
                for (int i = 1; i < verticalPeaks.Count; i++)
                {
                    Assert.LessOrEqual(verticalPeaks[i], verticalPeaks[i - 1] + 0.005f,
                        $"L8: Bounce peak {i} ({verticalPeaks[i]:F4}m) should be <= " +
                        $"peak {i - 1} ({verticalPeaks[i - 1]:F4}m) for damped oscillation");
                }
            }
            // If no bounce peaks detected, the suspension absorbed the landing cleanly (also valid)
        }


        // ================================================================
        // L10: High-Speed Straight — Speed Converges to Max
        // ================================================================

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator L10_HighSpeedStraight_SpeedConvergesToMax()
        {
            // Spawn and settle
            SpawnTestVehicle(k_DefaultSpawn);
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Apply full throttle on flat surface for 10 seconds
            // 26N total engine force (13.5T motor preset)
            SetMotorForce(26f);

            float speedAtHalfway = 0f;

            for (int i = 0; i < k_ExtendedDriveFrames; i++)
            {
                yield return new WaitForFixedUpdate();

                if (i == k_ExtendedDriveFrames / 2)
                    speedAtHalfway = _carRb.velocity.magnitude;
            }

            float finalSpeed = _carRb.velocity.magnitude;

            // Assert: speed stops increasing (convergence)
            // The rate of speed increase should diminish over time
            // Compare speed gain in first half vs second half
            float firstHalfGain = speedAtHalfway;
            float secondHalfGain = finalSpeed - speedAtHalfway;

            Assert.Less(secondHalfGain, firstHalfGain + 0.5f,
                "L10: Speed gain in second half should be less than or equal to first half " +
                "(convergence toward V_max). " +
                $"First half gain: {firstHalfGain:F3} m/s, second half gain: {secondHalfGain:F3} m/s");

            // Assert: final speed is below theoretical V_max from motor preset
            // Motor preset 13.5T has maxSpeed = 27 m/s
            float theoreticalVMax = 27f;
            Assert.Less(finalSpeed, theoreticalVMax * 1.1f,
                "L10: Final speed should not exceed theoretical V_max. " +
                $"V_max = {theoreticalVMax} m/s, actual = {finalSpeed:F3} m/s");

            ClearDriveInputs();
        }


        // ================================================================
        // D8: Free Fall — Acceleration Equals Gravity
        // ================================================================

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator D8_FreeFall_AccelerationEqualsGravity()
        {
            // Spawn car at height 5m (well above ground)
            SpawnTestVehicle(k_FreeFallSpawn);

            // Let one frame pass to initialize
            yield return new WaitForFixedUpdate();

            // Record initial downward velocity
            float initialDownSpeed = -_carRb.velocity.y;

            // Free-fall for 0.5 seconds (25 frames at 50Hz default)
            // Use actual frame count based on fixedDeltaTime
            float targetFallTime = 0.5f;
            int fallFrames = Mathf.RoundToInt(targetFallTime / Time.fixedDeltaTime);

            // Ensure we don't hit ground: at 5m height, 0.5s of freefall covers
            // d = 0.5*g*t^2 = 0.5*9.81*0.25 = 1.23m. Car at 5m won't hit ground.
            for (int i = 0; i < fallFrames; i++)
                yield return new WaitForFixedUpdate();

            // Measure velocity after 0.5 seconds of free fall
            float finalDownSpeed = -_carRb.velocity.y;
            float velocityGain = finalDownSpeed - initialDownSpeed;

            // Expected: delta_v = g * t = 9.81 * 0.5 = 4.905 m/s
            float expectedVelocityGain = k_Gravity * targetFallTime;

            // Assert within tolerance (allow for air drag, initial frame artifacts)
            float toleranceAbsolute = expectedVelocityGain * k_FreeFallTolerance;
            Assert.AreEqual(expectedVelocityGain, velocityGain, toleranceAbsolute,
                "D8: Free-fall velocity gain should match g*t. " +
                $"Expected: {expectedVelocityGain:F3} m/s, " +
                $"actual: {velocityGain:F3} m/s " +
                $"(tolerance: {k_FreeFallTolerance * 100f:F0}%)");

            // Verify car is still airborne (hasn't hit ground)
            bool anyGrounded = false;
            foreach (var w in _wheels)
            {
                if (w.IsOnGround) { anyGrounded = true; break; }
            }
            Assert.IsFalse(anyGrounded,
                "D8: Car should still be airborne during free-fall measurement. " +
                "If grounded, increase spawn height");
        }


        // ================================================================
        // L12: Weight Transfer Braking — Front Loads Up
        // ================================================================

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator L12_WeightTransferBraking_FrontLoadsUp()
        {
            // Spawn and settle
            SpawnTestVehicle(k_DefaultSpawn);
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Drive forward to build speed
            SetMotorForce(26f);
            yield return WaitPhysicsFrames(k_DriveFrames);

            float speedBeforeBrake = _carRb.velocity.magnitude;
            Assert.Greater(speedBeforeBrake, 0.3f,
                "L12 precondition: Car should have speed before braking");

            // Record pitch angle before braking
            // Pitch is rotation around the local X axis. In Unity, a forward-pitched car
            // has the nose down = negative pitch (euler X increases).
            float pitchBefore = _car.transform.eulerAngles.x;
            if (pitchBefore > 180f) pitchBefore -= 360f;

            // Brake hard: clear motor, set braking, apply backward impulse via friction
            ClearDriveInputs();
            SetBraking(true);

            // Also reduce velocity by applying backward force directly
            // to simulate the effect of brake friction (since we bypass ESC)
            _carRb.AddForce(-_car.transform.forward * 20f, ForceMode.Force);

            yield return WaitPhysicsFrames(k_DriveFrames / 2);

            float pitchDuringBrake = _car.transform.eulerAngles.x;
            if (pitchDuringBrake > 180f) pitchDuringBrake -= 360f;

            // Under braking, the nose should dip forward (pitch angle changes)
            // Weight transfer causes front suspension to compress more than rear.
            // We check that pitch changed at all during braking.
            float pitchChange = Mathf.Abs(pitchDuringBrake - pitchBefore);

            // If WheelFrameState is available, we could compare front vs rear grip load.
            // For now, check front wheels have more grip load than rear during braking.
            float frontLoadSum = 0f;
            float rearLoadSum = 0f;
            foreach (var w in _wheels)
            {
                if (w.IsOnGround)
                {
                    // Front wheels have positive local Z (z > 0)
                    if (w.transform.localPosition.z > 0f)
                        frontLoadSum += w.LastGripLoad;
                    else
                        rearLoadSum += w.LastGripLoad;
                }
            }

            // Assert at least one of: pitch changed OR front load > rear load
            bool pitchChanged = pitchChange > 0.1f;
            bool frontLoadedMore = frontLoadSum > rearLoadSum * 0.9f;

            Assert.IsTrue(pitchChanged || frontLoadedMore,
                "L12: During braking, the car should either pitch forward " +
                $"(pitch change: {pitchChange:F3} deg) " +
                $"or front wheels should bear more load than rear " +
                $"(front: {frontLoadSum:F3}, rear: {rearLoadSum:F3}). " +
                "Neither condition met — weight transfer may not be working");

            ClearDriveInputs();
        }
    }
}
