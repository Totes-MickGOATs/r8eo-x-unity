using UnityEngine;

/// <summary>
/// Differential coupling and drive layout logic.
/// Ported 1:1 from Godot drivetrain.gd — scalar math only, no coordinate concerns.
/// </summary>
public class Drivetrain : MonoBehaviour
{
    public enum DiffType { Open, BallDiff, Spool }
    public enum DriveLayout { RWD, AWD }

    [Header("Drivetrain")]
    public DriveLayout driveLayout = DriveLayout.RWD;

    [Header("Rear Differential")]
    public DiffType rearDiffType = DiffType.Open;
    [Tooltip("Max coupling force the rear ball diff transfers between wheels (N)")]
    public float rearPreload = 5.0f;

    [Header("Front Differential")]
    public DiffType frontDiffType = DiffType.Open;
    public float frontPreload = 1.0f;

    [Header("Center Differential (AWD only)")]
    public DiffType centerDiffType = DiffType.Open;
    public float centerPreload = 2.0f;
    [Tooltip("0.35 = 35% front / 65% rear torque split")]
    public float centerFrontBias = 0.35f;

    const float DIFF_STIFFNESS = 500f;

    /// <summary>
    /// Set is_motor on front/rear wheels based on drive layout.
    /// Returns total motor wheel count.
    /// </summary>
    public int UpdateLayout(RaycastWheel[] frontWheels, RaycastWheel[] rearWheels)
    {
        foreach (var w in frontWheels)
            w.isMotor = (driveLayout == DriveLayout.AWD);
        foreach (var w in rearWheels)
            w.isMotor = true;

        int count = 0;
        foreach (var w in frontWheels) if (w.isMotor) count++;
        foreach (var w in rearWheels) if (w.isMotor) count++;
        return count;
    }

    /// <summary>
    /// Distribute engine force across motor wheels using configured diff types.
    /// </summary>
    public void Distribute(float engineForce, RaycastWheel[] frontWheels, RaycastWheel[] rearWheels)
    {
        if (driveLayout == DriveLayout.RWD)
        {
            foreach (var w in frontWheels)
                w.motorForceShare = 0f;
            ApplyAxleDiff(rearWheels[0], rearWheels[1], engineForce, rearDiffType, rearPreload);
        }
        else // AWD
        {
            float frontForce = engineForce * centerFrontBias;
            float rearForce = engineForce * (1f - centerFrontBias);

            if (centerDiffType != DiffType.Open)
            {
                float frontAvgRpm = (frontWheels[0].wheelRpm + frontWheels[1].wheelRpm) * 0.5f;
                float rearAvgRpm = (rearWheels[0].wheelRpm + rearWheels[1].wheelRpm) * 0.5f;
                float centerSpeedDiff = frontAvgRpm - rearAvgRpm;
                float maxCoupling = centerDiffType == DiffType.BallDiff
                    ? centerPreload
                    : Mathf.Abs(engineForce) * 0.5f;
                float centerCoupling = Mathf.Clamp(centerSpeedDiff * DIFF_STIFFNESS, -maxCoupling, maxCoupling);
                frontForce -= centerCoupling;
                rearForce += centerCoupling;
            }

            ApplyAxleDiff(frontWheels[0], frontWheels[1], frontForce, frontDiffType, frontPreload);
            ApplyAxleDiff(rearWheels[0], rearWheels[1], rearForce, rearDiffType, rearPreload);
        }
    }

    void ApplyAxleDiff(RaycastWheel left, RaycastWheel right, float axleForce,
                       DiffType diffType, float couplingPreload)
    {
        // One wheel off ground — all force to grounded wheel
        if (!left.isOnGround && right.isOnGround)
        {
            left.motorForceShare = 0f;
            right.motorForceShare = axleForce;
            return;
        }
        if (left.isOnGround && !right.isOnGround)
        {
            left.motorForceShare = axleForce;
            right.motorForceShare = 0f;
            return;
        }

        float leftShare = axleForce * 0.5f;
        float rightShare = axleForce * 0.5f;

        if (diffType != DiffType.Open && left.isOnGround && right.isOnGround)
        {
            float speedDiff = left.wheelRpm - right.wheelRpm;
            float maxCoupling = diffType == DiffType.BallDiff
                ? couplingPreload
                : Mathf.Abs(axleForce) * 0.5f;
            float coupling = Mathf.Clamp(speedDiff * DIFF_STIFFNESS, -maxCoupling, maxCoupling);
            leftShare -= coupling;
            rightShare += coupling;
        }

        left.motorForceShare = leftShare;
        right.motorForceShare = rightShare;
    }
}
