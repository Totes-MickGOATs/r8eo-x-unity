using NUnit.Framework;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Documents grip-curve behavior findings for the default AnimationCurve.
    /// Linear force-direction tests live in ForceDirectionLinearTests.cs.
    /// </summary>
    public class ForceDirectionGripCurveTests
    {
        [Test]
        public void GripCurve_ZeroSlip_ReturnsValue()
        {
            // The default grip curve has keyframe (0, 0) — this means at zero slip,
            // grip factor is ZERO. This is a finding: the car has no grip when
            // moving perfectly straight because the curve starts at 0.
            // A correct curve should return ~1.0 at slip=0 for maximum grip.
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
            // At slip=0.05, grip should be meaningful (not near-zero)
            // With the current curve (0,0)→(0.15,0.8), interpolation at 0.05
            // gives a very small value, meaning the car has almost no grip
            // at small slip angles. This contributes to the "skating" feel.
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
