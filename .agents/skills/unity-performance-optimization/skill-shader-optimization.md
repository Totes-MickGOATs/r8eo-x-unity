# Shader Optimization

> Part of the `unity-performance-optimization` skill. See [SKILL.md](SKILL.md) for the overview.

## Shader Optimization

### Variant Stripping

Shaders compile into many variants (keywords x passes x stages). Strip unused ones:

```csharp
// In IPreprocessShaders implementation
public void OnProcessShader(Shader shader, ShaderSnippetData snippet,
    IList<ShaderCompilerData> data)
{
    for (int i = data.Count - 1; i >= 0; i--)
    {
        // Strip variants with keywords you never use
        if (data[i].shaderKeywordSet.IsEnabled(
            new ShaderKeyword("_DETAIL_MULX2")))
        {
            data.RemoveAt(i);
        }
    }
}
```

### Keyword Limits

Unity has a global limit of ~256 local shader keywords and ~384 global keywords. Keep keyword usage minimal.

### Mobile-Friendly Shader Tips

- Use `half` precision where possible (color, UV, normals)
- Avoid branching in fragment shaders
- Minimize texture samples per fragment
- Use `DisableBatching` tag sparingly

## Async Operations

### Addressables

```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// Async load
var handle = Addressables.LoadAssetAsync<GameObject>("Prefabs/Enemy");
handle.Completed += op =>
{
    if (op.Status == AsyncOperationStatus.Succeeded)
    {
        Instantiate(op.Result);
    }
};

// Don't forget to release
Addressables.Release(handle);
```

### Async Scene Loading

```csharp
public async void LoadGameScene()
{
    var op = SceneManager.LoadSceneAsync("GameScene");
    op.allowSceneActivation = false;

    while (op.progress < 0.9f)
    {
        loadingBar.value = op.progress / 0.9f;
        await Task.Yield();
    }

    loadingBar.value = 1f;
    await Task.Delay(500); // brief pause
    op.allowSceneActivation = true;
}
```

### Job System (Burst-Compiled Parallel Work)

```csharp
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

[BurstCompile]
public struct DistanceJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Vector3> Positions;
    public Vector3 Target;
    public NativeArray<float> Results;

    public void Execute(int index)
    {
        Results[index] = Vector3.Distance(Positions[index], Target);
    }
}

// Schedule
var positions = new NativeArray<Vector3>(1000, Allocator.TempJob);
var results = new NativeArray<float>(1000, Allocator.TempJob);
// ... fill positions ...

var job = new DistanceJob
{
    Positions = positions,
    Target = playerPos,
    Results = results
};

JobHandle handle = job.Schedule(1000, 64); // batch size 64
handle.Complete();

// Use results...
positions.Dispose();
results.Dispose();
```

