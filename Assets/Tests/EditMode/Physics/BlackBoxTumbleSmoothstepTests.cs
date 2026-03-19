#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for TumbleMath.Smoothstep and ComputeTiltAngle.
    /// Extracted from BlackBoxTumbleTests to keep each file under 200 lines.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxTumbleSmoothstepTests
    {
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
