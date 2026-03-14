using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for tumble detection and blending.
    /// Covers smoothstep, hysteresis, airborne zeroing, and tilt angle.
    /// </summary>
    public class TumbleMathTests
    {
        const float k_EngageDeg = 50f;
        const float k_FullDeg = 70f;
        const float k_HysteresisDeg = 5f;


        // ---- ComputeTumbleFactor ----

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
            // Midpoint between 50 and 70 = 60 degrees
            float factor = TumbleMath.ComputeTumbleFactor(
                60f, false, false, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            // t = (60 - 50) / (70 - 50) = 0.5
            // smoothstep(0.5) = 0.5^2 * (3 - 2*0.5) = 0.25 * 2.0 = 0.5
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


        // ---- Hysteresis ----

        [Test]
        public void ComputeTumbleFactor_Hysteresis_WasTumbling_LowerEngageThreshold()
        {
            // At 47 degrees: normally below engage (50), but with hysteresis (50-5=45)
            // wasTumbling=true → engage becomes 45, so 47 > 45 → tumble active
            float factor = TumbleMath.ComputeTumbleFactor(
                47f, false, true, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.Greater(factor, 0f,
                "With hysteresis, tumble should remain active above (engage - hysteresis)");
        }

        [Test]
        public void ComputeTumbleFactor_Hysteresis_WasNotTumbling_NormalThreshold()
        {
            // At 47 degrees: wasTumbling=false → engage stays at 50, 47 < 50 → no tumble
            float factor = TumbleMath.ComputeTumbleFactor(
                47f, false, false, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.AreEqual(0f, factor, 0.0001f);
        }

        [Test]
        public void ComputeTumbleFactor_Hysteresis_PreventsBouncing()
        {
            // Simulate oscillation around engage threshold
            // Frame 1: 52 degrees, not tumbling → enters tumble
            float f1 = TumbleMath.ComputeTumbleFactor(
                52f, false, false, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.Greater(f1, 0f, "52 deg should enter tumble");

            // Frame 2: 48 degrees, was tumbling → still tumbling (hysteresis lowers threshold to 45)
            float f2 = TumbleMath.ComputeTumbleFactor(
                48f, false, true, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.Greater(f2, 0f, "48 deg with hysteresis should stay in tumble");

            // Frame 3: 44 degrees, was tumbling → exits tumble (below hysteresis threshold of 45)
            float f3 = TumbleMath.ComputeTumbleFactor(
                44f, false, true, k_EngageDeg, k_FullDeg, k_HysteresisDeg);
            Assert.AreEqual(0f, f3, 0.0001f, "44 deg should exit tumble even with hysteresis");
        }


        // ---- Smoothstep ----

        [Test]
        public void Smoothstep_AtZero_ReturnsZero()
        {
            Assert.AreEqual(0f, TumbleMath.Smoothstep(0f), 0.0001f);
        }

        [Test]
        public void Smoothstep_AtOne_ReturnsOne()
        {
            Assert.AreEqual(1f, TumbleMath.Smoothstep(1f), 0.0001f);
        }

        [Test]
        public void Smoothstep_AtHalf_ReturnsHalf()
        {
            Assert.AreEqual(0.5f, TumbleMath.Smoothstep(0.5f), 0.0001f);
        }

        [Test]
        public void Smoothstep_ClampsBelow_ReturnsZero()
        {
            Assert.AreEqual(0f, TumbleMath.Smoothstep(-0.5f), 0.0001f);
        }

        [Test]
        public void Smoothstep_ClampsAbove_ReturnsOne()
        {
            Assert.AreEqual(1f, TumbleMath.Smoothstep(1.5f), 0.0001f);
        }


        // ---- ComputeTiltAngle ----

        [Test]
        public void ComputeTiltAngle_Upright_ReturnsZero()
        {
            float angle = TumbleMath.ComputeTiltAngle(Vector3.up);
            Assert.AreEqual(0f, angle, 0.01f);
        }

        [Test]
        public void ComputeTiltAngle_Inverted_Returns180()
        {
            float angle = TumbleMath.ComputeTiltAngle(Vector3.down);
            Assert.AreEqual(180f, angle, 0.01f);
        }

        [Test]
        public void ComputeTiltAngle_OnSide_Returns90()
        {
            float angle = TumbleMath.ComputeTiltAngle(Vector3.right);
            Assert.AreEqual(90f, angle, 0.01f);
        }

        [Test]
        public void ComputeTiltAngle_At45Degrees_Returns45()
        {
            Vector3 tilted = new Vector3(0f, 1f, 1f).normalized;
            float angle = TumbleMath.ComputeTiltAngle(tilted);
            Assert.AreEqual(45f, angle, 0.1f);
        }
    }
}
