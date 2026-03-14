using System.Collections.Generic;
using UnityEngine;

namespace R8EOX.GameFlow
{
    /// <summary>
    /// ScriptableObject lookup table for registered scenes.
    /// Designer-editable in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneRegistry", menuName = "R8EOX/Scene Registry")]
    public sealed class SceneRegistry : ScriptableObject
    {
        [SerializeField]
        [Tooltip("List of all registered scenes available in the game.")]
        private List<SceneEntry> _scenes = new List<SceneEntry>();

        /// <summary>All registered scene entries.</summary>
        public IReadOnlyList<SceneEntry> AllScenes => _scenes;

        /// <summary>
        /// Try to find a scene entry by its unique identifier.
        /// </summary>
        /// <param name="id">The scene identifier to look up.</param>
        /// <param name="entry">The found entry, or null if not found.</param>
        /// <returns>True if the scene was found.</returns>
        public bool TryGetScene(string id, out SceneEntry entry)
        {
            for (int i = 0; i < _scenes.Count; i++)
            {
                if (_scenes[i].Id == id)
                {
                    entry = _scenes[i];
                    return true;
                }
            }

            entry = null;
            return false;
        }

        /// <summary>
        /// Add a scene entry to the registry. Primarily for testing and editor tools.
        /// </summary>
        /// <param name="entry">The scene entry to add.</param>
        public void AddScene(SceneEntry entry)
        {
            _scenes.Add(entry);
        }
    }
}
