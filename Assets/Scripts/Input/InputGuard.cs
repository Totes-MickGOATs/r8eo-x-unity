namespace R8EOX.Input
{
    /// <summary>
    /// Pure utility for input startup suppression (M3 fix).
    /// Suppresses all input during the first few frames to avoid
    /// stale axis values that Unity reports on startup.
    /// </summary>
    public static class InputGuard
    {
        /// <summary>Minimum frame count before input is allowed.</summary>
        public const int k_MinFrameCount = 3;

        /// <summary>
        /// Returns true if input should be suppressed (zeroed) for the given frame.
        /// </summary>
        /// <param name="frameCount">Current Time.frameCount value</param>
        /// <returns>True if input should be suppressed</returns>
        public static bool ShouldSuppressInput(int frameCount)
        {
            return frameCount < k_MinFrameCount;
        }
    }
}
