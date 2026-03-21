using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for DrivetrainMath one-wheel-off scenarios, force conservation,
    /// and center-diff (AWD) split.
    /// Differential-type tests live in DrivetrainDiffTests.cs.
    /// </summary>
    public class DrivetrainConservationTests
    {
        const int k_Open = 0;
        const int k_BallDiff = 1;
        const int k_Spool = 2;
        const float k_Preload = 5.0f;
        const float k_EngineForce = 20f;

        [Test]
        public void ComputeAxleSplit_LeftOff_AllForceToRight()
        {
            var split = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, false, true, 0f, 100f, k_Open, k_Preload);
            Assert.AreEqual(0f, split.LeftShare, 0.01f);
            Assert.AreEqual(k_EngineForce, split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_RightOff_AllForceToLeft()
        {
            var split = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, true, false, 100f, 0f, k_Open, k_Preload);
            Assert.AreEqual(k_EngineForce, split.LeftShare, 0.01f);
            Assert.AreEqual(0f, split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_BothOff_Splits5050()
        {
            var split = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, false, false, 0f, 0f, k_Open, k_Preload);
            Assert.AreEqual(k_EngineForce * 0.5f, split.LeftShare, 0.01f);
            Assert.AreEqual(k_EngineForce * 0.5f, split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_TotalForceConserved_AllDiffTypes(
            [Values(0, 1, 2)] int diffType)
        {
            var split = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, true, true, 150f, 80f, diffType, k_Preload);
            Assert.AreEqual(k_EngineForce, split.LeftShare + split.RightShare, 0.01f,
                "Total force must be conserved across axle");
        }

        [Test]
        public void ComputeAxleSplit_NegativeForce_Reverse_StillConserved()
        {
            float reverseForce = -14.3f;
            var split = DrivetrainMath.ComputeAxleSplit(
                reverseForce, true, true, -50f, -50f, k_BallDiff, k_Preload);
            Assert.AreEqual(reverseForce, split.LeftShare + split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeCenterDiffSplit_OpenCenter_UseBiasDirectly()
        {
            float frontBias = 0.35f;
            var (front, rear) = DrivetrainMath.ComputeCenterDiffSplit(
                k_EngineForce, frontBias, 100f, 100f, k_Open, 2f);
            Assert.AreEqual(k_EngineForce * frontBias, front, 0.01f);
            Assert.AreEqual(k_EngineForce * (1f - frontBias), rear, 0.01f);
        }

        [Test]
        public void ComputeCenterDiffSplit_TotalForceConserved()
        {
            var (front, rear) = DrivetrainMath.ComputeCenterDiffSplit(
                k_EngineForce, 0.35f, 200f, 100f, k_BallDiff, 2f);
            Assert.AreEqual(k_EngineForce, front + rear, 0.01f,
                "Center diff must conserve total force");
        }

        [Test]
        public void ComputeCenterDiffSplit_FrontSpinsFaster_TransfersToRear()
        {
            float frontBias = 0.35f;
            var (front, rear) = DrivetrainMath.ComputeCenterDiffSplit(
                k_EngineForce, frontBias, 200f, 100f, k_BallDiff, 2f);
            Assert.Less(front, k_EngineForce * frontBias,
                "Front should get less force when spinning faster");
            Assert.Greater(rear, k_EngineForce * (1f - frontBias),
                "Rear should get more force when front spins faster");
        }
    }
}
