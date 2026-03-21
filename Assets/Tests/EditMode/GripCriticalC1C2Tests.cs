using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Regression tests for critical grip fixes C1 (grip curve zero-slip) and
    /// C2 (gripLoad includes damping). C3/C4 tests live in GripCriticalC3C4Tests.cs.
    /// </summary>
    public class GripCriticalC1C2Tests
    {
        [Test]
        public void C1_DefaultGripCurve_AtZeroSlip_ReturnsPositiveGrip()
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 0.3f),
                new Keyframe(0.15f, 0.8f),
                new Keyframe(0.4f, 1.0f),
                new Keyframe(1.0f, 0.7f)
            );

            float gripAtZero = curve.Evaluate(0f);

            Assert.Greater(gripAtZero, 0f,
                "Grip factor at zero slip must be > 0 for baseline lateral resistance");
            Assert.AreEqual(0.3f, gripAtZero, 0.01f,
                "Grip factor at zero slip should be 0.3");
        }

        [Test]
        public void C2_ComputeGripLoadFromSuspensionForce_IncludesDamping()
        {
            float suspensionForce = 12.5f;
            float maxSpringForce = 50f;

            float gripLoad = SuspensionMath.ComputeGripLoadFromSuspensionForce(
                suspensionForce, maxSpringForce);

            Assert.AreEqual(12.5f, gripLoad, 0.01f,
                "Grip load should equal the damped suspension force when below max");
        }

        [Test]
        public void C2_ComputeGripLoadFromSuspensionForce_ClampsToMax()
        {
            float suspensionForce = 80f;
            float maxSpringForce = 50f;

            float gripLoad = SuspensionMath.ComputeGripLoadFromSuspensionForce(
                suspensionForce, maxSpringForce);

            Assert.AreEqual(50f, gripLoad, 0.01f,
                "Grip load should clamp to maxSpringForce");
        }

        [Test]
        public void C2_ComputeGripLoadFromSuspensionForce_NegativeForce_ReturnsZero()
        {
            float suspensionForce = -5f;
            float maxSpringForce = 50f;

            float gripLoad = SuspensionMath.ComputeGripLoadFromSuspensionForce(
                suspensionForce, maxSpringForce);

            Assert.AreEqual(0f, gripLoad, 0.01f,
                "Grip load should never be negative");
        }
    }
}
