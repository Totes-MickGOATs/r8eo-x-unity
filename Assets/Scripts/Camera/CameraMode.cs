namespace R8EOX.Camera
{
    /// <summary>
    /// Available camera viewing modes for the RC buggy.
    /// </summary>
    public enum CameraMode
    {
        /// <summary>Follows behind the car with smooth interpolation.</summary>
        Chase,

        /// <summary>Player orbits around the car with right stick or mouse drag.</summary>
        Orbit,

        /// <summary>Fixed to the car body, looking forward like an FPV camera.</summary>
        Fpv,

        /// <summary>Static position that tracks the car with rotation only.</summary>
        Trackside
    }
}