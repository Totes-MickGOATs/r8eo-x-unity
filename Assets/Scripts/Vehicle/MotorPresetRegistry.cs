namespace R8EOX.Vehicle
{
    /// <summary>
    /// Motor turn-rating presets matching real RC motor specifications.
    /// Lower turn = faster motor = higher force and speed.
    /// </summary>
    public enum MotorPreset
    {
        Motor21_5T, Motor17_5T, Motor13_5T, Motor9_5T, Motor5_5T, Motor1_5T, Custom
    }

    /// <summary>
    /// Immutable data record for a single motor preset.
    /// All force values are in Newtons; speed in m/s; ramp in units/sec.
    /// </summary>
    internal struct MotorData
    {
        public readonly float EngineForceMax;
        public readonly float BrakeForce;
        public readonly float ReverseForce;
        public readonly float CoastDrag;
        public readonly float MaxSpeed;
        public readonly float ThrottleRampUp;

        public MotorData(float engine, float brake, float reverse, float coast, float max, float ramp)
        {
            EngineForceMax  = engine;
            BrakeForce      = brake;
            ReverseForce    = reverse;
            CoastDrag       = coast;
            MaxSpeed        = max;
            ThrottleRampUp  = ramp;
        }
    }

    /// <summary>
    /// Lookup table of built-in motor presets keyed by <see cref="MotorPreset"/> index.
    /// Add new entries here when calibrating additional motor winds.
    /// </summary>
    internal static class MotorPresetRegistry
    {
        // Indexed by (int)MotorPreset — must stay in enum declaration order.
        internal static readonly MotorData[] k_Presets =
        {
            new MotorData(155f, 132f,  85f, 20f, 13f,  3.0f),  // 21.5T
            new MotorData(180f, 153f,  99f, 25f, 20f,  4.0f),  // 17.5T
            new MotorData(260f, 221f, 143f, 30f, 27f,  5.5f),  // 13.5T
            new MotorData(340f, 289f, 187f, 35f, 34f,  7.0f),  // 9.5T
            new MotorData(440f, 374f, 242f, 40f, 44f,  9.0f),  // 5.5T
            new MotorData(560f, 476f, 308f, 50f, 56f, 12.0f),  // 1.5T
        };

        /// <summary>
        /// Returns the preset data for <paramref name="preset"/>.
        /// Returns false when preset is <see cref="MotorPreset.Custom"/> or out of range.
        /// </summary>
        internal static bool TryGet(MotorPreset preset, out MotorData data)
        {
            int idx = (int)preset;
            if (preset == MotorPreset.Custom || idx < 0 || idx >= k_Presets.Length)
            {
                data = default;
                return false;
            }
            data = k_Presets[idx];
            return true;
        }
    }
}
