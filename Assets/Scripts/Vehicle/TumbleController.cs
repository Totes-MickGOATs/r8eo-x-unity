using UnityEngine;
using PhysicsMath = R8EOX.Vehicle.Physics;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Evaluates tilt angle and blends the dynamic physics material between
    /// normal-drive values and crash/tumble values according to <see cref="TumbleFactor"/>.
    /// Owns the <see cref="PhysicMaterial"/> applied to the vehicle's body colliders.
    /// </summary>
    internal sealed class TumbleController
    {
        // ---- Constants ----

        const float k_DefaultBounciness  = 0.05f;
        const float k_DefaultFriction    = 0f;


        // ---- State ----

        private PhysicMaterial _physMat;
        private Collider[]     _colliders;
        private bool           _wasTumbling;


        // ---- Properties ----

        internal float TiltAngle    { get; private set; }
        internal float TumbleFactor { get; private set; }


        // ---- Initialisation ----

        /// <summary>
        /// Creates the physics material and applies it to all colliders on
        /// <paramref name="root"/>'s hierarchy.
        /// </summary>
        internal void Initialise(Transform root)
        {
            _physMat = new PhysicMaterial("CarBody")
            {
                dynamicFriction  = k_DefaultFriction,
                staticFriction   = k_DefaultFriction,
                bounciness       = k_DefaultBounciness,
                frictionCombine  = PhysicMaterialCombine.Minimum,
                bounceCombine    = PhysicMaterialCombine.Maximum
            };

            _colliders = root.GetComponentsInChildren<Collider>();
            foreach (var col in _colliders)
                col.material = _physMat;
        }


        // ---- Per-Frame Update ----

        /// <summary>
        /// Recomputes tumble factor from current tilt and updates the physics material blend.
        /// Must be called once per FixedUpdate before any force application.
        /// </summary>
        internal void Update(
            Transform vehicleTransform,
            bool isAirborne,
            float engageDeg,
            float fullDeg,
            float hysteresisDeg,
            float tumbleBounce,
            float tumbleFriction,
            bool enableDynamicMaterial)
        {
            TiltAngle    = PhysicsMath.TumbleMath.ComputeTiltAngle(vehicleTransform.up);
            TumbleFactor = PhysicsMath.TumbleMath.ComputeTumbleFactor(
                TiltAngle, isAirborne, _wasTumbling,
                engageDeg, fullDeg, hysteresisDeg);

            _wasTumbling = TumbleFactor > 0f;

            if (enableDynamicMaterial && _physMat != null)
            {
                _physMat.bounciness     = Mathf.Lerp(k_DefaultBounciness, tumbleBounce,   TumbleFactor);
                _physMat.dynamicFriction = Mathf.Lerp(k_DefaultFriction,  tumbleFriction, TumbleFactor);
                _physMat.staticFriction  = Mathf.Lerp(k_DefaultFriction,  tumbleFriction, TumbleFactor);
            }
        }
    }
}
