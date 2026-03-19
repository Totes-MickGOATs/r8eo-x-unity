#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for GripMath public functions.
    /// Tests verify physically correct behavior from inputs/outputs only.
    /// Uses realistic 1/10th scale RC car values throughout.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxGripTests
    {
        // =====================================================================
        // GripMath — ComputeSlipRatio
        // =====================================================================

        [Test]
        public void SlipRatio_StraightLine_ZeroSlip()
        {
            // Moving forward with no lateral component
            float slip = GripMath.ComputeSlipRatio(0f, 5.0f);
            Assert.AreEqual(0f, slip, k_Epsilon,
                "Zero lateral velocity should give zero slip ratio");
        }

        [Test]
        public void SlipRatio_PurelySideways_FullSlip()
        {
            // All velocity is lateral — complete slide
            float lateralSpeed = 5.0f;
            float totalSpeed = 5.0f;
            float slip = GripMath.ComputeSlipRatio(lateralSpeed, totalSpeed);
            Assert.AreEqual(1.0f, slip, k_Epsilon,
                "Purely sideways motion should give slip ratio of 1.0");
        }

        [Test]
        public void SlipRatio_CarAtRest_NoSlip()
        {
            float slip = GripMath.ComputeSlipRatio(0f, 0f);
            Assert.AreEqual(0f, slip, k_Epsilon,
                "Stationary car should have zero slip (no division by zero)");
        }

        [Test]
        public void SlipRatio_NegativeLateral_UsesAbsoluteValue()
        {
            float slipPos = GripMath.ComputeSlipRatio(3f, 5f);
            float slipNeg = GripMath.ComputeSlipRatio(-3f, 5f);
            Assert.AreEqual(slipPos, slipNeg, k_Epsilon,
                "Slip ratio should use absolute lateral velocity");
        }

        [Test]
        public void SlipRatio_AlwaysBetweenZeroAndOne()
        {
            // Even with lateral > speed (physically unusual), should clamp
            float slip = GripMath.ComputeSlipRatio(10f, 5f);
            Assert.GreaterOrEqual(slip, 0f);
            Assert.LessOrEqual(slip, 1f,
                "Slip ratio must always be clamped to [0, 1]");
        }

        [Test]
        public void SlipRatio_VerySmallSpeed_ReturnsZero()
        {
            // Near-zero speed should avoid division issues
            float slip = GripMath.ComputeSlipRatio(0.00001f, 0.00001f);
            Assert.AreEqual(0f, slip, k_Epsilon,
                "Near-zero speed should return zero slip to avoid instability");
        }


        // =====================================================================
        // GripMath — ComputeLateralForceMagnitude
        // =====================================================================

        [Test]
        public void LateralForce_OpposesLateralVelocity()
        {
            // Positive lateral velocity should produce negative force (opposing)
            float force = GripMath.ComputeLateralForceMagnitude(2.0f, 0.8f, 1.0f, 10f);
            Assert.Less(force, 0f,
                "Lateral force must oppose positive lateral velocity");

            // Negative lateral velocity should produce positive force
            float forceNeg = GripMath.ComputeLateralForceMagnitude(-2.0f, 0.8f, 1.0f, 10f);
            Assert.Greater(forceNeg, 0f,
                "Lateral force must oppose negative lateral velocity");
        }

        [Test]
        public void LateralForce_DoublingGripLoad_DoublesForce()
        {
            float f1 = GripMath.ComputeLateralForceMagnitude(2.0f, 0.8f, 1.0f, 10f);
            float f2 = GripMath.ComputeLateralForceMagnitude(2.0f, 0.8f, 1.0f, 20f);
            Assert.AreEqual(f2, f1 * 2f, 0.01f,
                "Doubling grip load should double lateral force (linearity)");
        }

        [Test]
        public void LateralForce_ZeroGripLoad_ZeroForce()
        {
            float force = GripMath.ComputeLateralForceMagnitude(2.0f, 0.8f, 1.0f, 0f);
            Assert.AreEqual(0f, force, k_Epsilon,
                "Zero grip load should produce zero lateral force");
        }

        [Test]
        public void LateralForce_ZeroGripFactor_ZeroForce()
        {
            float force = GripMath.ComputeLateralForceMagnitude(2.0f, 0f, 1.0f, 10f);
            Assert.AreEqual(0f, force, k_Epsilon,
                "Zero grip factor should produce zero lateral force");
        }

        [Test]
        public void LateralForce_ZeroLateralVelocity_ZeroForce()
        {
            float force = GripMath.ComputeLateralForceMagnitude(0f, 0.8f, 1.0f, 10f);
            Assert.AreEqual(0f, force, k_Epsilon,
                "Zero lateral velocity should produce zero lateral force");
        }


    }
}

#pragma warning restore CS0618
