#if UNITY_EDITOR
using UnityEngine;

namespace R8EOX.Editor
{
    [CreateAssetMenu(fileName = "OutpostTerrainConfig", menuName = "R8EOX/Editor/Outpost Terrain Config")]
    public class OutpostTerrainConfig : ScriptableObject
    {
        [SerializeField, Tooltip("Terrain width in metres")]
        float _width = 100f;

        [SerializeField, Tooltip("Terrain length in metres")]
        float _length = 100f;

        [SerializeField, Tooltip("Maximum terrain height in metres (heightmap 0-1 maps to 0 to this value)")]
        float _maxHeight = 2f;

        [SerializeField, Tooltip("Dirt texture tile size in metres (lower = more tiling/detail)")]
        float _dirtTileSize = 5f;

        public float Width    => _width;
        public float Length   => _length;
        public float MaxHeight => _maxHeight;
        public float DirtTileSize => _dirtTileSize;
    }
}
#endif
