namespace R8EOX.UI
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Maps screen IDs to their prefab or factory. Add new screens by adding
    /// entries to this ScriptableObject in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "ScreenRegistry", menuName = "R8EOX/Screen Registry")]
    public sealed class ScreenRegistry : ScriptableObject
    {
        [SerializeField]
        [Tooltip("List of screen entries mapping IDs to prefabs")]
        private List<ScreenRegistryEntry> _entries = new();

        /// <summary>Look up a screen prefab by ID.</summary>
        public bool TryGetScreen(string screenId, out GameObject prefab)
        {
            foreach (var entry in _entries)
            {
                if (entry.ScreenId == screenId)
                {
                    prefab = entry.Prefab;
                    return prefab != null;
                }
            }

            prefab = null;
            return false;
        }

        /// <summary>All registered screen entries.</summary>
        public IReadOnlyList<ScreenRegistryEntry> AllEntries => _entries;

        /// <summary>Register a screen programmatically (for testing).</summary>
        public void AddEntry(ScreenRegistryEntry entry) => _entries.Add(entry);
    }

    /// <summary>
    /// A single entry in the ScreenRegistry mapping a screen ID to its prefab.
    /// </summary>
    [Serializable]
    public sealed class ScreenRegistryEntry
    {
        [Tooltip("Screen identifier (use ScreenId constants)")]
        public string ScreenId;

        [Tooltip("Prefab containing the screen's UI and IScreen component")]
        public GameObject Prefab;

        /// <summary>Default constructor for serialization.</summary>
        public ScreenRegistryEntry() { }

        /// <summary>Construct with explicit values.</summary>
        public ScreenRegistryEntry(string screenId, GameObject prefab)
        {
            ScreenId = screenId;
            Prefab = prefab;
        }
    }
}
