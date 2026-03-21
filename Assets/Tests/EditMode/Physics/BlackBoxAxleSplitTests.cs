#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for DrivetrainMath.ComputeAxleSplit.
    /// Center-diff tests live in BlackBoxCenterDiffTests.cs.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxAxleSplitTests
    {
        [Test]
        public void AxleSplit_OpenDiff_Always5050()
        {
            float force = 100f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, true, true, 500f, 200f, 0, 10f);
            Assert.AreEqual(force * 0.5f, split.LeftShare, k_Epsilon);
            Assert.AreEqual(force * 0.5f, split.RightShare, k_Epsilon);
        }

        [Test]
        public void AxleSplit_LeftWheelOff_AllForceToRight()
        {
            float force = 100f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, false, true, 0f, 500f, 0, 10f);
            Assert.AreEqual(0f, split.LeftShare, k_Epsilon);
            Assert.AreEqual(force, split.RightShare, k_Epsilon);
        }

        [Test]
        public void AxleSplit_RightWheelOff_AllForceToLeft()
        {
            float force = 100f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, true, false, 500f, 0f, 0, 10f);
            Assert.AreEqual(force, split.LeftShare, k_Epsilon);
            Assert.AreEqual(0f, split.RightShare, k_Epsilon);
        }

        [Test]
        public void AxleSplit_BothOff_5050()
        {
            float force = 100f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, false, false, 0f, 0f, 0, 10f);
            Assert.AreEqual(force * 0.5f, split.LeftShare, k_Epsilon);
            Assert.AreEqual(force * 0.5f, split.RightShare, k_Epsilon);
        }

        [Test]
        public void AxleSplit_BallDiff_EqualSpeed_5050()
        {
            float force = 100f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, true, true, 300f, 300f, 1, 20f);
            Assert.AreEqual(force * 0.5f, split.LeftShare, k_Epsilon);
            Assert.AreEqual(force * 0.5f, split.RightShare, k_Epsilon);
        }

        [Test]
        public void AxleSplit_BallDiff_LeftSpinningFaster_LessForceToLeft()
        {
            float force = 100f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, true, true, 600f, 300f, 1, 50f);
            Assert.Less(split.LeftShare, force * 0.5f,
                "Ball diff should send less force to the faster-spinning left wheel");
            Assert.Greater(split.RightShare, force * 0.5f,
                "Ball diff should send more force to the slower right wheel");
        }

        [Test]
        public void AxleSplit_BallDiff_CouplingClampedToPreload()
        {
            float force = 100f;
            float preload = 5f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, true, true, 10000f, 0f, 1, preload);
            Assert.AreEqual(force * 0.5f - preload, split.LeftShare, 0.01f);
            Assert.AreEqual(force * 0.5f + preload, split.RightShare, 0.01f);
        }

        [Test]
        public void AxleSplit_Spool_StrongerCouplingThanBallDiff()
        {
            float force = 100f;
            float preload = 5f;
            var ballSplit = DrivetrainMath.ComputeAxleSplit(
                force, true, true, 600f, 300f, 1, preload);
            var spoolSplit = DrivetrainMath.ComputeAxleSplit(
                force, true, true, 600f, 300f, 2, preload);

            float ballDelta = Mathf.Abs(ballSplit.LeftShare - ballSplit.RightShare);
            float spoolDelta = Mathf.Abs(spoolSplit.LeftShare - spoolSplit.RightShare);
            Assert.GreaterOrEqual(spoolDelta, ballDelta,
                "Spool should have stronger coupling than ball diff with small preload");
        }

        [Test]
        public void AxleSplit_ForceConserved_AllDiffTypes()
        {
            float force = 123.456f;
            int[] diffTypes = { 0, 1, 2 };
            foreach (int dt in diffTypes)
            {
                var split = DrivetrainMath.ComputeAxleSplit(
                    force, true, true, 500f, 300f, dt, 15f);
                Assert.AreEqual(force, split.LeftShare + split.RightShare, 0.01f,
                    $"Total force must be conserved for diff type {dt}");
            }
        }

        [Test]
        public void AxleSplit_NegativeForce_StillConserved()
        {
            float force = -80f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, true, true, 300f, 300f, 1, 10f);
            Assert.AreEqual(force, split.LeftShare + split.RightShare, 0.01f,
                "Negative force (reverse) must be conserved");
        }

        [Test]
        public void AxleSplit_ZeroForce_ZeroShares()
        {
            var split = DrivetrainMath.ComputeAxleSplit(
                0f, true, true, 500f, 300f, 1, 10f);
            Assert.AreEqual(0f, split.LeftShare, k_Epsilon);
            Assert.AreEqual(0f, split.RightShare, k_Epsilon);
        }

        [Test]
        public void AxleSplit_OneWheelOff_ForceConserved()
        {
            float force = 100f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, false, true, 0f, 500f, 2, 10f);
            Assert.AreEqual(force, split.LeftShare + split.RightShare, k_Epsilon,
                "Force must be conserved even with one wheel off ground");
        }
    }
}

#pragma warning restore CS0618
