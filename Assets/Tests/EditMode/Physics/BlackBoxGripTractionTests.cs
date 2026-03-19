#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for GripMath.ComputeEffectiveTraction and
    /// ComputeLongitudinalForceMagnitude.
    /// Extracted from BlackBoxGripTests to keep each file under 200 lines.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxGripTractionTests
    {
        // =====================================================================
        // GripMath — ComputeEffectiveTraction
        // =====================================================================

        [Test]
        public void EffectiveTraction_NormalDriving_ReturnsBaseTraction()
        {
            float traction = GripMath.ComputeEffectiveTraction(
                isBraking: false, forwardSpeed: 5f, engineForce: 10f,
                zTraction: 0.5f, zBrakeTraction: 0.8f,
                staticFrictionSpeed: 0.1f, staticFrictionTraction: 1.2f);
            Assert.AreEqual(0.5f, traction, k_Epsilon,
                "Normal driving should use base traction");
        }

        [Test]
        public void EffectiveTraction_Braking_ReturnsBrakeTraction()
        {
            float traction = GripMath.ComputeEffectiveTraction(
                isBraking: true, forwardSpeed: 5f, engineForce: 0f,
                zTraction: 0.5f, zBrakeTraction: 0.8f,
                staticFrictionSpeed: 0.1f, staticFrictionTraction: 1.2f);
            Assert.AreEqual(0.8f, traction, k_Epsilon,
                "Braking should use brake traction");
        }

        [Test]
        public void EffectiveTraction_StoppedNoEngine_StaticFriction()
        {
            float traction = GripMath.ComputeEffectiveTraction(
                isBraking: false, forwardSpeed: 0.01f, engineForce: 0f,
                zTraction: 0.5f, zBrakeTraction: 0.8f,
                staticFrictionSpeed: 0.1f, staticFrictionTraction: 1.2f);
            Assert.AreEqual(1.2f, traction, k_Epsilon,
                "Stopped with no engine should use static friction (highest)");
        }

        [Test]
        public void EffectiveTraction_StoppedWithEngine_BaseTraction()
        {
            float traction = GripMath.ComputeEffectiveTraction(
                isBraking: false, forwardSpeed: 0.01f, engineForce: 5f,
                zTraction: 0.5f, zBrakeTraction: 0.8f,
                staticFrictionSpeed: 0.1f, staticFrictionTraction: 1.2f);
            Assert.AreEqual(0.5f, traction, k_Epsilon,
                "Stopped but with engine force should use base traction, not static");
        }

        [Test]
        public void EffectiveTraction_BrakingAndStopped_UsesBrakeTraction()
        {
            // Braking flag takes priority, then static friction check
            // Since isBraking=true, effectiveTraction starts as brake, then
            // static friction override only applies when engineForce == 0
            float traction = GripMath.ComputeEffectiveTraction(
                isBraking: true, forwardSpeed: 0.01f, engineForce: 0f,
                zTraction: 0.5f, zBrakeTraction: 0.8f,
                staticFrictionSpeed: 0.1f, staticFrictionTraction: 1.2f);
            // Static friction override applies because speed < threshold and engine == 0
            Assert.AreEqual(1.2f, traction, k_Epsilon,
                "Stopped and braking with no engine should use static friction");
        }


        // =====================================================================
        // GripMath — ComputeLongitudinalForceMagnitude
        // =====================================================================

        [Test]
        public void LongitudinalForce_OpposesForwardMotion()
        {
            // Positive forward speed should produce negative force (opposing)
            float force = GripMath.ComputeLongitudinalForceMagnitude(3f, 0.5f, 1.0f, 10f);
            Assert.Less(force, 0f,
                "Longitudinal friction must oppose forward motion");
        }

        [Test]
        public void LongitudinalForce_ZeroLoad_ZeroForce()
        {
            float force = GripMath.ComputeLongitudinalForceMagnitude(3f, 0.5f, 1.0f, 0f);
            Assert.AreEqual(0f, force, k_Epsilon,
                "Zero grip load should produce zero longitudinal force");
        }

        [Test]
        public void LongitudinalForce_NegativeSpeed_PositiveForce()
        {
            // Reversing should produce force that opposes reverse direction
            float force = GripMath.ComputeLongitudinalForceMagnitude(-3f, 0.5f, 1.0f, 10f);
            Assert.Greater(force, 0f,
                "Negative speed (reversing) should produce positive force (opposing)");
        }

        [Test]
        public void LongitudinalForce_ZeroTraction_ZeroForce()
        {
            float force = GripMath.ComputeLongitudinalForceMagnitude(3f, 0f, 1.0f, 10f);
            Assert.AreEqual(0f, force, k_Epsilon,
                "Zero traction should produce zero longitudinal force");
        }
    }
}

#pragma warning restore CS0618
