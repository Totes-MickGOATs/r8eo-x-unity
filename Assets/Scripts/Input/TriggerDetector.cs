namespace R8EOX.Input
{
    /// <summary>
    /// Pure logic for detecting gamepad trigger mode (Separate vs Combined vs None).
    /// Extracted from RCInput for testability. Implements C5 fix: grace period
    /// and sustained-input confirmation to prevent ghost brake from gamepad noise.
    /// </summary>
    public class TriggerDetector
    {
        // ---- Types ----

        public enum Mode { Detecting, Separate, Combined, None }

        // ---- Constants ----

        const float k_StrongInputThreshold = 0.3f;

        // ---- Fields ----

        private readonly int _graceFrames;
        private readonly int _confirmFrames;
        private int _detectFrames;
        private int _separateConfirmCount;
        private int _combinedConfirmCount;

        // ---- Properties ----

        public Mode CurrentMode { get; private set; } = Mode.Detecting;

        // ---- Constructor ----

        /// <param name="graceFrames">Number of startup frames to skip (C5/M3 fix)</param>
        /// <param name="confirmFrames">Consecutive strong-input frames required to lock mode</param>
        public TriggerDetector(int graceFrames, int confirmFrames)
        {
            _graceFrames = graceFrames;
            _confirmFrames = confirmFrames;
        }

        // ---- Public API ----

        /// <summary>
        /// Process one frame of trigger input. Call every Update() while mode is Detecting.
        /// </summary>
        /// <param name="separateRT">Absolute value of right trigger axis</param>
        /// <param name="separateLT">Absolute value of left trigger axis</param>
        /// <param name="combined">Absolute value of combined trigger axis</param>
        /// <param name="frameCount">Current Time.frameCount</param>
        public void ProcessFrame(float separateRT, float separateLT, float combined, int frameCount)
        {
            if (CurrentMode != Mode.Detecting)
                return;

            // Grace period: skip detection during early frames
            if (frameCount < _graceFrames)
                return;

            _detectFrames++;

            bool hasSeparate = separateRT > k_StrongInputThreshold || separateLT > k_StrongInputThreshold;
            bool hasCombined = combined > k_StrongInputThreshold;

            // Count consecutive frames of strong input for each mode
            if (hasSeparate)
            {
                _separateConfirmCount++;
                _combinedConfirmCount = 0; // Reset other counter
            }
            else if (hasCombined)
            {
                _combinedConfirmCount++;
                _separateConfirmCount = 0;
            }
            else
            {
                _separateConfirmCount = 0;
                _combinedConfirmCount = 0;
            }

            // Lock mode only after sustained input
            if (_separateConfirmCount >= _confirmFrames)
            {
                CurrentMode = Mode.Separate;
                return;
            }

            if (_combinedConfirmCount >= _confirmFrames)
            {
                CurrentMode = Mode.Combined;
                return;
            }

            // Timeout: no triggers detected
            if (_detectFrames >= 300)
            {
                CurrentMode = Mode.None;
            }
        }
    }
}
