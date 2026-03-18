namespace R8EOX.Vehicle
{
    /// <summary>
    /// Debounces airborne detection with a fixed-frame threshold to prevent
    /// single-frame ground-contact flicker from toggling air-physics.
    /// A vehicle is considered airborne only after <see cref="k_Threshold"/>
    /// consecutive frames with no wheel ground contact.
    /// </summary>
    internal sealed class AirborneDetector
    {
        // ---- Constants ----

        const int k_Threshold = 5;


        // ---- State ----

        private int _airborneFrames;


        // ---- Public API ----

        /// <summary>
        /// Updates the frame counter from wheel ground state and returns
        /// whether the vehicle is currently considered airborne.
        /// </summary>
        /// <param name="anyWheelOnGround">True when at least one wheel reports ground contact.</param>
        /// <returns>True when airborne frame count has reached the threshold.</returns>
        internal bool Update(bool anyWheelOnGround)
        {
            if (anyWheelOnGround)
            {
                _airborneFrames = 0;
            }
            else
            {
                if (_airborneFrames < k_Threshold)
                    _airborneFrames++;
            }

            return _airborneFrames >= k_Threshold;
        }

        /// <summary>Resets the airborne frame counter (e.g. after a flip reset).</summary>
        internal void Reset() => _airborneFrames = 0;
    }
}
