# Workflow Tips

> Part of the `unity-3d-world-building` skill. See [SKILL.md](SKILL.md) for the overview.

## Workflow Tips

### Scene Loading for Large Worlds

```csharp
// Additive scene loading — load world sections as needed
SceneManager.LoadSceneAsync("Level_Section_B", LoadSceneMode.Additive);

// Unload when player moves away
SceneManager.UnloadSceneAsync("Level_Section_A");

// Multi-scene editing in Editor:
// File > Open Scene Additive (drag scene into hierarchy)
// Set one scene as Active (lighting target)
```

### Streaming with Addressables

```csharp
// Load terrain chunk on demand
var handle = Addressables.LoadSceneAsync("TerrainChunk_2_3", LoadSceneMode.Additive);
handle.Completed += (op) => { /* chunk loaded */ };

// Unload
Addressables.UnloadSceneAsync(handle);
```

### Editor Gizmo Drawing

```csharp
// Visualize bounds, triggers, paths in Scene view
void OnDrawGizmosSelected()
{
    Gizmos.color = new Color(1, 0, 0, 0.3f);
    Gizmos.DrawCube(transform.position, triggerSize);
    Gizmos.color = Color.red;
    Gizmos.DrawWireCube(transform.position, triggerSize);
}

// Handles (Editor only) for interactive scene tools
#if UNITY_EDITOR
void OnDrawGizmos()
{
    Handles.color = Color.yellow;
    Handles.DrawWireDisc(transform.position, Vector3.up, detectionRadius);
    Handles.Label(transform.position + Vector3.up * 2f, gameObject.name);
}
#endif
```

### Performance Checklist for World Building

```
[ ] Occlusion Culling baked for indoor areas
[ ] LOD Groups on all medium/large props
[ ] Terrain pixel error ≥ 5 (higher for mobile)
[ ] Draw Instanced enabled on terrain
[ ] Static batching flags set on non-moving geometry
[ ] Light Probes placed in areas with dynamic objects
[ ] Reflection Probes: box projection for interiors
[ ] Grass/detail distance tuned (100-150m typical)
[ ] Tree billboard distance set appropriately
[ ] NavMesh obstacles use "Carve Only Stationary"
[ ] Colliders use primitives where possible (not MeshCollider)
[ ] Texture sizes appropriate for object screen size
```
