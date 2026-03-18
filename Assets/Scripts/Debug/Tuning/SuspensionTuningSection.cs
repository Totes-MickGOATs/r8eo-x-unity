using R8EOX.Vehicle;

namespace R8EOX.Debug.Tuning
{
    /// <summary>
    /// Tuning section for suspension spring and damping parameters.
    /// Writes both front and rear values symmetrically via SetSuspension.
    /// </summary>
    public sealed class SuspensionTuningSection : TuningSection
    {
        public SuspensionTuningSection() : base("SUSPENSION") { }

        public override void Initialize(RCCar car)
        {
            Sliders = new[]
            {
                new SliderDefinition(
                    "Spring (N/m)", 10f, 2000f,
                    () => car.FrontSpringStrength,
                    v => car.SetSuspension(v, car.FrontSpringDamping)),
                new SliderDefinition(
                    "Damping (N·s/m)", 0.5f, 40f,
                    () => car.FrontSpringDamping,
                    v => car.SetSuspension(car.FrontSpringStrength, v)),
            };
        }
    }
}
