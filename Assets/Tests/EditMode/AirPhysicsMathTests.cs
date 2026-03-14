using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for airborne physics calculations.
    /// Covers pitch/roll torque, gyroscopic damping, and RPM averaging.
    /// </summary>
    public class AirPhysicsMathTests
    {
        const float k_PitchTorque = 40f;
        const float k_PitchSensitivity = 1f;
        const float k_RollTorque = 12.8f;
        const float k_RollSensitivity = 0.6f;
        const float k_GyroStrength = 4f;
        const float k_GyroFullRpm = 125f;


        // ---- Pitch Torque ----

        [Test]
        public void ComputePitchTorque_FullThrottle_ReturnsMaxPositive()
        {
            float torque = AirPhysicsMath.ComputePitchTorque(1f, 0f, k_PitchTorque, k_PitchSensitivity);
            Assert.AreEqual(k_PitchTorque, torque, 0.01f);
        }

        [Test]
        public void ComputePitchTorque_FullBrake_ReturnsMaxNegative()
        {
            float torque = AirPhysicsMath.ComputePitchTorque(0f, 1f, k_PitchTorque, k_PitchSensitivity);
            Assert.AreEqual(-k_PitchTorque, torque, 0.01f);
        }

        [Test]
        public void ComputePitchTorque_NoInput_ReturnsZero()
        {
            float torque = AirPhysicsMath.ComputePitchTorque(0f, 0f, k_PitchTorque, k_PitchSensitivity);
            Assert.AreEqual(0f, torque, 0.0001f);
        }

        [Test]
        public void ComputePitchTorque_BothPressed_Cancels()
        {
            float torque = AirPhysicsMath.ComputePitchTorque(0.5f, 0.5f, k_PitchTorque, k_PitchSensitivity);
            Assert.AreEqual(0f, torque, 0.0001f);
        }

        [Test]
        public void ComputePitchTorque_HalfSensitivity_HalvesOutput()
        {
            float torque = AirPhysicsMath.ComputePitchTorque(1f, 0f, k_PitchTorque, 0.5f);
            Assert.AreEqual(k_PitchTorque * 0.5f, torque, 0.01f);
        }


        // ---- Roll Torque ----

        [Test]
        public void ComputeRollTorque_FullRight_ReturnsPositive()
        {
            float torque = AirPhysicsMath.ComputeRollTorque(1f, k_RollTorque, k_RollSensitivity);
            Assert.AreEqual(k_RollTorque * k_RollSensitivity, torque, 0.01f);
        }

        [Test]
        public void ComputeRollTorque_FullLeft_ReturnsNegative()
        {
            float torque = AirPhysicsMath.ComputeRollTorque(-1f, k_RollTorque, k_RollSensitivity);
            Assert.AreEqual(-k_RollTorque * k_RollSensitivity, torque, 0.01f);
        }

        [Test]
        public void ComputeRollTorque_NoSteer_ReturnsZero()
        {
            float torque = AirPhysicsMath.ComputeRollTorque(0f, k_RollTorque, k_RollSensitivity);
            Assert.AreEqual(0f, torque, 0.0001f);
        }


        // ---- Gyro Damping ----

        [Test]
        public void ComputeGyroDampingFactor_BelowThreshold_ReturnsZero()
        {
            float factor = AirPhysicsMath.ComputeGyroDampingFactor(5f, k_GyroStrength, k_GyroFullRpm);
            Assert.AreEqual(0f, factor, 0.0001f);
        }

        [Test]
        public void ComputeGyroDampingFactor_AtFullRpm_ReturnsGyroStrength()
        {
            float factor = AirPhysicsMath.ComputeGyroDampingFactor(k_GyroFullRpm, k_GyroStrength, k_GyroFullRpm);
            Assert.AreEqual(k_GyroStrength, factor, 0.01f);
        }

        [Test]
        public void ComputeGyroDampingFactor_HalfRpm_ReturnsHalfStrength()
        {
            float halfRpm = k_GyroFullRpm * 0.5f;
            float factor = AirPhysicsMath.ComputeGyroDampingFactor(halfRpm, k_GyroStrength, k_GyroFullRpm);
            Assert.AreEqual(k_GyroStrength * 0.5f, factor, 0.01f);
        }

        [Test]
        public void ComputeGyroDampingFactor_AboveFullRpm_ClampsToStrength()
        {
            float factor = AirPhysicsMath.ComputeGyroDampingFactor(500f, k_GyroStrength, k_GyroFullRpm);
            Assert.AreEqual(k_GyroStrength, factor, 0.01f);
        }

        [Test]
        public void ComputeGyroDampingFactor_ZeroGyroFullRpm_ReturnsZero()
        {
            float factor = AirPhysicsMath.ComputeGyroDampingFactor(100f, k_GyroStrength, 0f);
            Assert.AreEqual(0f, factor, 0.0001f);
        }


        // ---- Average RPM ----

        [Test]
        public void ComputeAverageAbsRpm_FourWheels_ReturnsCorrectAverage()
        {
            float[] rpms = { 100f, -100f, 150f, -50f };
            float avg = AirPhysicsMath.ComputeAverageAbsRpm(rpms);
            Assert.AreEqual(100f, avg, 0.01f);
        }

        [Test]
        public void ComputeAverageAbsRpm_EmptyArray_ReturnsZero()
        {
            float avg = AirPhysicsMath.ComputeAverageAbsRpm(new float[0]);
            Assert.AreEqual(0f, avg, 0.0001f);
        }

        [Test]
        public void ComputeAverageAbsRpm_NullArray_ReturnsZero()
        {
            float avg = AirPhysicsMath.ComputeAverageAbsRpm(null);
            Assert.AreEqual(0f, avg, 0.0001f);
        }
    }
}
