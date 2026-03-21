using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Debug
{
    /// <summary>
    /// Pure string-generation helpers for the <see cref="TelemetryHUD"/> overlay.
    /// Extracted so the data-formatting logic can be unit-tested without a running scene.
    /// </summary>
    public static class TelemetryHudRenderer
    {
        /// <summary>
        /// Returns the six vehicle-state display lines in order:
        /// Speed, ForwardSpeed, Throttle/Engine, Brake/Reverse, Steering, State/Tumble/Tilt.
        /// </summary>
        public static string[] GetVehicleLines(RCCar car, Rigidbody rb)
        {
            string airState = car.IsAirborne ? "AIRBORNE" : "GROUNDED";
            return new[]
            {
                $"Speed: {car.GetSpeedKmh():F1} km/h  ({rb.velocity.magnitude:F1} m/s)",
                $"Fwd Speed: {car.GetForwardSpeedKmh():F1} km/h",
                $"Throttle: {car.SmoothThrottle:F2}  Engine: {car.CurrentEngineForce:F1} N",
                $"Brake: {car.CurrentBrakeForce:F1} N  Reverse: {(car.ReverseEngaged ? "YES" : "no")}",
                $"Steering: {car.CurrentSteering * Mathf.Rad2Deg:F1} deg",
                $"State: {airState}  Tumble: {car.TumbleFactor:F2}  Tilt: {car.TiltAngle:F1} deg",
            };
        }

        /// <summary>
        /// Returns one display line per wheel. Returns an empty array when wheels is null.
        /// </summary>
        public static string[] GetWheelLines(RaycastWheel[] wheels)
        {
            if (wheels == null) return System.Array.Empty<string>();

            var lines = new string[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
            {
                var w = wheels[i];
                string ground = w.IsOnGround ? "GND" : "AIR";
                string motor  = w.IsMotor ? "M" : " ";
                string steer  = w.IsSteer ? "S" : " ";
                lines[i] = $"{w.name} [{motor}{steer}] {ground}  " +
                            $"spring={w.LastSpringLen:F3}  slip={w.SlipRatio:F2}  " +
                            $"grip={w.GripFactor:F2}  rpm={w.WheelRpm:F0}";
            }
            return lines;
        }
    }
}
