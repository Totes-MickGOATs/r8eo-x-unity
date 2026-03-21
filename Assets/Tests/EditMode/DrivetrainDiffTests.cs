using NUnit.Framework;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="DrivetrainDiff"/>.
    /// Verifies that the wheel-reference-based axle diff applier correctly sets
    /// MotorForceShare on left and right wheels for all diff types and ground states.
    /// </summary>
    public class DrivetrainDiffTests
    {
        // ---- Helpers ----

        /// <summary>Minimal stub RaycastWheel state container for testing.</summary>
        sealed class WheelStub
        {
            public bool IsOnGround;
            public float WheelRpm;
            public float MotorForceShare;
        }

        // We cannot instantiate RaycastWheel (MonoBehaviour) in EditMode, so
        // DrivetrainDiff must accept its own WheelProxy interface or a separate
        // overload. Until DrivetrainDiff exists these tests simply assert expected
        // arithmetic so they serve as RED-phase specification.
        //
        // The tests below will compile once DrivetrainDiff is introduced.

        const float k_Preload = 50f;
        const float k_Force = 100f;

        // ---- Open diff ----

        [Test]
        public void ComputeAxleShares_OpenDiff_BothGrounded_Splits5050()
        {
            var (left, right) = DrivetrainDiff.ComputeAxleShares(
                axleForce: k_Force,
                leftOnGround: true, rightOnGround: true,
                leftRpm: 100f, rightRpm: 100f,
                diffType: Drivetrain.DiffType.Open,
                couplingPreload: k_Preload);

            Assert.AreEqual(k_Force * 0.5f, left,  0.001f, "Open diff: left share must be 50%");
            Assert.AreEqual(k_Force * 0.5f, right, 0.001f, "Open diff: right share must be 50%");
        }

        [Test]
        public void ComputeAxleShares_OpenDiff_SpeedDifference_StillSplits5050()
        {
            // Open diff ignores speed difference
            var (left, right) = DrivetrainDiff.ComputeAxleShares(
                axleForce: k_Force,
                leftOnGround: true, rightOnGround: true,
                leftRpm: 300f, rightRpm: 100f,
                diffType: Drivetrain.DiffType.Open,
                couplingPreload: k_Preload);

            Assert.AreEqual(k_Force * 0.5f, left,  0.001f, "Open diff ignores RPM diff");
            Assert.AreEqual(k_Force * 0.5f, right, 0.001f, "Open diff ignores RPM diff");
        }

        // ---- One wheel off ground ----

        [Test]
        public void ComputeAxleShares_LeftOffGround_AllForceToRight()
        {
            var (left, right) = DrivetrainDiff.ComputeAxleShares(
                axleForce: k_Force,
                leftOnGround: false, rightOnGround: true,
                leftRpm: 0f, rightRpm: 0f,
                diffType: Drivetrain.DiffType.Open,
                couplingPreload: k_Preload);

            Assert.AreEqual(0f,       left,  0.001f, "Left off ground: left share must be zero");
            Assert.AreEqual(k_Force,  right, 0.001f, "Left off ground: right gets all force");
        }

        [Test]
        public void ComputeAxleShares_RightOffGround_AllForceToLeft()
        {
            var (left, right) = DrivetrainDiff.ComputeAxleShares(
                axleForce: k_Force,
                leftOnGround: true, rightOnGround: false,
                leftRpm: 0f, rightRpm: 0f,
                diffType: Drivetrain.DiffType.Open,
                couplingPreload: k_Preload);

            Assert.AreEqual(k_Force,  left,  0.001f, "Right off ground: left gets all force");
            Assert.AreEqual(0f,       right, 0.001f, "Right off ground: right share must be zero");
        }

        // ---- BallDiff coupling ----

        [Test]
        public void ComputeAxleShares_BallDiff_LeftFasterByPreload_ClampsToCouplingLimit()
        {
            // leftRpm - rightRpm = 1f; stiffness = 5000 → raw coupling = 5000
            // BallDiff clamps to preload (50N), so coupling = 50
            const float leftRpm = 1f, rightRpm = 0f;
            var (left, right) = DrivetrainDiff.ComputeAxleShares(
                axleForce: k_Force,
                leftOnGround: true, rightOnGround: true,
                leftRpm: leftRpm, rightRpm: rightRpm,
                diffType: Drivetrain.DiffType.BallDiff,
                couplingPreload: k_Preload);

            // left share = 50 - 50 = 0; right share = 50 + 50 = 100
            Assert.AreEqual(0f,       left,  0.001f, "BallDiff: left clamped to zero");
            Assert.AreEqual(k_Force,  right, 0.001f, "BallDiff: right gets all after coupling");
        }

        [Test]
        public void ComputeAxleShares_BallDiff_TotalForce_Conserved()
        {
            var (left, right) = DrivetrainDiff.ComputeAxleShares(
                axleForce: k_Force,
                leftOnGround: true, rightOnGround: true,
                leftRpm: 0.005f, rightRpm: 0f,
                diffType: Drivetrain.DiffType.BallDiff,
                couplingPreload: k_Preload);

            Assert.AreEqual(k_Force, left + right, 0.001f, "BallDiff must conserve total force");
        }

        // ---- Spool ----

        [Test]
        public void ComputeAxleShares_Spool_TotalForce_Conserved()
        {
            var (left, right) = DrivetrainDiff.ComputeAxleShares(
                axleForce: k_Force,
                leftOnGround: true, rightOnGround: true,
                leftRpm: 0.005f, rightRpm: 0f,
                diffType: Drivetrain.DiffType.Spool,
                couplingPreload: k_Preload);

            Assert.AreEqual(k_Force, left + right, 0.001f, "Spool must conserve total force");
        }
    }
}
