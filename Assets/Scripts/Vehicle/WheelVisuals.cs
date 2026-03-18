using UnityEngine;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Pure-visual helpers for RaycastWheel: droop animation, spin, and debug gizmos.
    /// No physics state is stored here — all data is passed in per-call.
    /// </summary>
    internal static class WheelVisuals
    {
        const float k_DroopSpeed = 200f;
        const float k_DebugScale = 0.024f;

        /// <summary>Animate wheel/hub visuals to the suspension droop position when airborne.</summary>
        internal static void ApplyDroop(Transform wheelVisual, Transform hubVisual,
            float restDistance, float overExtend, float dt)
        {
            float droopTarget = -(restDistance + overExtend);
            if (wheelVisual != null)
                wheelVisual.localPosition = new Vector3(0f,
                    Mathf.MoveTowards(wheelVisual.localPosition.y, droopTarget, k_DroopSpeed * dt), 0f);
            if (hubVisual != null)
                hubVisual.localPosition = new Vector3(0f,
                    Mathf.MoveTowards(hubVisual.localPosition.y, droopTarget, k_DroopSpeed * dt), 0f);
        }

        /// <summary>Position and spin wheel/hub visuals when grounded.</summary>
        internal static void UpdateGrounded(Transform wheelVisual, Transform hubVisual,
            Transform axleTransform, float springLen, float fSpeed, float wheelRadius, float dt)
        {
            float spinAngle = fSpeed / wheelRadius * dt * Mathf.Rad2Deg;
            if (wheelVisual != null)
            {
                wheelVisual.localPosition = new Vector3(0f, -springLen, 0f);
                wheelVisual.Rotate(axleTransform.right, spinAngle, Space.World);
            }
            if (hubVisual != null)
            {
                hubVisual.localPosition = new Vector3(0f, -springLen, 0f);
                hubVisual.Rotate(axleTransform.right, spinAngle, Space.World);
            }
        }

        /// <summary>Draw Scene-view force arrows for the contact point forces.</summary>
        internal static void DrawForces(Transform axleTransform, Vector3 contactPoint,
            Vector3 yForce, Vector3 xForce, Vector3 zForce, Vector3 motorForce)
        {
            Debug.DrawLine(axleTransform.position, contactPoint, Color.white);
            if (yForce.sqrMagnitude > 0.0001f)
                Debug.DrawRay(contactPoint, yForce * k_DebugScale, Color.yellow);
            if (xForce.sqrMagnitude > 0.0001f)
                Debug.DrawRay(contactPoint, xForce * k_DebugScale, Color.red);
            if (zForce.sqrMagnitude > 0.0001f)
                Debug.DrawRay(contactPoint, zForce * k_DebugScale, Color.green);
            if (motorForce.sqrMagnitude > 0.0001f)
                Debug.DrawRay(contactPoint, motorForce * k_DebugScale, Color.cyan);
        }
    }
}
