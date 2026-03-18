using UnityEngine;
using PhysicsMath = R8EOX.Vehicle.Physics;

namespace R8EOX.Vehicle
{
    /// <summary>Computes tumble factor and blends physics material during crashes.</summary>
    public class TumbleController
    {
        const float k_DefaultBounciness = 0.05f;

        readonly PhysicMaterial _physMat;
        bool _wasTumbling;

        public float TiltAngle   { get; private set; }
        public float TumbleFactor { get; private set; }

        public TumbleController(PhysicMaterial physMat) { _physMat = physMat; }

        public void Update(Transform t, bool isAirborne,
            float engageDeg, float fullDeg, float hysteresisDeg,
            bool enableDynamic, float bounce, float friction)
        {
            TiltAngle = PhysicsMath.TumbleMath.ComputeTiltAngle(t.up);
            TumbleFactor = PhysicsMath.TumbleMath.ComputeTumbleFactor(
                TiltAngle, isAirborne, _wasTumbling,
                engageDeg, fullDeg, hysteresisDeg);
            _wasTumbling = TumbleFactor > 0f;

            if (!enableDynamic || _physMat == null) return;
            _physMat.bounciness      = Mathf.Lerp(k_DefaultBounciness, bounce,  TumbleFactor);
            _physMat.dynamicFriction = Mathf.Lerp(0f,                  friction, TumbleFactor);
            _physMat.staticFriction  = Mathf.Lerp(0f,                  friction, TumbleFactor);
        }
    }
}
