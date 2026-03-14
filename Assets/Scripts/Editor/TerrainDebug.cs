#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class TerrainDebug
{
    [MenuItem("RC Buggy/DEBUG - Test Terrain Heights")]
    static void TestTerrainHeights()
    {
        // First: check if a terrain exists and what its heights look like
        var terrain = Object.FindObjectOfType<Terrain>();
        if (terrain != null)
        {
            var td = terrain.terrainData;
            float[,] h = td.GetHeights(0, 0, td.heightmapResolution, td.heightmapResolution);
            float min = float.MaxValue, max = float.MinValue, sum = 0;
            int res = td.heightmapResolution;
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float v = h[y, x];
                    if (v < min) min = v;
                    if (v > max) max = v;
                    sum += v;
                }
            float avg = sum / (res * res);
            Debug.Log($"[TerrainDebug] EXISTING terrain heights: res={res} min={min:F6} max={max:F6} avg={avg:F6}");
            Debug.Log($"[TerrainDebug] Size={td.size} => height range: {min * td.size.y:F2}m to {max * td.size.y:F2}m");
            
            // Sample specific points
            float centerH = terrain.SampleHeight(new Vector3(0, 0, 0));
            float cornerH = terrain.SampleHeight(terrain.transform.position);
            Debug.Log($"[TerrainDebug] SampleHeight at origin: {centerH:F2}m, at corner: {cornerH:F2}m");
        }
        else
        {
            Debug.Log("[TerrainDebug] No terrain found in scene.");
        }

        // Second: create a simple test terrain with OBVIOUS hills
        Debug.Log("[TerrainDebug] Creating test terrain with obvious hills...");
        var testData = new TerrainData();
        testData.heightmapResolution = 257;
        testData.size = new Vector3(100, 20, 100);
        
        int testRes = testData.heightmapResolution;
        float[,] testH = new float[testRes, testRes];
        for (int y = 0; y < testRes; y++)
            for (int x = 0; x < testRes; x++)
            {
                float u = (float)x / (testRes - 1);
                float v = (float)y / (testRes - 1);
                testH[y, x] = 0.3f + 0.2f * Mathf.Sin(u * Mathf.PI * 4) * Mathf.Cos(v * Mathf.PI * 4);
            }
        
        testData.SetHeights(0, 0, testH);
        
        // Verify it took
        float[,] readBack = testData.GetHeights(0, 0, testRes, testRes);
        float rbMin = float.MaxValue, rbMax = float.MinValue;
        for (int y = 0; y < testRes; y++)
            for (int x = 0; x < testRes; x++)
            {
                if (readBack[y, x] < rbMin) rbMin = readBack[y, x];
                if (readBack[y, x] > rbMax) rbMax = readBack[y, x];
            }
        Debug.Log($"[TerrainDebug] Test terrain readback: min={rbMin:F4} max={rbMax:F4}");
        
        var existing = GameObject.Find("DebugTerrain");
        if (existing != null) Object.DestroyImmediate(existing);
        
        GameObject go = Terrain.CreateTerrainGameObject(testData);
        go.name = "DebugTerrain";
        go.transform.position = new Vector3(-50, 0, -50);
        
        AssetDatabase.CreateAsset(testData, "Assets/Terrain/DebugTerrainData.asset");
        AssetDatabase.SaveAssets();
        
        Debug.Log("[TerrainDebug] Debug terrain created. You should see obvious hills.");
    }
}
#endif
