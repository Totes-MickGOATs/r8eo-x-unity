using NUnit.Framework;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Grip curve bug-detection tests (documents current vs expected behavior).
    /// Pure math — no MonoBehaviour instantiation needed.
    /// </summary>
    public class ForceDirectionGripCurveTests
    {
        [Test]
        public void GripCurve_ZeroSlip_ReturnsValue()
        {
            var gripCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.15f, 0.8f),
                new Keyframe(0.4f, 1.0f),
                new Keyframe(1.0f, 0.7f)
            );

            float gripAtZero = gripCurve.Evaluate(0f);

            Assert.AreEqual(0f, gripAtZero, 0.01f,
                "FINDING: Default grip curve returns 0 at slip=0 — car has no lateral grip " +
                "when moving straight. This is likely a bug; the curve should start near 1.0");
        }

        [Test]
        public void GripCurve_SmallSlip_ReturnsNonZero()
        {
            var gripCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.15f, 0.8f),
                new Keyframe(0.4f, 1.0f),
                new Keyframe(1.0f, 0.7f)
            );

            float gripAtSmallSlip = gripCurve.Evaluate(0.05f);

            Assert.Greater(gripAtSmallSlip, 0f,
                "Grip at small slip angle must be non-zero for responsive handling");
        }
    }
}
