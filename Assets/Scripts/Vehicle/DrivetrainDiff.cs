using UnityEngine;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Pure-static axle differential helper extracted from <see cref="Drivetrain"/>.
    /// Computes left/right wheel force shares and assigns them to the wheel objects.
    /// Uses the Vehicle-assembly diff stiffness constant (5000 N/RPM) rather than
    /// the Physics-assembly value so behaviour is identical to the original code.
    /// </summary>
    public static class DrivetrainDiff
    {
        /// <summary>
        /// Speed-to-force coupling stiffness (N per RPM difference).
        /// Exposed as public so <see cref="Drivetrain"/> can use it for the center diff.
        /// </summary>
        public const float DiffStiffness = 5000f;

        /// <summary>
        /// Compute left and right force shares for an axle given the diff parameters.
        /// Pure function — no side effects; wheels are not touched.
        /// </summary>
        public static (float leftShare, float rightShare) ComputeAxleShares(
            float axleForce,
            bool leftOnGround, bool rightOnGround,
            float leftRpm, float rightRpm,
            Drivetrain.DiffType diffType,
            float couplingPreload)
        {
            if (!leftOnGround && rightOnGround)
                return (0f, axleForce);
            if (leftOnGround && !rightOnGround)
                return (axleForce, 0f);

            float leftShare  = axleForce * 0.5f;
            float rightShare = axleForce * 0.5f;

            if (diffType != Drivetrain.DiffType.Open && leftOnGround && rightOnGround)
            {
                float speedDiff    = leftRpm - rightRpm;
                float maxCoupling  = diffType == Drivetrain.DiffType.BallDiff
                    ? couplingPreload
                    : Mathf.Abs(axleForce) * 0.5f;
                float coupling = Mathf.Clamp(speedDiff * DiffStiffness, -maxCoupling, maxCoupling);
                leftShare  -= coupling;
                rightShare += coupling;
            }

            return (leftShare, rightShare);
        }

        /// <summary>
        /// Compute shares and assign <see cref="RaycastWheel.MotorForceShare"/> on both wheels.
        /// </summary>
        public static void Apply(RaycastWheel left, RaycastWheel right,
                                 float axleForce,
                                 Drivetrain.DiffType diffType,
                                 float couplingPreload)
        {
            var (ls, rs) = ComputeAxleShares(
                axleForce,
                left.IsOnGround, right.IsOnGround,
                left.WheelRpm, right.WheelRpm,
                diffType, couplingPreload);

            left.MotorForceShare  = ls;
            right.MotorForceShare = rs;
        }
    }
}
