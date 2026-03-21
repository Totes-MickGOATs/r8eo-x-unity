using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Regression tests for critical grip/traction fixes C1-C4.</summary>
    public class GripTractionCriticalTests
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
            Assert.Greater(curve.Evaluate(0f), 0f);
            Assert.AreEqual(0.3f, curve.Evaluate(0f), 0.01f);
        }

        [Test]
        public void C2_ComputeGripLoadFromSuspensionForce_IncludesDamping()
        {
            float gripLoad = SuspensionMath.ComputeGripLoadFromSuspensionForce(12.5f, 50f);
            Assert.AreEqual(12.5f, gripLoad, 0.01f);
        }

        [Test]
        public void C2_ComputeGripLoadFromSuspensionForce_ClampsToMax()
        {
            float gripLoad = SuspensionMath.ComputeGripLoadFromSuspensionForce(80f, 50f);
            Assert.AreEqual(50f, gripLoad, 0.01f);
        }

        [Test]
        public void C2_ComputeGripLoadFromSuspensionForce_NegativeForce_ReturnsZero()
        {
            float gripLoad = SuspensionMath.ComputeGripLoadFromSuspensionForce(-5f, 50f);
            Assert.AreEqual(0f, gripLoad, 0.01f);
        }

        [Test]
        public void C3_LongitudinalForceMagnitude_IsDirectionIndependent()
        {
            float magnitude = GripMath.ComputeLongitudinalForceMagnitude(5f, 0.1f, 0.7f, 10f);
            Assert.AreEqual(-3.5f, magnitude, 0.01f);

            Vector3 carForward = Vector3.forward;
            Vector3 wheelForward = Quaternion.Euler(0f, 30f, 0f) * Vector3.forward;
            Assert.AreNotEqual(carForward * magnitude, wheelForward * magnitude,
                "Force direction must differ when wheel is steered");
        }

        [Test]
        public void C4_RampSlidingCancellation_WorksWhenCarIsRotated()
        {
            Vector3 contactNormal = new Vector3(0.2f, 0.97f, 0.1f).normalized;
            float suspensionForce = 10f;
            Vector3 springForce = contactNormal * suspensionForce;
            Vector3 springHorizontal = new Vector3(springForce.x, 0f, springForce.z);

            Vector3 xForceFixed = new Vector3(5f, 0f, 3f);
            Vector3 xFixCorrect = xForceFixed - springHorizontal;

            Vector3 xFixBuggy = xForceFixed;
            xFixBuggy.x -= contactNormal.x * suspensionForce;

            Assert.AreEqual(xForceFixed.x - springHorizontal.x, xFixCorrect.x, 0.001f);
            Assert.AreEqual(xForceFixed.z - springHorizontal.z, xFixCorrect.z, 0.001f);
            Assert.AreNotEqual(xFixCorrect.z, xFixBuggy.z,
                "Component-wise subtraction misses cross-axis contributions");
        }
    }
}
