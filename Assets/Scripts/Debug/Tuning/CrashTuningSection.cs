using R8EOX.Vehicle;

namespace R8EOX.Debug.Tuning
{
    /// <summary>
    /// Tuning section for crash / tumble physics parameters.
    /// </summary>
    public sealed class CrashTuningSection : TuningSection
    {
        public CrashTuningSection() : base("CRASH PHYSICS") { }

        public override void Initialize(RCCar car)
        {
            Sliders = new[]
            {
                new SliderDefinition(
                    "Tumble Engage (deg)", 10f, 89f,
                    () => car.TumbleEngageDeg,
                    v => car.SetCrashParams(v, car.TumbleFullDeg, car.TumbleBounce, car.TumbleFriction),
                    "F1"),
                new SliderDefinition(
                    "Tumble Full (deg)", 20f, 89f,
                    () => car.TumbleFullDeg,
                    v => car.SetCrashParams(car.TumbleEngageDeg, v, car.TumbleBounce, car.TumbleFriction),
                    "F1"),
                new SliderDefinition(
                    "Tumble Bounce", 0f, 1f,
                    () => car.TumbleBounce,
                    v => car.SetCrashParams(car.TumbleEngageDeg, car.TumbleFullDeg, v, car.TumbleFriction)),
                new SliderDefinition(
                    "Tumble Friction", 0f, 1f,
                    () => car.TumbleFriction,
                    v => car.SetCrashParams(car.TumbleEngageDeg, car.TumbleFullDeg, car.TumbleBounce, v)),
            };
        }
    }
}
