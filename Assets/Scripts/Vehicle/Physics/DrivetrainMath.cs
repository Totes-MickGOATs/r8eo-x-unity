using UnityEngine;

namespace R8EOX.Vehicle.Physics
{
    /// <summary>
    /// Pure math functions for differential coupling and force distribution.
    /// </summary>
    public static class DrivetrainMath
    {
        const float k_DiffStiffness = 500f;

        /// <summary>
        /// Result of axle differential force distribution.
        /// </summary>
        public struct AxleSplit
        {
            public float LeftShare;
            public float RightShare;
        }

        /// <summary>
        /// Distribute force across an axle using the specified differential type.
        /// Handles one-wheel-off-ground routing automatically.
        /// </summary>
        /// <param name="axleForce">Total force to distribute (N)</param>
        /// <param name="leftOnGround">Whether left wheel has ground contact</param>
        /// <param name="rightOnGround">Whether right wheel has ground contact</param>
        /// <param name="leftRpm">Left wheel RPM</param>
        /// <param name="rightRpm">Right wheel RPM</param>
        /// <param name="diffType">Differential type (0=Open, 1=BallDiff, 2=Spool)</param>
        /// <param name="couplingPreload">Max coupling force for ball diff (N)</param>
        /// <returns>Left and right force shares</returns>
        public static AxleSplit ComputeAxleSplit(
            float axleForce, bool leftOnGround, bool rightOnGround,
            float leftRpm, float rightRpm,
            int diffType, float couplingPreload)
        {
            // One wheel off ground: all force to grounded wheel
            if (!leftOnGround && rightOnGround)
                return new AxleSplit { LeftShare = 0f, RightShare = axleForce };
            if (leftOnGround && !rightOnGround)
                return new AxleSplit { LeftShare = axleForce, RightShare = 0f };

            // Both off ground or both on ground: start with 50/50
            float leftShare = axleForce * 0.5f;
            float rightShare = axleForce * 0.5f;

            // Apply coupling for non-open diffs when both wheels are grounded
            if (diffType != 0 && leftOnGround && rightOnGround) // 0 = Open
            {
                float speedDiff = leftRpm - rightRpm;
                float maxCoupling = diffType == 1 // 1 = BallDiff
                    ? couplingPreload
                    : Mathf.Abs(axleForce) * 0.5f; // Spool
                float coupling = Mathf.Clamp(speedDiff * k_DiffStiffness, -maxCoupling, maxCoupling);
                leftShare -= coupling;
                rightShare += coupling;
            }

            return new AxleSplit { LeftShare = leftShare, RightShare = rightShare };
        }

        /// <summary>
        /// Compute center differential coupling for AWD layouts.
        /// Returns adjusted front and rear forces after coupling.
        /// </summary>
        /// <param name="engineForce">Total engine force (N)</param>
        /// <param name="frontBias">Front bias ratio (0-1, e.g. 0.35 = 35% front)</param>
        /// <param name="frontAvgRpm">Average front axle RPM</param>
        /// <param name="rearAvgRpm">Average rear axle RPM</param>
        /// <param name="diffType">Center diff type (0=Open, 1=BallDiff, 2=Spool)</param>
        /// <param name="preload">Center diff preload (N)</param>
        /// <returns>(frontForce, rearForce) tuple</returns>
        public static (float frontForce, float rearForce) ComputeCenterDiffSplit(
            float engineForce, float frontBias,
            float frontAvgRpm, float rearAvgRpm,
            int diffType, float preload)
        {
            float frontForce = engineForce * frontBias;
            float rearForce = engineForce * (1f - frontBias);

            if (diffType != 0) // Not Open
            {
                float centerSpeedDiff = frontAvgRpm - rearAvgRpm;
                float maxCoupling = diffType == 1 // BallDiff
                    ? preload
                    : Mathf.Abs(engineForce) * 0.5f;
                float coupling = Mathf.Clamp(centerSpeedDiff * k_DiffStiffness, -maxCoupling, maxCoupling);
                frontForce -= coupling;
                rearForce += coupling;
            }

            return (frontForce, rearForce);
        }
    }
}
