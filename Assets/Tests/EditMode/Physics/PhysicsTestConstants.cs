namespace R8EOX.Tests.EditMode
{
    public static class PhysicsTestConstants
    {
        public const float k_Mass = 1.5f;                   // kg
        public const float k_WheelRadiusFront = 0.0425f;   // m  (Proline Electron front, 1:10 scale)
        public const float k_WheelRadiusRear  = 0.0420f;   // m  (Proline Electron rear, 1:10 scale)
        public const float k_RestDistance = 0.20f;          // m
        public const float k_MinSpringLen = 0.032f;         // m (bump stop)
        public const float k_SpringK = 75f;                 // N/m
        public const float k_Damping = 4.25f;               // damping coefficient
        public const float k_MaxSpringForce = 50f;          // N
        public const float k_OverExtend = 0.08f;            // m
        public const float k_Dt = 0.008333f;                // 120 Hz physics step
        public const float k_Epsilon = 0.0001f;             // float comparison tolerance
    }
}
