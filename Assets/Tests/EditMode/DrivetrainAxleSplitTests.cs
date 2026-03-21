using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Unit tests for DrivetrainMath.ComputeAxleSplit — open, ball diff, spool.</summary>
    public class DrivetrainAxleSplitTests
    {
        const int k_Open = 0;
        const int k_BallDiff = 1;
        const int k_Spool = 2;
        const float k_Preload = 5.0f;
        const float k_EngineForce = 20f;

        [Test]
        public void ComputeAxleSplit_OpenDiff_BothGrounded_Splits5050()
        {
            var split = DrivetrainMath.ComputeAxleSplit(k_EngineForce, true, true, 100f, 100f, k_Open, k_Preload);
            Assert.AreEqual(k_EngineForce * 0.5f, split.LeftShare, 0.01f);
            Assert.AreEqual(k_EngineForce * 0.5f, split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_OpenDiff_SpeedDifference_StillSplits5050()
        {
            var split = DrivetrainMath.ComputeAxleSplit(k_EngineForce, true, true, 200f, 50f, k_Open, k_Preload);
            Assert.AreEqual(k_EngineForce * 0.5f, split.LeftShare, 0.01f);
            Assert.AreEqual(k_EngineForce * 0.5f, split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_LeftOff_AllForceToRight()
        {
            var split = DrivetrainMath.ComputeAxleSplit(k_EngineForce, false, true, 0f, 100f, k_Open, k_Preload);
            Assert.AreEqual(0f, split.LeftShare, 0.01f);
            Assert.AreEqual(k_EngineForce, split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_RightOff_AllForceToLeft()
        {
            var split = DrivetrainMath.ComputeAxleSplit(k_EngineForce, true, false, 100f, 0f, k_Open, k_Preload);
            Assert.AreEqual(k_EngineForce, split.LeftShare, 0.01f);
            Assert.AreEqual(0f, split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_BothOff_Splits5050()
        {
            var split = DrivetrainMath.ComputeAxleSplit(k_EngineForce, false, false, 0f, 0f, k_Open, k_Preload);
            Assert.AreEqual(k_EngineForce * 0.5f, split.LeftShare, 0.01f);
            Assert.AreEqual(k_EngineForce * 0.5f, split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_BallDiff_EqualSpeed_Splits5050()
        {
            var split = DrivetrainMath.ComputeAxleSplit(k_EngineForce, true, true, 100f, 100f, k_BallDiff, k_Preload);
            Assert.AreEqual(k_EngineForce * 0.5f, split.LeftShare, 0.01f);
            Assert.AreEqual(k_EngineForce * 0.5f, split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_BallDiff_LeftSpinsFaster_TransfersToRight()
        {
            var split = DrivetrainMath.ComputeAxleSplit(k_EngineForce, true, true, 200f, 100f, k_BallDiff, k_Preload);
            Assert.Less(split.LeftShare, k_EngineForce * 0.5f);
            Assert.Greater(split.RightShare, k_EngineForce * 0.5f);
        }

        [Test]
        public void ComputeAxleSplit_BallDiff_CouplingClampedToPreload()
        {
            var split = DrivetrainMath.ComputeAxleSplit(k_EngineForce, true, true, 1000f, 0f, k_BallDiff, k_Preload);
            Assert.AreEqual(k_EngineForce * 0.5f - k_Preload, split.LeftShare, 0.01f);
            Assert.AreEqual(k_EngineForce * 0.5f + k_Preload, split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_Spool_LargeSpeedDiff_CouplingLargerThanBallDiff()
        {
            var ballSplit = DrivetrainMath.ComputeAxleSplit(k_EngineForce, true, true, 500f, 0f, k_BallDiff, k_Preload);
            var spoolSplit = DrivetrainMath.ComputeAxleSplit(k_EngineForce, true, true, 500f, 0f, k_Spool, k_Preload);
            float ballDelta = Mathf.Abs(ballSplit.RightShare - ballSplit.LeftShare);
            float spoolDelta = Mathf.Abs(spoolSplit.RightShare - spoolSplit.LeftShare);
            Assert.Greater(spoolDelta, ballDelta);
        }

        [Test]
        public void ComputeAxleSplit_TotalForceConserved_AllDiffTypes(
            [Values(k_Open, k_BallDiff, k_Spool)] int diffType)
        {
            var split = DrivetrainMath.ComputeAxleSplit(k_EngineForce, true, true, 150f, 80f, diffType, k_Preload);
            Assert.AreEqual(k_EngineForce, split.LeftShare + split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_NegativeForce_Reverse_StillConserved()
        {
            float reverseForce = -14.3f;
            var split = DrivetrainMath.ComputeAxleSplit(reverseForce, true, true, -50f, -50f, k_BallDiff, k_Preload);
            Assert.AreEqual(reverseForce, split.LeftShare + split.RightShare, 0.01f);
        }
    }
}
