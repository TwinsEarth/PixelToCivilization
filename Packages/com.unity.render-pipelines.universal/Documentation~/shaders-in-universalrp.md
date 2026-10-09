# 着色器与材质

URP 为常见用例场景提供了以下着色器：

- [Complex Lit](shader-complex-lit.md)
- [Lit](lit-shader.md)
- [Simple Lit](simple-lit-shader.md)
- [Baked Lit](baked-lit-shader.md)
- [Unlit](unlit-shader.md)
- [Terrain Lit](shader-terrain-lit.md)
- [Particles Lit](particles-lit-shader.md)
- [Particles Simple Lit](particles-simple-lit-shader.md)
- [Particles Unlit](particles-unlit-shader.md)
- [SpeedTree](speedtree.md)
- [Decal](decal-shader.md)
- Autodesk Interactive
- Autodesk Interactive Transparent
- Autodesk Interactive Masked

## 着色器兼容性

为内置渲染管线编写的 Lit 及自定义 Lit 着色器与 URP 不兼容。

为内置渲染管线编写的 Unlit 着色器可与 URP 兼容。

要将为内置渲染管线编写的着色器转换为 URP 着色器，请参阅 [升级你的着色器](upgrading-your-shaders.md) 页面。

## 选择合适的着色器

通用渲染管线 (URP) 实现了基于物理的渲染（PBR）。

管线提供了预构建着色器，用于模拟现实世界材质。

PBR 材质提供一组参数，使美术人员在不同材质类型与不同光照条件下获得一致的外观。

URP 的 [Lit Shader](lit-shader.md) 适合模拟大多数现实世界材质。[Complex Lit Shader](shader-complex-lit.md) 适合对需要更复杂光照计算的高级材质进行模拟，例如透明涂层（clear coat）效果。

URP 提供 [Simple Lit Shader](simple-lit-shader.md) 来帮助将基于内置渲染管线的非 PBR 项目转换为 URP。该着色器非 PBR，并且 Shader Graph 不支持它。

如果你不需要实时光照，或者只想使用[烘焙光照](https://docs.unity.cn/cn/tuanjiemanual/Manual/LightMode-Baked.html)并采样全局光照，可以选择 Baked Lit Shader。

如果你不需要材质上的光照，可以选择 Unlit Shader。

## SRP Batcher 兼容性

若要确保着色器与 SRP Batcher 兼容，需要满足以下要求：
- 在名为 `UnityPerMaterial` 的单个 CBUFFER 中声明所有材质属性。
- 在名为 `UnityPerDraw` 的单个 CBUFFER 中声明所有内置引擎属性，例如 `unity_ObjectToWorld` 或 `unity_WorldTransformParams`。

有关 SRP Batcher 的更多信息，请参阅 [Scriptable Render Pipeline (SRP) Batcher](https://docs.unity.cn/cn/tuanjiemanual/Manual/SRPBatcher.html) 页面。
