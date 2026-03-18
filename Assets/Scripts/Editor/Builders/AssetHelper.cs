#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace R8EOX.Editor.Builders
{
    /// <summary>
    /// Shared asset-database utilities for editor builder classes.
    /// </summary>
    public static class AssetHelper
    {
        /// <summary>
        /// Deletes the existing asset at assetPath before creating a new one.
        /// Only safe for assets that no component references by GUID (e.g. generated
        /// materials). Never use for TerrainData or TerrainLayers — use LoadOrCreate.
        /// </summary>
        public static void SaveOrReplaceAsset(Object obj, string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(obj, assetPath);
        }
    }
}
#endif
