# Custom Function Nodes

> Part of the `unity-shaders` skill. See [SKILL.md](SKILL.md) for the overview.

## Custom Function Nodes

Bridge between Shader Graph and raw HLSL.

### Inline (String) Mode

```
Create Custom Function node > Type: String

Name: SimpleRemap
Inputs: In (Float), InMin (Float), InMax (Float), OutMin (Float), OutMax (Float)
Outputs: Out (Float)

Body:
Out = OutMin + (In - InMin) * (OutMax - OutMin) / (InMax - InMin);
```

### File Mode

```hlsl
// Assets/Shaders/Includes/CustomLighting.hlsl

// Function signature must match node configuration exactly
void MainLight_float(float3 WorldPos, out float3 Direction, out float3 Color,
                     out float DistanceAtten, out float ShadowAtten)
{
#ifdef SHADERGRAPH_PREVIEW
    Direction = float3(0.5, 0.5, 0);
    Color = 1;
    DistanceAtten = 1;
    ShadowAtten = 1;
#else
    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    Light mainLight = GetMainLight(shadowCoord);
    Direction = mainLight.direction;
    Color = mainLight.color;
    DistanceAtten = mainLight.distanceAttenuation;
    ShadowAtten = mainLight.shadowAttenuation;
#endif
}
```

The `SHADERGRAPH_PREVIEW` guard provides fallback values for the Shader Graph preview window.

## Full Custom Shaders (.shader Files)

For maximum control beyond Shader Graph capabilities.

### URP Shader Structure

```hlsl
Shader "Custom/MyShader"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // SRP Batcher compatible CBUFFER
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes  // vertex input
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings  // vertex output / fragment input
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // Simple Lambert diffuse
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 diffuse = baseColor.rgb * mainLight.color * NdotL;
                half3 ambient = SampleSH(input.normalWS) * baseColor.rgb;

                return half4(diffuse + ambient, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster pass (required for object to cast shadows)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Minimal shadow caster implementation
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
```

### Key URP Includes

```hlsl
Core.hlsl          // Transform functions, common macros, space conversions
Lighting.hlsl      // GetMainLight(), GetAdditionalLight(), PBR functions
Shadows.hlsl       // Shadow sampling, TransformWorldToShadowCoord()
ShaderVariablesFunctions.hlsl  // Camera data, fog
DeclareDepthTexture.hlsl       // _CameraDepthTexture access
```

## Render Features (URP)

Custom rendering passes injected into the URP pipeline.

### ScriptableRenderPass

```csharp
public class OutlineRenderPass : ScriptableRenderPass
{
    private Material _outlineMaterial;
    private FilteringSettings _filterSettings;
    private ShaderTagId _shaderTag = new ShaderTagId("UniversalForward");

    public OutlineRenderPass(Material material, LayerMask layerMask)
    {
        _outlineMaterial = material;
        _filterSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
        renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("OutlinePass");

        // Draw objects with outline material
        var drawSettings = CreateDrawingSettings(_shaderTag, ref renderingData,
            SortingCriteria.CommonOpaque);
        drawSettings.overrideMaterial = _outlineMaterial;

        context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _filterSettings);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
```

### ScriptableRendererFeature

```csharp
public class OutlineFeature : ScriptableRendererFeature
{
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private LayerMask outlineLayer;

    private OutlineRenderPass _pass;

    public override void Create()
    {
        _pass = new OutlineRenderPass(outlineMaterial, outlineLayer);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (outlineMaterial != null)
            renderer.EnqueuePass(_pass);
    }
}
```

Add to: **URP Renderer Data Asset > Add Renderer Feature > OutlineFeature**.

### Full Screen Pass Renderer Feature

Built-in URP feature for post-processing-style effects:

```
1. Create full-screen shader (Shader Graph: Fullscreen Shader Graph)
2. Create material from shader
3. URP Renderer Data > Add Renderer Feature > Full Screen Pass Renderer Feature
4. Assign material, set injection point
```

## Compute Shaders

GPU-accelerated parallel computation — not for rendering, but for data processing.

### Basic Compute Shader

```hlsl
// Assets/Shaders/ParticleCompute.compute

#pragma kernel CSMain

struct Particle
{
    float3 position;
    float3 velocity;
    float life;
};

RWStructuredBuffer<Particle> particles;
float deltaTime;
float3 gravity;

[numthreads(256, 1, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    Particle p = particles[id.x];

    p.velocity += gravity * deltaTime;
    p.position += p.velocity * deltaTime;
    p.life -= deltaTime;

    particles[id.x] = p;
}
```

### Dispatching from C#

```csharp
public class ParticleSystem : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    private ComputeBuffer _particleBuffer;
    private int _kernelIndex;
    private const int PARTICLE_COUNT = 65536;
    private const int THREAD_GROUP_SIZE = 256;

    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float life;
    }

    void Start()
    {
        // Create buffer
        _particleBuffer = new ComputeBuffer(PARTICLE_COUNT,
            sizeof(float) * 7);  // 3+3+1 floats

        // Initialize particles
        Particle[] particles = new Particle[PARTICLE_COUNT];
        // ... initialize data ...
        _particleBuffer.SetData(particles);

        _kernelIndex = computeShader.FindKernel("CSMain");
    }

    void Update()
    {
        computeShader.SetBuffer(_kernelIndex, "particles", _particleBuffer);
        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetVector("gravity", Physics.gravity);

        int threadGroups = Mathf.CeilToInt(PARTICLE_COUNT / (float)THREAD_GROUP_SIZE);
        computeShader.Dispatch(_kernelIndex, threadGroups, 1, 1);
    }

    void OnDestroy()
    {
        _particleBuffer?.Release();
    }
}
```

### RWTexture for Image Processing

```hlsl
#pragma kernel Blur

RWTexture2D<float4> Result;
Texture2D<float4> Input;
int _Width;
int _Height;

[numthreads(8, 8, 1)]
void Blur(uint3 id : SV_DispatchThreadID)
{
    if (id.x >= (uint)_Width || id.y >= (uint)_Height) return;

    float4 sum = 0;
    for (int y = -1; y <= 1; y++)
        for (int x = -1; x <= 1; x++)
            sum += Input[id.xy + int2(x, y)];

    Result[id.xy] = sum / 9.0;
}
```

## Shader Variants

### Keywords

```hlsl
// Global variant — always compiled
#pragma multi_compile _ _RAIN_ON _SNOW_ON

// Material variant — stripped if unused
#pragma shader_feature _DETAIL_MAP

// Local keywords (don't count against global limit of 384)
#pragma multi_compile_local _ _USE_VERTEX_COLORS
#pragma shader_feature_local _TRIPLANAR

// Usage in shader
#if defined(_RAIN_ON)
    // wet surface logic
#elif defined(_SNOW_ON)
    // snow accumulation logic
#endif
```

### Variant Stripping

```csharp
// Editor script to strip unused variants at build time
class ShaderVariantStripper : IPreprocessShaders
{
    public int callbackOrder => 0;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet,
        IList<ShaderCompilerData> data)
    {
        for (int i = data.Count - 1; i >= 0; i--)
        {
            // Strip variants with keywords you never use
            if (data[i].shaderKeywordSet.IsEnabled(
                new ShaderKeyword("_DETAIL_MAP")))
            {
                data.RemoveAt(i);
            }
        }
    }
}
```

### Monitoring Variant Count

```
Edit > Project Settings > Graphics > Shader Stripping
  - Log variant counts during build

Window > Analysis > Shader Variants (Unity 2023+)
  - See total variant count per shader
  - Identify bloated shaders
```

## Performance

### Instruction Budget Guidelines

| Platform | Target | Max Texture Samples |
|----------|--------|-------------------|
| Mobile (low-end) | < 64 ALU instructions | 4 |
| Mobile (high-end) | < 128 ALU instructions | 8 |
| Desktop | < 256 ALU instructions | 16 |
| Console | < 512 ALU instructions | 32 |

### Optimization Tips

```
1. Minimize texture samples — each sample has latency
2. Use half precision where possible (half instead of float)
3. Avoid dynamic branching on mobile (use step/lerp instead of if/else)
4. Pre-compute in vertex shader, interpolate to fragment
5. Use texture LOD (tex2Dlod) when mip level is known
6. Pack data into fewer textures (RGBA channels)
7. Avoid pow() on mobile — use approximations
8. Reduce overdraw: Opaque before Transparent, sort by depth
```

### Half Precision

```hlsl
// Use half for color, UV, and normalized vectors
half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
half3 normal = normalize(input.normalWS);
half NdotL = saturate(dot(normal, lightDir));

// Use float for position, depth, and precision-sensitive math
float3 worldPos = input.positionWS;
float depth = input.positionCS.z;
```

## Shader Debugging

### Frame Debugger

```
Window > Analysis > Frame Debugger
  - Step through each draw call
  - See shader, material, properties, render state
  - Identify overdraw, batch breaks, pass order
  - Check individual render targets
```

### RenderDoc Integration

```
1. Install RenderDoc (free, open source)
2. In Unity: hold Alt+F12 or use RenderDoc overlay
3. Capture a frame
4. Inspect:
   - Every draw call with full state
   - Shader input/output textures
   - Per-pixel shader debugging (step through HLSL)
   - Mesh data, buffer contents
   - Performance timings per draw call
```

### Shader Error Debugging

```
Common errors:
  - "undeclared identifier" — missing include or CBUFFER declaration
  - "cannot convert" — precision mismatch (half vs float)
  - "SRP Batcher: not compatible" — properties not in CBUFFER
  - Pink material — shader compile error, check Console

Debug output:
  - Temporarily output a value as color: return float4(value, 0, 0, 1);
  - Check normals: return float4(normalWS * 0.5 + 0.5, 1);
  - Check UVs: return float4(input.uv, 0, 1);
```

### SRP Batcher Compatibility

```
Requirements for SRP Batcher:
  1. All material properties in a single CBUFFER named UnityPerMaterial
  2. All built-in engine properties in UnityPerDraw (automatic with includes)
  3. No per-material structured buffers

Check: select material > Inspector shows "SRP Batcher: compatible/not compatible"
```

