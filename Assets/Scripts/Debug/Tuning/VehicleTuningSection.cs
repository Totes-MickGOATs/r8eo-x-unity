using R8EOX.Vehicle;

namespace R8EOX.Debug.Tuning
{
    /// <summary>
    /// Tuning section for top-level vehicle parameters: mass, grip, throttle response,
    /// and centre-of-mass height.
    /// </summary>
    public sealed class VehicleTuningSection : TuningSection
    {
        public VehicleTuningSection() : base("VEHICLE") { }

        public override void Initialize(RCCar car)
        {
            Sliders = new[]
            {
                new SliderDefinition(
                    "Mass (kg)", 0.5f, 50f,
                    () => car.Mass,
                    v => car.SetMass(v)),
                new SliderDefinition(
                    "Grip Coeff (0-1)", 0f, 1f,
                    () => car.GripCoeff,
                    v => car.SetTraction(v)),
                new SliderDefinition(
                    "Ramp Up (u/s)", 1f, 20f,
                    () => car.ThrottleRampUp,
                    v => car.SetThrottleResponse(v, car.ThrottleRampDown)),
                new SliderDefinition(
                    "Ramp Down (u/s)", 1f, 30f,
                    () => car.ThrottleRampDown,
                    v => car.SetThrottleResponse(car.ThrottleRampUp, v)),
                new SliderDefinition(
                    "CoM Ground Y (m)", -1f, 0.5f,
                    () => car.ComGroundY,
                    v => car.SetCentreOfMass(v)),
            };
        }
    }
}
