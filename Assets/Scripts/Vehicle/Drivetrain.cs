using UnityEngine;
using R8EOX.Shared;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Differential coupling and drive layout logic.
    /// Distributes engine force across motor wheels using configured differential types.
    /// </summary>
    public class Drivetrain : MonoBehaviour
    {
        // ---- Enums ----

        public enum DiffType { Open, BallDiff, Spool }
        public enum DriveLayout { RWD, AWD }


        // ---- Serialized Fields ----

        [Header("Drivetrain")]
        [Tooltip("Drive layout: RWD sends power to rear wheels only, AWD to all")]
        [SerializeField] private DriveLayout _driveLayout = DriveLayout.RWD;

        [Header("Rear Differential")]
        [Tooltip("Rear axle differential type")]
        [SerializeField] private DiffType _rearDiffType = DiffType.Open;
        [Tooltip("Max coupling force the rear ball diff transfers between wheels (N)")]
        [SerializeField] private float _rearPreload = 50.0f;

        [Header("Front Differential")]
        [Tooltip("Front axle differential type")]
        [SerializeField] private DiffType _frontDiffType = DiffType.Open;
        [Tooltip("Max coupling force for front differential (N)")]
        [SerializeField] private float _frontPreload = 10.0f;

        [Header("Center Differential (AWD only)")]
        [Tooltip("Center differential type for AWD torque distribution")]
        [SerializeField] private DiffType _centerDiffType = DiffType.Open;
        [Tooltip("Max coupling force for center differential (N)")]
        [SerializeField] private float _centerPreload = 20.0f;
        [Tooltip("0.35 = 35% front / 65% rear torque split")]
        [Range(0f, 1f)]
        [SerializeField] private float _centerFrontBias = 0.35f;


        // ---- Private Fields ----

#if UNITY_EDITOR || DEBUG
        float _debugLogTimer;
#endif


        // ---- Public Properties ----

        /// <summary>Current drive layout (RWD or AWD).</summary>
        public DriveLayout ActiveDriveLayout { get => _driveLayout; set => _driveLayout = value; }
        /// <summary>Rear differential type.</summary>
        public DiffType RearDiff { get => _rearDiffType; set => _rearDiffType = value; }
        /// <summary>Front differential type.</summary>
        public DiffType FrontDiff { get => _frontDiffType; set => _frontDiffType = value; }
        /// <summary>Rear differential preload in Newtons.</summary>
        public float RearPreload { get => _rearPreload; set => _rearPreload = value; }


        // ---- Public API ----

        /// <summary>
        /// Set IsMotor on front/rear wheels based on drive layout.
        /// Returns total motor wheel count.
        /// </summary>
        public int UpdateLayout(RaycastWheel[] frontWheels, RaycastWheel[] rearWheels)
        {
            foreach (var w in frontWheels)
                w.IsMotor = (_driveLayout == DriveLayout.AWD);
            foreach (var w in rearWheels)
                w.IsMotor = true;

            int count = 0;
            foreach (var w in frontWheels) if (w.IsMotor) count++;
            foreach (var w in rearWheels) if (w.IsMotor) count++;
            return count;
        }

        /// <summary>
        /// Distribute engine force across motor wheels using configured diff types.
        /// </summary>
        public void Distribute(float engineForce, RaycastWheel[] frontWheels, RaycastWheel[] rearWheels)
        {
            if (_driveLayout == DriveLayout.RWD)
            {
                foreach (var w in frontWheels)
                    w.MotorForceShare = 0f;
                DrivetrainDiff.Apply(rearWheels[0], rearWheels[1], engineForce, _rearDiffType, _rearPreload);
            }
            else
            {
                float frontForce = engineForce * _centerFrontBias;
                float rearForce  = engineForce * (1f - _centerFrontBias);

                if (_centerDiffType != DiffType.Open)
                {
                    float frontAvgRpm    = (frontWheels[0].WheelRpm + frontWheels[1].WheelRpm) * 0.5f;
                    float rearAvgRpm     = (rearWheels[0].WheelRpm  + rearWheels[1].WheelRpm)  * 0.5f;
                    float centerSpeedDiff = frontAvgRpm - rearAvgRpm;
                    float maxCoupling    = _centerDiffType == DiffType.BallDiff
                        ? _centerPreload
                        : Mathf.Abs(engineForce) * 0.5f;
                    float centerCoupling = Mathf.Clamp(centerSpeedDiff * DrivetrainDiff.DiffStiffness, -maxCoupling, maxCoupling);
                    frontForce -= centerCoupling;
                    rearForce  += centerCoupling;
                }

                DrivetrainDiff.Apply(frontWheels[0], frontWheels[1], frontForce, _frontDiffType, _frontPreload);
                DrivetrainDiff.Apply(rearWheels[0], rearWheels[1], rearForce, _rearDiffType, _rearPreload);
            }

#if UNITY_EDITOR || DEBUG
            _debugLogTimer += Time.fixedDeltaTime;
            if (_debugLogTimer >= 0.5f)
            {
                RuntimeLog.Log($"[drivetrain] totalForce={engineForce:F2}N fl={frontWheels[0].MotorForceShare:F2}N fr={frontWheels[1].MotorForceShare:F2}N rl={rearWheels[0].MotorForceShare:F2}N rr={rearWheels[1].MotorForceShare:F2}N");
                _debugLogTimer = 0f;
            }
#endif
        }
    }
}
