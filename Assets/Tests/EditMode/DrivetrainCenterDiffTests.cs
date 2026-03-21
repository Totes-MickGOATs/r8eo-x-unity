using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Unit tests for DrivetrainMath.ComputeCenterDiffSplit (AWD center differential).</summary>
    public class DrivetrainCenterDiffTests
    {
        const int k_Open = 0;
        const int k_BallDiff = 1;
        const float k_EngineForce = 20f;

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
            Assert.AreEqual(k_EngineForce, front + rear, 0.01f);
        }

        [Test]
        public void ComputeCenterDiffSplit_FrontSpinsFaster_TransfersToRear()
        {
            float frontBias = 0.35f;
            var (front, rear) = DrivetrainMath.ComputeCenterDiffSplit(
                k_EngineForce, frontBias, 200f, 100f, k_BallDiff, 2f);
            Assert.Less(front, k_EngineForce * frontBias);
            Assert.Greater(rear, k_EngineForce * (1f - frontBias));
        }
    }
}
