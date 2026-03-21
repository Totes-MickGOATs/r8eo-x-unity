using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for TumbleMath.ComputeTumbleFactor including hysteresis behavior.
    /// Smoothstep tests live in TumbleSmoothstepTests.cs.
    /// TiltAngle tests live in TumbleTiltTests.cs.
    /// </summary>
    public class TumbleFactorTests
    {
        const float k_EngageDeg = 50f;
        const float k_FullDeg = 70f;
        const float k_HysteresisDeg = 5f;

        [Test]
        public void ComputeTumbleFactor_Upright_ReturnsZero()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                0f, false, false, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.AreEqual(0f, factor, 0.0001f);
        }

        [Test]
        public void ComputeTumbleFactor_BelowEngage_ReturnsZero()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                45f, false, false, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.AreEqual(0f, factor, 0.0001f);
        }

        [Test]
        public void ComputeTumbleFactor_AtFullDeg_ReturnsOne()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                70f, false, false, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.AreEqual(1f, factor, 0.0001f);
        }

        [Test]
        public void ComputeTumbleFactor_BeyondFullDeg_ReturnsOne()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                90f, false, false, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.AreEqual(1f, factor, 0.0001f);
        }

        [Test]
        public void ComputeTumbleFactor_MidRange_ReturnsSmoothstepValue()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                60f, false, false, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.AreEqual(0.5f, factor, 0.01f);
        }

        [Test]
        public void ComputeTumbleFactor_Airborne_AlwaysZero()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                80f, true, false, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.AreEqual(0f, factor, 0.0001f);
        }

        [Test]
        public void ComputeTumbleFactor_Airborne_EvenWhenWasTumbling_ReturnsZero()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                80f, true, true, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.AreEqual(0f, factor, 0.0001f);
        }

        [Test]
        public void ComputeTumbleFactor_Hysteresis_WasTumbling_LowerEngageThreshold()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                47f, false, true, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.Greater(factor, 0f,
                "With hysteresis, tumble should remain active above (engage - hysteresis)");
        }

        [Test]
        public void ComputeTumbleFactor_Hysteresis_WasNotTumbling_NormalThreshold()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                47f, false, false, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.AreEqual(0f, factor, 0.0001f);
        }

        [Test]
        public void ComputeTumbleFactor_Hysteresis_PreventsBouncing()
        {
            float f1 = TumbleMath.ComputeTumbleFactor(
                52f, false, false, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.Greater(f1, 0f, "52 deg should enter tumble");

            float f2 = TumbleMath.ComputeTumbleFactor(
                48f, false, true, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.Greater(f2, 0f, "48 deg with hysteresis should stay in tumble");

            float f3 = TumbleMath.ComputeTumbleFactor(
                44f, false, true, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.AreEqual(0f, f3, 0.0001f, "44 deg should exit tumble even with hysteresis");
        }
    }
}
