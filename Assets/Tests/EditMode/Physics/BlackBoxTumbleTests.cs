#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for TumbleMath public functions.
    /// Tests verify physically correct behavior from inputs/outputs only.
    /// Uses realistic 1/10th scale RC car values throughout.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxTumbleTests
    {
        // =====================================================================
        // TumbleMath — ComputeTumbleFactor
        // =====================================================================

        [Test]
        public void TumbleFactor_Upright_Zero()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 0f, isAirborne: false, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(0f, factor, k_Epsilon,
                "Upright car (0 degrees) should have zero tumble factor");
        }

        [Test]
        public void TumbleFactor_BelowEngageAngle_Zero()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 30f, isAirborne: false, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(0f, factor, k_Epsilon,
                "Below engage angle should return zero");
        }

        [Test]
        public void TumbleFactor_AtFullAngle_One()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 90f, isAirborne: false, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(1f, factor, k_Epsilon,
                "At full angle should return 1.0");
        }

        [Test]
        public void TumbleFactor_BeyondFullAngle_ClampedToOne()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 120f, isAirborne: false, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(1f, factor, k_Epsilon,
                "Beyond full angle should be clamped to 1.0");
        }

        [Test]
        public void TumbleFactor_Airborne_AlwaysZero()
        {
            // Even at extreme tilt, airborne should return 0
            float factor = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 90f, isAirborne: true, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(0f, factor, k_Epsilon,
                "Airborne should ALWAYS return zero regardless of tilt");
        }

        [Test]
        public void TumbleFactor_Airborne_WasTumbling_StillZero()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 90f, isAirborne: true, wasTumbling: true,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(0f, factor, k_Epsilon,
                "Airborne should override tumbling state and return zero");
        }

        [Test]
        public void TumbleFactor_HysteresisLowersThresholdWhenTumbling()
        {
            // Without hysteresis (not tumbling): engage=45, so 43 degrees = 0
            float factorNotTumbling = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 43f, isAirborne: false, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(0f, factorNotTumbling, k_Epsilon,
                "Not tumbling: 43 deg below engage 45 should be zero");

            // With hysteresis (was tumbling): effective engage = 45-5 = 40, so 43 > 40
            float factorWasTumbling = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 43f, isAirborne: false, wasTumbling: true,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.Greater(factorWasTumbling, 0f,
                "Was tumbling: 43 deg above effective engage 40 should be nonzero (hysteresis)");
        }

        [Test]
        public void TumbleFactor_HysteresisPreventsOscillation()
        {
            // At exactly the engage angle, toggling wasTumbling should produce different results
            float factorOff = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 44f, isAirborne: false, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            float factorOn = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 44f, isAirborne: false, wasTumbling: true,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(0f, factorOff, k_Epsilon,
                "Not tumbling at 44 deg (below 45 engage) should be zero");
            Assert.Greater(factorOn, 0f,
                "Was tumbling at 44 deg (above 40 effective engage) should be nonzero");
        }

        [Test]
        public void TumbleFactor_MidpointUsesSmoothing()
        {
            // Midpoint between engage (45) and full (90) = 67.5 degrees
            // t = (67.5 - 45) / (90 - 45) = 0.5
            // smoothstep(0.5) = 0.5
            float factor = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 67.5f, isAirborne: false, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 0f);
            Assert.AreEqual(0.5f, factor, 0.01f,
                "Midpoint should give smoothstep(0.5) = 0.5");
        }


    }
}

#pragma warning restore CS0618
