using UnityEngine;
using System.Collections.Generic;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Discovers and configures <see cref="RaycastWheel"/> children on the vehicle hierarchy.
    /// Separates wheels into front/rear axles by local-Z sign and delegates suspension
    /// and traction settings from RCCar's serialised fields.
    /// </summary>
    internal sealed class WheelManager
    {
        // ---- State ----

        private RaycastWheel[] _all;
        private RaycastWheel[] _front;
        private RaycastWheel[] _rear;


        // ---- Properties ----

        internal RaycastWheel[] All   => _all;
        internal RaycastWheel[] Front => _front;
        internal RaycastWheel[] Rear  => _rear;


        // ---- Discovery ----

        /// <summary>
        /// Walks the transform hierarchy rooted at <paramref name="root"/> and
        /// partitions wheels into front (localPosition.z > 0) and rear lists.
        /// Safe to call before Start; call again to re-discover after hierarchy changes.
        /// </summary>
        internal void Discover(Transform root)
        {
            var allList   = new List<RaycastWheel>();
            var frontList = new List<RaycastWheel>();
            var rearList  = new List<RaycastWheel>();

            foreach (var w in root.GetComponentsInChildren<RaycastWheel>())
            {
                allList.Add(w);
                if (w.transform.localPosition.z > 0f)
                    frontList.Add(w);
                else
                    rearList.Add(w);
            }

            _all   = allList.ToArray();
            _front = frontList.ToArray();
            _rear  = rearList.ToArray();
        }


        // ---- Configuration ----

        /// <summary>
        /// Assigns physics layer mask, debug visibility, and drive layout to all wheels,
        /// then pushes suspension and traction initial values.
        /// </summary>
        internal void Configure(
            Drivetrain drivetrain,
            int carLayer,
            float frontK, float frontDamp,
            float rearK,  float rearDamp,
            float gripCoeff)
        {
            if (drivetrain != null)
                drivetrain.UpdateLayout(_front, _rear);

            ApplySuspension(frontK, frontDamp, rearK, rearDamp);
            ApplyTraction(gripCoeff);

            int groundMask = ~(1 << carLayer);
            foreach (var w in _all)
            {
                w.GroundMask = groundMask;
                w.ShowDebug  = false;
            }
        }

        /// <summary>Pushes per-axle spring/damping values to wheels.</summary>
        internal void ApplySuspension(float frontK, float frontDamp, float rearK, float rearDamp)
        {
            foreach (var w in _front)
            {
                w.SpringStrength = frontK;
                w.SpringDamping  = frontDamp;
            }
            foreach (var w in _rear)
            {
                w.SpringStrength = rearK;
                w.SpringDamping  = rearDamp;
            }
        }

        /// <summary>Pushes grip coefficient to every wheel.</summary>
        internal void ApplyTraction(float gripCoeff)
        {
            foreach (var w in _all)
                w.GripCoeff = gripCoeff;
        }
    }
}
