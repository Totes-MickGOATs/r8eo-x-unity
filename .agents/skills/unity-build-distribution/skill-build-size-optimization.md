# Build Size Optimization

> Part of the `unity-build-distribution` skill. See [SKILL.md](SKILL.md) for the overview.

## Build Size Optimization

### Reading the Build Report

After every build, check the Editor Log for the build report:

**Edit > Preferences > General > Editor Log** (or `%LOCALAPPDATA%/Unity/Editor/Editor.log`)

The report lists asset categories by size. Focus on the largest first (usually Textures, then Audio, then Meshes).

### Texture Compression

| Format | Use Case | Quality | Size |
|--------|----------|---------|------|
| DXT1 | Opaque textures (no alpha) | Good | 4 bpp |
| DXT5 | Textures with alpha | Good | 8 bpp |
| **BC7** | **Best quality for both** | Excellent | 8 bpp |
| Crunch (DXT1) | Download size reduction | Lossy | Variable |
| Crunch (DXT5) | Download size reduction | Lossy | Variable |

- Use **BC7** for quality-critical textures (vehicle liveries, UI)
- Use **Crunch compression** at quality 70-85 for textures where download size matters more than load time
- Crunch adds decompression time at load but significantly reduces download/install size

### Audio Compression

| Format | Use Case | Memory |
|--------|----------|--------|
| **PCM** | Short SFX (< 1s, impacts, clicks) | High (uncompressed) |
| **ADPCM** | Repetitive SFX (engine loops, tire noise) | Medium (3.5:1) |
| **Vorbis** | Music, ambient, long audio | Low (streaming) |

- Set Load Type to **Streaming** for Vorbis music tracks
- Set Load Type to **Decompress On Load** for short PCM SFX
- Set Load Type to **Compressed In Memory** for ADPCM loops

