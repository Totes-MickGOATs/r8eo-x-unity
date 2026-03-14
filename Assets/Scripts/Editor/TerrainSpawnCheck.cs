#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class TerrainSpawnCheck
{
    [MenuItem("RC Buggy/DEBUG - Check Car Spawn")]
    static void CheckCarSpawn()
    {
        var terrain = Object.FindObjectOfType<Terrain>();
        var car = GameObject.Find("RCBuggy");
        
        if (terrain == null) { Debug.LogError("[SpawnCheck] No terrain found!"); return; }
        if (car == null) { Debug.LogError("[SpawnCheck] No RCBuggy found!"); return; }
        
        Vector3 carPos = car.transform.position;
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        
        // Check car is within terrain bounds
        float minX = terrainPos.x;
        float maxX = terrainPos.x + terrainSize.x;
        float minZ = terrainPos.z;
        float maxZ = terrainPos.z + terrainSize.z;
        
        bool inBounds = carPos.x >= minX && carPos.x <= maxX && 
                        carPos.z >= minZ && carPos.z <= maxZ;
        
        Debug.Log($"[SpawnCheck] Terrain bounds: X=[{minX}, {maxX}] Z=[{minZ}, {maxZ}]");
        Debug.Log($"[SpawnCheck] Car position: ({carPos.x:F2}, {carPos.y:F2}, {carPos.z:F2})");
        Debug.Log($"[SpawnCheck] Car within terrain bounds: {inBounds}");
        
        if (!inBounds)
        {
            Debug.LogError("[SpawnCheck] CAR IS OUTSIDE TERRAIN! It will fall through.");
            return;
        }
        
        // Sample terrain height at car position
        float terrainH = terrain.SampleHeight(carPos);
        float worldTerrainH = terrainH + terrainPos.y;
        float clearance = carPos.y - worldTerrainH;
        
        Debug.Log($"[SpawnCheck] Terrain height at car: {terrainH:F3}m (world: {worldTerrainH:F3}m)");
        Debug.Log($"[SpawnCheck] Car clearance above terrain: {clearance:F3}m");
        
        if (clearance < 0)
            Debug.LogError($"[SpawnCheck] CAR IS BELOW TERRAIN by {-clearance:F3}m!");
        else if (clearance > 2.0f)
            Debug.LogWarning($"[SpawnCheck] Car is {clearance:F1}m above terrain - will drop when Play starts.");
        else
            Debug.Log($"[SpawnCheck] GOOD - Car is {clearance:F3}m above terrain surface.");
        
        // Raycast down from car to verify collider
        RaycastHit hit;
        if (Physics.Raycast(carPos + Vector3.up * 10, Vector3.down, out hit, 50f))
        {
            Debug.Log($"[SpawnCheck] Raycast hit: '{hit.collider.name}' at y={hit.point.y:F3}m " +
                $"(distance: {hit.distance:F3}m, collider type: {hit.collider.GetType().Name})");
        }
        else
        {
            Debug.LogError("[SpawnCheck] RAYCAST MISSED - No collider below car! It WILL fall through.");
        }
        
        // Check terrain collider is enabled
        var tc = terrain.GetComponent<TerrainCollider>();
        if (tc == null)
            Debug.LogError("[SpawnCheck] No TerrainCollider component!");
        else if (!tc.enabled)
            Debug.LogError("[SpawnCheck] TerrainCollider is DISABLED!");
        else if (tc.terrainData == null)
            Debug.LogError("[SpawnCheck] TerrainCollider has NO terrainData!");
        else
            Debug.Log($"[SpawnCheck] TerrainCollider: enabled, terrainData={tc.terrainData.name}");
        
        // Check car layer vs terrain layer for physics collision
        int carLayer = car.layer;
        Debug.Log($"[SpawnCheck] Car layer: {carLayer} ({LayerMask.LayerToName(carLayer)})");
        Debug.Log($"[SpawnCheck] Terrain layer: {terrain.gameObject.layer} ({LayerMask.LayerToName(terrain.gameObject.layer)})");
        
        // Check if car layer can collide with terrain layer
        bool canCollide = !Physics.GetIgnoreLayerCollision(carLayer, terrain.gameObject.layer);
        Debug.Log($"[SpawnCheck] Layers can collide: {canCollide}");
        
        if (!canCollide)
            Debug.LogError("[SpawnCheck] CAR AND TERRAIN LAYERS CANNOT COLLIDE! Check Physics layer matrix.");
    }
}
#endif