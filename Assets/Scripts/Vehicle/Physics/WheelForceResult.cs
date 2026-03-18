using UnityEngine;

namespace R8EOX.Vehicle.Physics
{
    /// <summary>
    /// Output of <see cref="WheelForceSolver.Solve"/>. Contains all per-wheel force vectors
    /// and intermediate scalar diagnostics (spring length, grip load, slip ratio, etc.)
    /// needed by RaycastWheel to apply forces to the Rigidbody and drive telemetry.
    /// </summary>
    public struct WheelForceResult
    {
        /// <summary>Suspension spring + damper force along the contact normal.</summary>
        public Vector3 SuspensionForce;
        /// <summary>Lateral (sideways) grip force.</summary>
        public Vector3 LateralForce;
        /// <summary>Longitudinal (fore-aft) friction force.</summary>
        public Vector3 LongitudinalForce;
        /// <summary>Motor drive force (zero on non-motor wheels).</summary>
        public Vector3 MotorForce;
        /// <summary>Sum of all four force components.</summary>
        public Vector3 TotalForce;

        // --- Diagnostics ---

        /// <summary>Computed spring length this frame (m).</summary>
        public float SpringLen;
        /// <summary>Suspension force magnitude (N).</summary>
        public float SuspensionForceMag;
        /// <summary>Normalized grip load [0, 1] derived from suspension force.</summary>
        public float GripLoad;
        /// <summary>Lateral slip ratio [0, 1].</summary>
        public float SlipRatio;
        /// <summary>Grip factor sampled from the grip curve.</summary>
        public float GripFactor;
        /// <summary>Forward speed component of tire velocity (m/s).</summary>
        public float ForwardSpeed;
        /// <summary>Total tire contact speed (m/s).</summary>
        public float Speed;
    }
}
