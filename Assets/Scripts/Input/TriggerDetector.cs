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

        /// <summary>
        /// Minimum change from baseline required to consider an axis as having real input.
        /// Prevents constant resting values (e.g., -1.0 on Xbox combined axis) from
        /// being detected as user input.
        /// </summary>
        const float k_BaselineChangeThreshold = 0.1f;

        // ---- Fields ----

        private readonly int _graceFrames;
        private readonly int _confirmFrames;
        private int _detectFrames;
        private int _separateConfirmCount;
        private int _combinedConfirmCount;

        // Baselines: first observed values for each axis, used to detect actual change
        private float _baselineSepRT = float.NaN;
        private float _baselineSepLT = float.NaN;
        private float _baselineCombined = float.NaN;

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

            // Record baseline on first post-grace frame
            if (float.IsNaN(_baselineSepRT))
            {
                _baselineSepRT = separateRT;
                _baselineSepLT = separateLT;
                _baselineCombined = combined;
            }

            // An axis has real input only if it exceeds the strong threshold
            // AND has changed from its baseline value. This prevents constant
            // resting values (e.g., combined axis stuck at 1.0 = abs(-1.0))
            // from being mistaken for user input.
            bool sepRTActive = separateRT > k_StrongInputThreshold
                && System.Math.Abs(separateRT - _baselineSepRT) > k_BaselineChangeThreshold;
            bool sepLTActive = separateLT > k_StrongInputThreshold
                && System.Math.Abs(separateLT - _baselineSepLT) > k_BaselineChangeThreshold;
            bool hasSeparate = sepRTActive || sepLTActive;
            bool hasCombined = combined > k_StrongInputThreshold
                && System.Math.Abs(combined - _baselineCombined) > k_BaselineChangeThreshold;

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
