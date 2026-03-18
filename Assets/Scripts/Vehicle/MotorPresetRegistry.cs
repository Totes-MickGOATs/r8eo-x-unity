namespace R8EOX.Vehicle
{
    /// <summary>Motor turn rating presets matching real RC motor specifications.</summary>
    public static class MotorPresetRegistry
    {
        public struct MotorData
        {
            public float EngineForceMax;
            public float BrakeForce;
            public float ReverseForce;
            public float CoastDrag;
            public float MaxSpeed;
            public float ThrottleRampUp;

            public MotorData(float engine, float brake, float reverse, float coast, float max, float ramp)
            {
                EngineForceMax = engine;
                BrakeForce = brake;
                ReverseForce = reverse;
                CoastDrag = coast;
                MaxSpeed = max;
                ThrottleRampUp = ramp;
            }
        }

        public static readonly MotorData[] Presets =
        {
            new MotorData(155f, 132f,  85f, 20f, 13f, 3.0f),  // 21.5T
            new MotorData(180f, 153f,  99f, 25f, 20f, 4.0f),  // 17.5T
            new MotorData(260f, 221f, 143f, 30f, 27f, 5.5f),  // 13.5T
            new MotorData(340f, 289f, 187f, 35f, 34f, 7.0f),  // 9.5T
            new MotorData(440f, 374f, 242f, 40f, 44f, 9.0f),  // 5.5T
            new MotorData(560f, 476f, 308f, 50f, 56f, 12.0f), // 1.5T
        };

        /// <summary>Returns preset data for the given preset, or null for Custom.</summary>
        public static MotorData? Get(RCCar.MotorPreset preset)
        {
            int idx = (int)preset;
            if (preset == RCCar.MotorPreset.Custom || idx < 0 || idx >= Presets.Length)
                return null;
            return Presets[idx];
        }
    }
}
