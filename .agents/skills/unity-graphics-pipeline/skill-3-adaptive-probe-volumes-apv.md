# 3. Adaptive Probe Volumes (APV)

> Part of the `unity-graphics-pipeline` skill. See [SKILL.md](SKILL.md) for the overview.

## 3. Adaptive Probe Volumes (APV)

APV replaces the legacy Light Probe Group workflow with automatic probe placement and sky occlusion support. Essential for day/night cycles or dynamic weather on the track.

**Setup:**
1. Project Settings > Graphics > Lighting: Enable **Adaptive Probe Volumes**
2. Add an `Adaptive Probe Volumes` component to the scene
3. Configure baking settings:
   - Min Subdivision Level: 1-2m for track surfaces (captures road-to-grass transitions)
   - Max Subdivision Level: 4-8m for open sky areas
   - Dilation: Enabled (fills gaps near geometry boundaries)

**Sky Occlusion:**
- Enable for day/night lighting without rebaking
- APV stores sky visibility per probe
- Runtime sky color changes propagate through probes automatically
- Essential for time-of-day racing (dawn, noon, dusk, night)

**Racing-Specific Configuration:**
- Dense probes along the track surface (1m subdivision) — captures shadows from bridges, tunnels, tree canopy
- Sparse probes in open sky areas (4-8m) — no detail needed above the track
- Place probe volumes per track section (tunnel volume, open area volume)
- Streaming: Enable for large tracks — loads probe data per camera position

**Migration from Light Probe Groups:**
- Remove all `Light Probe Group` components
- APV auto-generates equivalent coverage during bake
- Bake time may increase by 20-40% but runtime quality is higher

---

## 4. Render Graph

Render Graph is **mandatory** in Unity 6 URP. All custom render passes must use the new `RecordRenderGraph` API instead of the legacy `Execute` method.

**Key Changes from Legacy:**
- `ScriptableRenderPass.Execute()` is deprecated — use `RecordRenderGraph()`
- Resources are declared as handles (`TextureHandle`, `BufferHandle`)
- The graph automatically manages resource lifetimes and barriers
- Passes that don't produce used outputs are automatically culled

**Writing a Custom Pass:**

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class CustomSpeedBlurPass : ScriptableRenderPass
{
    private class PassData
    {
        public TextureHandle source;
        public TextureHandle destination;
        public float blurStrength;
    }

    public override void RecordRenderGraph(
        RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "Speed Blur", out var passData))
        {
            passData.source = resourceData.activeColorTexture;
            passData.blurStrength = 0.5f;

            var desc = renderGraph.GetTextureDesc(passData.source);
            passData.destination = renderGraph.CreateTexture(desc);

            builder.UseTexture(passData.source, AccessFlags.Read);
            builder.SetRenderAttachment(passData.destination, 0);

            builder.SetRenderFunc(
                (PassData data, RasterGraphContext ctx) =>
                {
                    // Blit with blur material
                });
        }
    }
}
```

**Debugging:**
- Window > Analysis > **Render Graph Viewer** — visualizes the full pass graph
- Shows resource lifetimes, pass dependencies, and culled passes
- Use this FIRST when debugging rendering issues, before Frame Debugger

---

