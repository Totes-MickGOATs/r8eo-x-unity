using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for gyroscopic precession and reaction torque math.
    /// Covers cross-product torque, reaction torque from spin change,
    /// and wheel angular velocity conversion.
    /// </summary>
    public class GyroscopicMathTests
    {
        // ---- Constants ----

        const float k_Epsilon = 0.0001f;
        const float k_WheelMoI = 0.000120f; // kg*m^2, typical 1/10 RC wheel
        const float k_WheelRadius = 0.053f; // metres
        const float k_SpinRate = 283f; // rad/s at ~15 m/s
        const float k_DeltaTime = 0.02f; // 50 Hz fixed timestep


        // ---- ComputeGyroscopicTorque ----

        [Test]
        public void ComputeGyroscopicTorque_YawWithSpinningWheels_ProducesPitchTorque()
        {
            // Wheels spin around local right axis (1,0,0).
            // Body yaws (rotates around up axis) at 2 rad/s.
            // τ = ω_body × (I * ω_spin * axis) = (0,2,0) × (I*ω*(1,0,0))
            // = (0,2,0) × (L,0,0) = (0*0 - 0*0, 0*L - 0*0, 0*0 - 2*L)
            // = (0, 0, -2*L) → torque in -Z direction (pitch)
            Vector3 bodyOmega = new Vector3(0f, 2f, 0f);
            Vector3 spinAxis = Vector3.right;

            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(
                bodyOmega, spinAxis, k_WheelMoI, k_SpinRate);

            // Expect non-zero Z component (pitch torque), zero X and Y
            Assert.AreEqual(0f, torque.x, k_Epsilon, "X should be zero");
            Assert.AreEqual(0f, torque.y, k_Epsilon, "Y should be zero");
            Assert.AreNotEqual(0f, torque.z, "Z (pitch) should be non-zero");
        }

        [Test]
        public void ComputeGyroscopicTorque_PitchWithSpinningWheels_ProducesYawTorque()
        {
            // Body pitches (rotates around local right axis) at 2 rad/s.
            // τ = (2,0,0) × (I*ω*(1,0,0)) = (0,0,0) ... wait, parallel axes.
            // Actually, pitch rotation around Z axis in Unity:
            // Body pitches: angular velocity around Z = (0,0,2)
            // τ = (0,0,2) × (L,0,0) = (0*0 - 2*0, 2*L - 0*0, 0*0 - 0*L)
            // = (0, 2*L, 0) → torque in +Y direction (yaw)
            Vector3 bodyOmega = new Vector3(0f, 0f, 2f);
            Vector3 spinAxis = Vector3.right;

            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(
                bodyOmega, spinAxis, k_WheelMoI, k_SpinRate);

            // Expect non-zero Y component (yaw torque)
            Assert.AreEqual(0f, torque.x, k_Epsilon, "X should be zero");
            Assert.AreNotEqual(0f, torque.y, "Y (yaw) should be non-zero");
            Assert.AreEqual(0f, torque.z, k_Epsilon, "Z should be zero");
        }

        [Test]
        public void ComputeGyroscopicTorque_RollWithSpinningWheels_ProducesZeroTorque()
        {
            // Body rolls around spin axis (right). Cross product of parallel vectors = zero.
            // τ = (2,0,0) × (L,0,0) = (0,0,0)
            Vector3 bodyOmega = new Vector3(2f, 0f, 0f);
            Vector3 spinAxis = Vector3.right;

            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(
                bodyOmega, spinAxis, k_WheelMoI, k_SpinRate);

            Assert.AreEqual(0f, torque.x, k_Epsilon);
            Assert.AreEqual(0f, torque.y, k_Epsilon);
            Assert.AreEqual(0f, torque.z, k_Epsilon);
        }

        [Test]
        public void ComputeGyroscopicTorque_ZeroSpin_ReturnsZero()
        {
            Vector3 bodyOmega = new Vector3(0f, 2f, 0f);
            Vector3 spinAxis = Vector3.right;

            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(
                bodyOmega, spinAxis, k_WheelMoI, 0f);

            Assert.AreEqual(Vector3.zero, torque);
        }

        [Test]
        public void ComputeGyroscopicTorque_ZeroBodyRotation_ReturnsZero()
        {
            Vector3 bodyOmega = Vector3.zero;
            Vector3 spinAxis = Vector3.right;

            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(
                bodyOmega, spinAxis, k_WheelMoI, k_SpinRate);

            Assert.AreEqual(Vector3.zero, torque);
        }


        // ---- ComputeReactionTorque ----

        [Test]
        public void ComputeReactionTorque_SpinIncrease_ReturnsOpposingTorque()
        {
            // Throttle increases spin: current > previous → positive Δω
            // Reaction opposes: τ = -axis * I * Δω/Δt
            Vector3 spinAxis = Vector3.right;
            float currentSpin = 300f;
            float previousSpin = 250f;

            Vector3 torque = GyroscopicMath.ComputeReactionTorque(
                spinAxis, k_WheelMoI, currentSpin, previousSpin, k_DeltaTime);

            // Δω = 50, Δω/Δt = 2500. τ = -(1,0,0) * 0.000120 * 2500 = (-0.3, 0, 0)
            // Torque should oppose spin-up: negative X component
            Assert.Less(torque.x, 0f, "Reaction should oppose spin increase (negative X)");
            Assert.AreEqual(0f, torque.y, k_Epsilon);
            Assert.AreEqual(0f, torque.z, k_Epsilon);
        }

        [Test]
        public void ComputeReactionTorque_SpinDecrease_ReturnsOpposingTorque()
        {
            // Brake decreases spin: current < previous → negative Δω
            // Reaction opposes: positive direction
            Vector3 spinAxis = Vector3.right;
            float currentSpin = 200f;
            float previousSpin = 300f;

            Vector3 torque = GyroscopicMath.ComputeReactionTorque(
                spinAxis, k_WheelMoI, currentSpin, previousSpin, k_DeltaTime);

            Assert.Greater(torque.x, 0f, "Reaction should oppose spin decrease (positive X)");
            Assert.AreEqual(0f, torque.y, k_Epsilon);
            Assert.AreEqual(0f, torque.z, k_Epsilon);
        }

        [Test]
        public void ComputeReactionTorque_NoSpinChange_ReturnsZero()
        {
            Vector3 spinAxis = Vector3.right;
            float spinRate = 283f;

            Vector3 torque = GyroscopicMath.ComputeReactionTorque(
                spinAxis, k_WheelMoI, spinRate, spinRate, k_DeltaTime);

            Assert.AreEqual(0f, torque.x, k_Epsilon);
            Assert.AreEqual(0f, torque.y, k_Epsilon);
            Assert.AreEqual(0f, torque.z, k_Epsilon);
        }

        [Test]
        public void ComputeReactionTorque_BrakeIsStrongerThanThrottle()
        {
            // Braking produces larger Δω than throttle over same time
            Vector3 spinAxis = Vector3.right;

            // Throttle: moderate increase
            float throttleCurrent = 300f;
            float throttlePrevious = 270f;
            Vector3 throttleTorque = GyroscopicMath.ComputeReactionTorque(
                spinAxis, k_WheelMoI, throttleCurrent, throttlePrevious, k_DeltaTime);

            // Brake: larger decrease (friction braking is near-instantaneous)
            float brakeCurrent = 200f;
            float brakePrevious = 270f;
            Vector3 brakeTorque = GyroscopicMath.ComputeReactionTorque(
                spinAxis, k_WheelMoI, brakeCurrent, brakePrevious, k_DeltaTime);

            Assert.Greater(brakeTorque.magnitude, throttleTorque.magnitude,
                "Brake reaction torque should be larger than throttle (larger Δω)");
        }


        // ---- ComputeWheelAngularVelocity ----

        [Test]
        public void ComputeWheelAngularVelocity_KnownSpeed_ReturnsCorrectRadPerSec()
        {
            // ω = v / r = 15 / 0.053 ≈ 283.02 rad/s
            float speed = 15f;
            float omega = GyroscopicMath.ComputeWheelAngularVelocity(speed, k_WheelRadius);

            float expected = speed / k_WheelRadius;
            Assert.AreEqual(expected, omega, 0.01f);
        }
    }
}
