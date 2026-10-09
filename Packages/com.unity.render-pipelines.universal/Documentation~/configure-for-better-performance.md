# 配置以提高性能

你可以禁用或调整 **通用渲染管线（URP）** 中影响性能较大的设置和功能，以提升项目的运行效率，特别是在低端平台上。

根据你的项目需求或目标平台，下列因素可能会对性能产生最大影响：

- 选择的 渲染路径（Rendering Path）
- URP 的内存使用情况
- CPU 处理时间
- GPU 处理时间

你可以使用 [**Unity Profiler**](https://docs.unity.cn/cn/tuanjiemanual/Manual/Profiler.html) 或 **GPU 性能分析工具**（如 [RenderDoc](https://docs.unity.cn/cn/tuanjiemanual/Manual/RenderDocIntegration.html) 或 [Xcode](https://docs.unity.cn/cn/tuanjiemanual/Manual/XcodeFrameDebuggerIntegration.html)）来衡量不同设置对项目性能的影响。

**注意**：如果你的项目依赖某些功能，你可能无法禁用它们。

## 选择渲染路径

URP 提供三种渲染路径，每种路径在性能和功能上各有特点。  
请参考 [URP 通用渲染器（Universal Renderer）](urp-universal-renderer.md) 了解它们的性能影响和限制。


## 降低 URP 的内存占用

你可以在 URP 资源（URP Asset） 中进行以下优化：

- **禁用 Depth Texture**（深度纹理）——如果不需要深度采样（如 Shader 依赖深度信息），则关闭它，以减少存储和计算深度纹理的开销。
- **禁用 Opaque Texture**（不透明纹理）——如果 URP 不需要存储场景中不透明对象的快照，则禁用该选项。
- **禁用 Rendering Layers**（仅适用于 **Deferred 渲染路径**）——关闭该选项，以减少一个额外的渲染目标（Render Target）。
- **禁用 HDR（高动态范围）**——如果项目不需要 HDR，禁用该选项可减少 HDR 计算。如果需要 HDR，请将 HDR Precision 设为 32 位。
- **降低 Main Light > Shadow Resolution**（主光源阴影分辨率）——减少主光源阴影贴图的大小，以降低内存占用。
- **降低 Additional Lights > Shadow Atlas Resolution**（额外光源的阴影贴图分辨率）——减少额外光源的阴影贴图大小，以降低内存占用。
- **禁用 Light Cookies**（光照 Cookie 贴图），或**降低 Cookie Atlas Resolution 和 Cookie Atlas Format**，以减少内存和计算开销。
- 在低端移动平台上，将 **Store Actions** 设为 **Auto** 或 **Discard**，以减少渲染目标在各个 Pass 之间的内存带宽消耗。

在 **通用渲染器资源（Universal Renderer Asset）** 中：
- 将 **Intermediate Texture** 设为 **Auto**，让 Unity 仅在必要时使用中间纹理（Intermediate Texture）。这可能会减少 GPU 的内存带宽占用。
- 使用 **Frame Debugger**（[帧调试器](https://docs.unity.cn/cn/tuanjiemanual/Manual/frame-debugger-window.html)）检查 URP 是否移除了不必要的中间纹理。

额外的优化措施：
- 尽量减少 Decal Renderer Feature 的使用——URP 需要额外的渲染 Pass 来处理 Decals，这会增加 CPU/GPU 开销。详见 [Decal Renderer Feature](renderer-feature-decal.md)。
- 剥离不需要的 Shader 变体（[Shader Stripping](shader-stripping.md)），以减少内存占用和加载时间。


## 降低 CPU 处理时间

在 URP 资源 中，你可以进行以下优化：

- **将 Volume Update Mode 设为 Via Scripting**——这样 URP 不会每帧更新 Volume，你需要通过 API（如 [UpdateVolumeStack](xref:UnityEngine.Rendering.Universal.CameraExtensions.UpdateVolumeStack(UnityEngine.Camera))）手动更新。
- 在低端移动平台上，如果使用 [Reflection Probes](lighting/reflection-probes.md)，**禁用 Probe Blending 和 Box Projection** 以减少计算量。
- 在 Shadows（阴影）部分：
  - **降低 Max Distance**（最大阴影距离）——减少阴影 Pass 需要处理的对象数量，同时降低 GPU 计算负担。
  - **降低 Cascade Count**（阴影级联数量）——减少渲染 Pass，降低 CPU 和 GPU 负担。
- 在 Additional Lights（额外光源）部分：
  - **禁用 Cast Shadows**（额外光源投射阴影）——降低 CPU/GPU 计算开销，减少内存占用。

**减少场景中的相机数量**——每个相机都会增加 URP 剔除 和 渲染计算，尽量减少不必要的相机可提高 CPU/GPU 性能。


## 降低 GPU 处理时间

在 URP 资源 中，你可以进行以下优化：

- **降低或禁用 Anti-aliasing (MSAA)**——减少 MSAA 使用的内存带宽，这同时降低 GPU 计算和内存占用。
- **禁用 Terrain Holes**（地形洞）。
- **启用 SRP Batcher**——减少 GPU Draw Call 之间的切换，并使 Material 数据 在 GPU 内存 中保持持久化。请确保你的 Shader 兼容 [SRP Batcher](https://docs.unity.cn/cn/tuanjiemanual/Manual/SRPBatcher.html)。
- 在低端移动平台上：
  - **禁用 LOD Cross Fade**——减少 LOD 过渡的 alpha 测试计算。
  - **将 Additional Lights 设为 Disabled 或 Per Vertex**（如果使用 Forward 渲染路径）——减少光照计算负担，同时减少 CPU 处理开销（设为 Disabled 时）。

在 通用渲染器资源（Universal Renderer Asset） 中：
- **启用 Native RenderPass**（适用于 Vulkan、Metal 或 DirectX 12）——让 URP 自动减少 Render Textures 在内存中的复制操作，同时减少内存使用量。
- 在 PC 和主机平台上：
  - 如果使用 Forward 或 Forward+ 渲染路径，将 **Depth Priming Mode** 设为 **Auto** 或 **Forced**，以利用深度纹理优化 像素着色 计算。
  - 将 **Depth Texture Mode** 设为 **After Transparents**，以避免在不透明 Pass 和透明 Pass 之间频繁切换渲染目标。

额外的优化措施：
- 避免使用 [Complex Lit Shader](shader-complex-lit.md)，因为它包含复杂的光照计算。
  - 如果使用 Complex Lit Shader，建议禁用 Clear Coat。
- 在低端移动平台上：
  - 使用 Baked Lit Shader 处理静态对象。
  - 使用 Simple Lit Shader 处理动态对象。
- 如果使用 SSAO（屏幕空间环境光遮蔽），请参考 [Ambient Occlusion](post-processing-ssao.md)，调整对性能影响较大的设置。


## 额外资源

- [理解 URP 性能优化](understand-performance.md)
- [优化以提高性能](optimize-for-better-performance.md)
- [高级 Unity 开发者的 URP 介绍](https://resources.unity.com/games/introduction-universal-render-pipeline-for-advanced-unity-creators)
- [高端 PC 和主机平台的性能优化](https://unity.com/how-to/performance-optimization-high-end-graphics)
- [Alba 游戏开发案例：如何构建高效的开放世界游戏](https://www.youtube.com/watch?v=YOtDVv5-0A4)
- [移动设备上的 URP 后处理优化](integration-with-post-processing.md)
- [优化光照以提升帧率](https://unity.com/how-to/advanced/optimize-lighting-mobile-games)

有关设置的详细信息，请参考：
- [URP 的延迟渲染路径](rendering/deferred-rendering-path.md)
- [URP 的 Forward+ 渲染路径](rendering/forward-plus-rendering-path.md)
- [URP 资源](universalrp-asset.md)
- [通用渲染器](urp-universal-renderer.md)
