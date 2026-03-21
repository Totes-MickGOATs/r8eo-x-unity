# Editor/Builders/

Editor-only builder scripts for constructing test scenes and prefabs programmatically.
Used by test fixtures and the `OutpostTrackSetup` scene builder.

## Files

| File | Purpose |
|------|---------|
| `AssetHelper.cs` | Utility methods for loading and creating assets in editor scripts |
| `EnvironmentBuilder.cs` | Constructs environment elements (lighting, skybox, post-processing) in test scenes |
| `PhysicsConfigurator.cs` | Configures physics layers, collision matrix, and physics material defaults |
| `RCBuggyBuilder.cs` | Instantiates and wires the RC car prefab hierarchy for test scenes |
| `TerrainBuilder.cs` | Creates and configures terrain objects with surface layers and height maps |
| `TerrainLayerBuilder.cs` | Builds terrain layer assets (texture, normal map, friction settings) |
| `TestTrackBuilder.cs` | Assembles a complete test track scene from primitive components |
| `RCBuggyModularBuilder.cs` | Entry points (menu + batchmode) for nested-prefab decomposition of RCBuggy.prefab |
| `RCBuggyNestedPrefabExtractor.cs` | Low-level prefab extraction helpers used by RCBuggyModularBuilder |
| `TrackSceneModularBuilder.cs` | Entry points (menu + batchmode) for additive-scene decomposition of TestTrack/OutpostTrack |
| `SceneDecomposer.cs` | Low-level scene decomposition utilities used by TrackSceneModularBuilder |
| `SceneModularizationRunner.cs` | Unified batchmode entry point — runs all modularization builders in order |

## Relevant Skills

- **`editor-script-authoring`** — Editor-only C# script patterns and `InitializeOnLoad` conventions
