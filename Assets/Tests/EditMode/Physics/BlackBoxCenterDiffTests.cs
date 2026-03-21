#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for DrivetrainMath.ComputeCenterDiffSplit.
    /// Axle-split tests live in BlackBoxAxleSplitTests.cs.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxCenterDiffTests
    {
        [Test]
        public void CenterDiff_Open_UsesBiasDirectly()
        {
            var (front, rear) = DrivetrainMath.ComputeCenterDiffSplit(100f, 0.35f, 300f, 300f, 0, 10f);
            Assert.AreEqual(100f * 0.35f, front, k_Epsilon);
            Assert.AreEqual(100f * 0.65f, rear, k_Epsilon);
        }

        [Test]
        public void CenterDiff_ForceConserved()
        {
            var (front, rear) = DrivetrainMath.ComputeCenterDiffSplit(200f, 0.4f, 500f, 300f, 1, 15f);
            Assert.AreEqual(200f, front + rear, 0.01f,
                "Center diff must conserve total force (front + rear = input)");
        }

        [Test]
        public void CenterDiff_FrontSpinningFaster_MoreForceToRear()
        {
            var (front, rear) = DrivetrainMath.ComputeCenterDiffSplit(100f, 0.5f, 600f, 300f, 1, 50f);
            Assert.Less(front, 100f * 0.5f, "Front spinning faster should transfer force away from front");
            Assert.Greater(rear, 100f * 0.5f, "Front spinning faster should transfer force toward rear");
        }

        [Test]
        public void CenterDiff_EqualRpm_NoCouplingEffect()
        {
            var (front, rear) = DrivetrainMath.ComputeCenterDiffSplit(100f, 0.4f, 300f, 300f, 1, 20f);
            Assert.AreEqual(100f * 0.4f, front, k_Epsilon, "Equal RPM should produce no coupling effect");
            Assert.AreEqual(100f * 0.6f, rear, k_Epsilon);
        }

        [Test]
        public void CenterDiff_ZeroEngine_ZeroOutput()
        {
            var (front, rear) = DrivetrainMath.ComputeCenterDiffSplit(0f, 0.35f, 300f, 300f, 1, 10f);
            Assert.AreEqual(0f, front, k_Epsilon);
            Assert.AreEqual(0f, rear, k_Epsilon);
        }
    }
}

#pragma warning restore CS0618
