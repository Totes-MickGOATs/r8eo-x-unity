using System.Collections.Generic;
using UnityEngine;

namespace R8EOX.Vehicle
{
    /// <summary>Discovers and configures RaycastWheel children for RCCar.</summary>
    public class WheelManager
    {
        public RaycastWheel[] All { get; private set; } = new RaycastWheel[0];
        public RaycastWheel[] Front { get; private set; } = new RaycastWheel[0];
        public RaycastWheel[] Rear { get; private set; } = new RaycastWheel[0];

        public void Discover(Transform root)
        {
            var all = new List<RaycastWheel>();
            var front = new List<RaycastWheel>();
            var rear = new List<RaycastWheel>();

            foreach (var w in root.GetComponentsInChildren<RaycastWheel>())
            {
                all.Add(w);
                if (w.transform.localPosition.z > 0f) front.Add(w);
                else rear.Add(w);
            }

            All = all.ToArray();
            Front = front.ToArray();
            Rear = rear.ToArray();
        }

        public void Configure(int carLayer, Drivetrain drivetrain, float frontK, float frontDamp, float rearK, float rearDamp, float gripCoeff)
        {
            if (drivetrain != null)
                drivetrain.UpdateLayout(Front, Rear);

            PushSuspension(frontK, frontDamp, rearK, rearDamp);
            PushGrip(gripCoeff);

            foreach (var w in All)
            {
                w.GroundMask = ~(1 << carLayer);
                w.ShowDebug = false;
            }
        }

        public void PushSuspension(float frontK, float frontDamp, float rearK, float rearDamp)
        {
            foreach (var w in Front) { w.SpringStrength = frontK; w.SpringDamping = frontDamp; }
            foreach (var w in Rear)  { w.SpringStrength = rearK;  w.SpringDamping = rearDamp; }
        }

        public void PushGrip(float gripCoeff)
        {
            foreach (var w in All)
                w.GripCoeff = gripCoeff;
        }

        public bool AnyOnGround()
        {
            foreach (var w in All)
                if (w.IsOnGround) return true;
            return false;
        }
    }
}
