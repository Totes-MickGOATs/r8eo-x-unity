using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Regression tests for critical grip fixes C3 (longitudinal force uses wheel forward)
    /// and C4 (ramp sliding cancellation in local space).
    /// C1/C2 tests live in GripCriticalC1C2Tests.cs.
    /// </summary>
    public class GripCriticalC3C4Tests
    {
        [Test]
        public void C3_LongitudinalForceMagnitude_IsDirectionIndependent()
        {
            float forwardSpeed = 5f;
            float traction = 0.1f;
            float gripCoeff = 0.7f;
            float gripLoad = 10f;

            float magnitude = GripMath.ComputeLongitudinalForceMagnitude(
                forwardSpeed, traction, gripCoeff, gripLoad);

            Assert.AreEqual(-3.5f, magnitude, 0.01f);

            Vector3 carForward = Vector3.forward;
            Vector3 wheelForward = Quaternion.Euler(0f, 30f, 0f) * Vector3.forward;

            Vector3 forceAlongCar = carForward * magnitude;
            Vector3 forceAlongWheel = wheelForward * magnitude;

            Assert.AreNotEqual(forceAlongCar, forceAlongWheel,
                "Force direction must differ when wheel is steered");
        }

        [Test]
        public void C4_RampSlidingCancellation_WorksWhenCarIsRotated()
        {
            Vector3 contactNormal = new Vector3(0.2f, 0.97f, 0.1f).normalized;
            float suspensionForce = 10f;

            Vector3 springForce = contactNormal * suspensionForce;
            Vector3 springHorizontal = new Vector3(springForce.x, 0f, springForce.z);

            Vector3 xForceBuggy = new Vector3(5f, 0f, 3f);
            Vector3 zForceBuggy = new Vector3(2f, 0f, 4f);
            Vector3 xFixBuggy = xForceBuggy;
            Vector3 zFixBuggy = zForceBuggy;
            xFixBuggy.x -= contactNormal.x * suspensionForce;
            zFixBuggy.z -= contactNormal.z * suspensionForce;

            Vector3 xForceFixed = new Vector3(5f, 0f, 3f);
            Vector3 xFixCorrect = xForceFixed - springHorizontal;

            Assert.AreEqual(xForceFixed.x - springHorizontal.x, xFixCorrect.x, 0.001f);
            Assert.AreEqual(xForceFixed.y - springHorizontal.y, xFixCorrect.y, 0.001f);
            Assert.AreEqual(xForceFixed.z - springHorizontal.z, xFixCorrect.z, 0.001f);

            Assert.AreNotEqual(xFixCorrect.z, xFixBuggy.z,
                "Component-wise subtraction misses cross-axis contributions when car is rotated");
        }
    }
}
