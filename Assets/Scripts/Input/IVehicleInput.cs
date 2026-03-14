namespace R8EOX.Input
{
    /// <summary>
    /// Interface for vehicle input providers.
    /// Decouples the vehicle controller from the specific input implementation,
    /// enabling swappable input sources (player, AI, replay, network).
    /// </summary>
    public interface IVehicleInput
    {
        /// <summary>Throttle input (0 to 1).</summary>
        float Throttle { get; }

        /// <summary>Brake input (0 to 1).</summary>
        float Brake { get; }

        /// <summary>Steering input (-1 to +1, left to right).</summary>
        float Steer { get; }

        /// <summary>True on the frame the reset/flip button was pressed.</summary>
        bool ResetPressed { get; }

        /// <summary>True on the frame the debug toggle was pressed.</summary>
        bool DebugTogglePressed { get; }

        /// <summary>True on the frame the camera cycle button was pressed.</summary>
        bool CameraCyclePressed { get; }
    }
}
