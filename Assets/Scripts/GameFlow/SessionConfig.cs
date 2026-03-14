namespace R8EOX.GameFlow
{
    /// <summary>
    /// Immutable session configuration passed through the menu flow.
    /// </summary>
    public sealed class SessionConfig
    {
        /// <summary>Game mode identifier (e.g., "practice", "race", "time_trial").</summary>
        public string ModeId { get; }

        /// <summary>Track identifier for registry lookup.</summary>
        public string TrackId { get; }

        /// <summary>Scene path for the selected track.</summary>
        public string TrackScene { get; }

        /// <summary>Car identifier for registry lookup.</summary>
        public string CarId { get; }

        /// <summary>Number of laps for the session.</summary>
        public int TotalLaps { get; }

        /// <summary>AI difficulty level (0 = easiest).</summary>
        public int AiDifficulty { get; }

        public SessionConfig(string modeId, string trackId, string trackScene,
                             string carId, int totalLaps, int aiDifficulty)
        {
            ModeId = modeId;
            TrackId = trackId;
            TrackScene = trackScene;
            CarId = carId;
            TotalLaps = totalLaps;
            AiDifficulty = aiDifficulty;
        }
    }
}
