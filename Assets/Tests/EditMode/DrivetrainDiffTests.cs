using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for DrivetrainMath axle-split differential types (Open, BallDiff, Spool).
    /// One-wheel-off and force-conservation tests live in DrivetrainConservationTests.cs.
    /// Center-diff tests live in DrivetrainCenterDiffTests.cs.
    /// </summary>
    public class DrivetrainDiffTests
    {
        const int k_Open = 0;
        const int k_BallDiff = 1;
        const int k_Spool = 2;
        const float k_Preload = 5.0f;
        const float k_EngineForce = 20f;

        [Test]
        public void ComputeAxleSplit_OpenDiff_BothGrounded_Splits5050()
        {
            var split = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, true, true, 100f, 100f, k_Open, k_Preload);
            Assert.AreEqual(k_EngineForce * 0.5f, split.LeftShare, 0.01f);
            Assert.AreEqual(k_EngineForce * 0.5f, split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_OpenDiff_SpeedDifference_StillSplits5050()
        {
            var split = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, true, true, 200f, 50f, k_Open, k_Preload);
            Assert.AreEqual(k_EngineForce * 0.5f, split.LeftShare, 0.01f);
            Assert.AreEqual(k_EngineForce * 0.5f, split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_BallDiff_EqualSpeed_Splits5050()
        {
            var split = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, true, true, 100f, 100f, k_BallDiff, k_Preload);
            Assert.AreEqual(k_EngineForce * 0.5f, split.LeftShare, 0.01f);
            Assert.AreEqual(k_EngineForce * 0.5f, split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_BallDiff_LeftSpinsFaster_TransfersToRight()
        {
            var split = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, true, true, 200f, 100f, k_BallDiff, k_Preload);
            Assert.Less(split.LeftShare, k_EngineForce * 0.5f,
                "Left share should decrease when left spins faster");
            Assert.Greater(split.RightShare, k_EngineForce * 0.5f,
                "Right share should increase when left spins faster");
        }

        [Test]
        public void ComputeAxleSplit_BallDiff_CouplingClampedToPreload()
        {
            var split = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, true, true, 1000f, 0f, k_BallDiff, k_Preload);
            Assert.AreEqual(k_EngineForce * 0.5f - k_Preload, split.LeftShare, 0.01f);
            Assert.AreEqual(k_EngineForce * 0.5f + k_Preload, split.RightShare, 0.01f);
        }

        [Test]
        public void ComputeAxleSplit_Spool_LargeSpeedDiff_CouplingLargerThanBallDiff()
        {
            var ballSplit = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, true, true, 500f, 0f, k_BallDiff, k_Preload);
            var spoolSplit = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, true, true, 500f, 0f, k_Spool, k_Preload);
            float ballDelta = Mathf.Abs(ballSplit.RightShare - ballSplit.LeftShare);
            float spoolDelta = Mathf.Abs(spoolSplit.RightShare - spoolSplit.LeftShare);
            Assert.Greater(spoolDelta, ballDelta,
                "Spool should redistribute more force than ball diff");
        }
    }
}
