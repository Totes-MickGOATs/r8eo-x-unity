# 2. GPU Resident Drawer (GRD)

> Part of the `unity-graphics-pipeline` skill. See [SKILL.md](SKILL.md) for the overview.

## 2. GPU Resident Drawer (GRD)

Unity 6's GPU Resident Drawer replaces manual GPU instancing, static batching, and most SRP Batcher concerns. It keeps mesh data GPU-resident and uses indirect draw calls.

**How to Enable:**
1. URP Asset > Rendering > GPU Resident Drawer: **Enabled**
2. Project Settings > Player > Static Batching: **Disabled** (conflicts with GRD)
3. Keep `BatchRendererGroup` shader variants enabled in URP Asset

**What GRD Supersedes:**
- Manual GPU Instancing (`Graphics.DrawMeshInstanced`) — GRD does this automatically
- Static Batching — GRD handles static geometry more efficiently
- MaterialPropertyBlock instancing workarounds — GRD batches per-instance properties natively

**What GRD Does NOT Replace:**
- SRP Batcher — GRD and SRP Batcher are **complementary**, enable both
- Dynamic Batching — still useful for small meshes (<300 verts) on low-end hardware
- Custom compute draw calls — GRD handles standard MeshRenderer only

**Racing-Specific Notes:**
- Track-side objects (barriers, signs, cones) benefit enormously — hundreds of identical meshes
- Vehicle meshes are typically unique — GRD helps less here, but doesn't hurt
- Vegetation (grass, bushes) should use GRD instead of manual instancing

**Verification:**
- Frame Debugger: look for `BatchRendererGroup.DrawIndirect` calls
- Rendering Debugger: GPU Resident Drawer stats panel shows batch counts

---

