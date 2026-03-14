using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for differential coupling and force distribution.
    /// Covers Open/BallDiff/Spool types, one-wheel-off, and AWD center diff.
    /// </summary>
    public class DrivetrainMathTests
    {
        const int k_Open = 0;
        const int k_BallDiff = 1;
        const int k_Spool = 2;
        const float k_Preload = 5.0f;
        const float k_EngineForce = 20f;


        // ---- Open Differential ----

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
            // Open diff ignores speed difference
            var split = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, true, true, 200f, 50f, k_Open, k_Preload);
            Assert.AreEqual(k_EngineForce * 0.5f, split.LeftShare, 0.01f);
            Assert.AreEqual(k_EngineForce * 0.5f, split.RightShare, 0.01f);
        }


        // ---- One Wheel Off Ground ----

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


        // ---- Ball Differential ----

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
            // Left spinning faster: coupling transfers force right → left gets less
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
            // Large speed difference — coupling should clamp at preload
            var split = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, true, true, 1000f, 0f, k_BallDiff, k_Preload);
            // Max coupling = preload = 5N
            // Left = 10 - 5 = 5, Right = 10 + 5 = 15
            Assert.AreEqual(k_EngineForce * 0.5f - k_Preload, split.LeftShare, 0.01f);
            Assert.AreEqual(k_EngineForce * 0.5f + k_Preload, split.RightShare, 0.01f);
        }


        // ---- Spool ----

        [Test]
        public void ComputeAxleSplit_Spool_LargeSpeedDiff_CouplingLargerThanBallDiff()
        {
            // Spool max coupling = |axleForce| * 0.5 = 10N (vs 5N preload)
            var ballSplit = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, true, true, 500f, 0f, k_BallDiff, k_Preload);
            var spoolSplit = DrivetrainMath.ComputeAxleSplit(
                k_EngineForce, true, true, 500f, 0f, k_Spool, k_Preload);
            float ballDelta = Mathf.Abs(ballSplit.RightShare - ballSplit.LeftShare);
            float spoolDelta = Mathf.Abs(spoolSplit.RightShare - spoolSplit.LeftShare);
            Assert.Greater(spoolDelta, ballDelta,
                "Spool should redistribute more force than ball diff");
        }


        // ---- Force Conservation ----

        [Test]
        public void ComputeAxleSplit_TotalForceConserved_AllDiffTypes(
            [Values(k_Open, k_BallDiff, k_Spool)] int diffType)
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


        // ---- Center Diff (AWD) ----

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
