using R8EOX.Vehicle;

namespace R8EOX.Debug.Tuning
{
    /// <summary>
    /// Tuning section for steering geometry and speed-reduction parameters.
    /// </summary>
    public sealed class SteeringTuningSection : TuningSection
    {
        public SteeringTuningSection() : base("STEERING") { }

        public override void Initialize(RCCar car)
        {
            Sliders = new[]
            {
                new SliderDefinition(
                    "Max Angle (rad)", 0.1f, 1.2f,
                    () => car.SteeringMax,
                    v => car.SetSteeringParams(v, car.SteeringSpeed, car.SteeringSpeedLimit, car.SteeringHighSpeedFactor)),
                new SliderDefinition(
                    "Speed (rad/s)", 1f, 15f,
                    () => car.SteeringSpeed,
                    v => car.SetSteeringParams(car.SteeringMax, v, car.SteeringSpeedLimit, car.SteeringHighSpeedFactor)),
                new SliderDefinition(
                    "Speed Limit (m/s)", 1f, 30f,
                    () => car.SteeringSpeedLimit,
                    v => car.SetSteeringParams(car.SteeringMax, car.SteeringSpeed, v, car.SteeringHighSpeedFactor)),
                new SliderDefinition(
                    "Hi-Speed Factor", 0f, 1f,
                    () => car.SteeringHighSpeedFactor,
                    v => car.SetSteeringParams(car.SteeringMax, car.SteeringSpeed, car.SteeringSpeedLimit, v)),
            };
        }
    }
}
