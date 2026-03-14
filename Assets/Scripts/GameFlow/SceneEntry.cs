namespace R8EOX.GameFlow
{
    /// <summary>
    /// Data class for a scene registry entry.
    /// </summary>
    [System.Serializable]
    public sealed class SceneEntry
    {
        /// <summary>Unique identifier for the scene.</summary>
        public string Id;

        /// <summary>Human-readable display name.</summary>
        public string DisplayName;

        /// <summary>Unity scene asset path (e.g., "Assets/Scenes/Track.unity").</summary>
        public string ScenePath;

        public SceneEntry(string id, string displayName, string scenePath)
        {
            Id = id;
            DisplayName = displayName;
            ScenePath = scenePath;
        }
    }
}
