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


        // =====================================================================
        // TumbleMath — Smoothstep
        // =====================================================================

        [Test]
        public void Smoothstep_ZeroReturnsZero()
        {
            Assert.AreEqual(0f, TumbleMath.Smoothstep(0f), k_Epsilon);
        }

        [Test]
        public void Smoothstep_HalfReturnsHalf()
        {
            // 3*(0.5)^2 - 2*(0.5)^3 = 0.75 - 0.25 = 0.5
            Assert.AreEqual(0.5f, TumbleMath.Smoothstep(0.5f), k_Epsilon);
        }

        [Test]
        public void Smoothstep_OneReturnsOne()
        {
            Assert.AreEqual(1f, TumbleMath.Smoothstep(1f), k_Epsilon);
        }

        [Test]
        public void Smoothstep_BelowZero_ClampsToZero()
        {
            Assert.AreEqual(0f, TumbleMath.Smoothstep(-0.5f), k_Epsilon,
                "Negative input should clamp to 0");
        }

        [Test]
        public void Smoothstep_AboveOne_ClampsToOne()
        {
            Assert.AreEqual(1f, TumbleMath.Smoothstep(1.5f), k_Epsilon,
                "Input above 1 should clamp to 1");
        }

        [Test]
        public void Smoothstep_MonotonicallyIncreasing()
        {
            float v25 = TumbleMath.Smoothstep(0.25f);
            float v50 = TumbleMath.Smoothstep(0.50f);
            float v75 = TumbleMath.Smoothstep(0.75f);
            Assert.Less(v25, v50, "0.25 < 0.50");
            Assert.Less(v50, v75, "0.50 < 0.75");
        }

        [Test]
        public void Smoothstep_SymmetricAroundHalf()
        {
            // smoothstep(t) + smoothstep(1-t) = 1 for Hermite smoothstep
            float a = TumbleMath.Smoothstep(0.25f);
            float b = TumbleMath.Smoothstep(0.75f);
            Assert.AreEqual(1f, a + b, k_Epsilon,
                "Hermite smoothstep should be symmetric: f(t) + f(1-t) = 1");
        }


        // =====================================================================
        // TumbleMath — ComputeTiltAngle
        // =====================================================================

        [Test]
        public void TiltAngle_VectorUp_ZeroDegrees()
        {
            float angle = TumbleMath.ComputeTiltAngle(Vector3.up);
            Assert.AreEqual(0f, angle, 0.01f,
                "Upright car (up vector = world up) should be 0 degrees");
        }

        [Test]
        public void TiltAngle_VectorDown_180Degrees()
        {
            float angle = TumbleMath.ComputeTiltAngle(Vector3.down);
            Assert.AreEqual(180f, angle, 0.01f,
                "Inverted car (up vector = world down) should be 180 degrees");
        }

        [Test]
        public void TiltAngle_VectorRight_90Degrees()
        {
            float angle = TumbleMath.ComputeTiltAngle(Vector3.right);
            Assert.AreEqual(90f, angle, 0.01f,
                "Car on its side (up = world right) should be 90 degrees");
        }

        [Test]
        public void TiltAngle_45DegreeTilt()
        {
            // A vector tilted 45 degrees from up toward forward
            Vector3 tilted = new Vector3(0f, 1f, 1f).normalized;
            float angle = TumbleMath.ComputeTiltAngle(tilted);
            Assert.AreEqual(45f, angle, 0.1f,
                "45-degree tilt should report approximately 45 degrees");
        }

        [Test]
        public void TiltAngle_VectorForward_90Degrees()
        {
            float angle = TumbleMath.ComputeTiltAngle(Vector3.forward);
            Assert.AreEqual(90f, angle, 0.01f,
                "Car nose-up (up = forward) should be 90 degrees");
        }
    }
}

#pragma warning restore CS0618
