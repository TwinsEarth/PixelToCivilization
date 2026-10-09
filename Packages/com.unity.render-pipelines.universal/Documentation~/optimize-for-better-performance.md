# 优化以提高性能

如果你的通用渲染管线（URP）项目性能较慢，可以分析项目并调整设置以提高性能。

## 使用 Unity Profiler 分析项目

你可以使用 [Unity Profiler](https://docs.unity.cn/cn/tuanjiemanual/Manual/Profiler.html) 来获取项目在 CPU、内存等方面的性能数据。

## Profiler 标记（Profiler Markers）

下表列出了 Unity Profiler 中影响 URP 帧性能的重要标记。

如果某个标记位于 Profiler 层级较深的位置，或者标记名称已清楚说明 URP 的功能，则不会在表中列出。

| Marker | Sub-marker | 描述 |
|-|-|-|
| **Inl_UniversalRenderPipeline.RenderSingleCameraInternal** || URP 在 [`ScriptableRenderContext`](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/Rendering.ScriptableRenderContext.html) 中为单个相机构建渲染命令列表。此标记仅记录渲染命令，但不会执行它们。标记名称包含相机名称，例如 **Main Camera**。 |
|| **Inl_ScriptableRenderer.Setup** | URP 进行渲染前的准备工作，例如为相机和阴影贴图准备渲染纹理。 |
|| **CullScriptable** | URP 生成要渲染的 GameObject 和光源列表，并剔除（排除）超出相机视野的对象。此过程的耗时取决于场景中的 GameObject 和光源数量。 |
| **Inl_ScriptableRenderContext.Submit** || URP 将 `ScriptableRenderContext` 中的命令列表提交到图形 API。<br/>如果 URP 在每帧中多次提交命令，或者你在代码中调用 [`ScriptableRenderContext.Submit`](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/Rendering.ScriptableRenderContext.Submit.html)，则该标记可能会多次出现。 |
|| **MainLightShadow** | URP 为主定向光（Directional Light）渲染[阴影贴图](https://docs.unity.cn/cn/tuanjiemanual/Manual/shadow-mapping.html)。 |
|| **AdditionalLightsShadow** | URP 为其他光源渲染阴影贴图。 |
|| **UberPostProcess** | URP 渲染启用的[后处理效果](EffectList.md)。该标记包含某些后处理效果的单独标记。 |
|| **RenderLoop.DrawSRPBatcher** | URP 使用 [Scriptable Render Pipeline Batcher](https://docs.unity.cn/cn/tuanjiemanual/Manual/SRPBatcher.html) 渲染一个或多个对象批次。 |
| **CopyColor** || URP 将颜色缓冲区从一个渲染纹理复制到另一个。你可以在 [URP 资源（URP Asset）](universalrp-asset.md) 中禁用 **Opaque Texture**，这样 URP 仅在需要时复制颜色缓冲区。 |
| **CopyDepth** || URP 将深度缓冲区从一个渲染纹理复制到另一个。<br/>除非你需要深度纹理（例如，使用依赖于场景深度的 Shader），否则可以在 [URP 资源](universalrp-asset.md) 中禁用 **Depth Texture**。 |
| **FinalBlit** || URP 将渲染纹理复制到当前相机的渲染目标。 |


## 使用 GPU Profiler 分析项目

你可以使用平台级 GPU Profiler（如 [Xcode](https://docs.unity.cn/cn/tuanjiemanual/Manual/XcodeFrameDebuggerIntegration.html)）来获取 GPU 在渲染过程中的性能数据。你还可以使用 [RenderDoc](https://docs.unity.cn/cn/tuanjiemanual/Manual/RenderDocIntegration.html) 等 Profiler，不过它可能提供的性能数据不够精确。

GPU Profiler 提供的数据包括 URP 渲染事件的标记，例如不同的渲染通道。

## 使用其他工具分析项目

你还可以使用以下工具来分析项目性能：

- [Scene 视图选项](https://docs.unity.cn/cn/tuanjiemanual/Manual/ViewModes.html)
- [渲染调试器（Rendering Debugger）](features/rendering-debugger.md)
- [帧调试器（Frame Debugger）](https://docs.unity.cn/cn/tuanjiemanual/Manual/frame-debugger-window.html)

## 调整设置

根据分析结果，你可以在 [URP 资源（Universal Render Pipeline Asset）](universalrp-asset.md) 或 [URP 渲染器资源（Universal Renderer Asset）](urp-universal-renderer.md) 中调整以下设置，以优化项目性能。

根据你的项目或目标平台的不同，某些设置可能不会产生显著影响。此外，可能还有其他设置会影响项目的性能。

| **设置** | **所在位置** | **优化建议** |
| ------------------------------------------- | ------------------------------ | --------------------------------------------------------------------------- |
| **Accurate G-buffer normals** | [URP 渲染器](urp-universal-renderer.md) > **Rendering** | 如果使用延迟渲染路径（Deferred），请禁用此选项 |
| **Additional Lights** > **Cast Shadows** | [URP 资源](universalrp-asset.md) > **Lighting** | 禁用 |
| **Additional Lights** > **Cookie Atlas Format** | URP 资源 > **Lighting** | 设置为 **Color Low** |
| **Additional Lights** > **Cookie Atlas Resolution** | URP 资源 > **Lighting** | 设为可接受的最低值 |
| **Additional Lights** > **Per Object Limit** | URP 资源 > **Lighting** | 设为可接受的最低值（如果使用延迟或 Forward+ 渲染路径，此设置无效） |
| **Additional Lights** > **Shadow Atlas Resolution** | URP 资源 > **Lighting** | 设为可接受的最低值 |
| **Additional Lights** > **Shadow Resolution** | URP 资源 > **Lighting** | 设为可接受的最低值 |
| **Cascade Count** | URP 资源 > **Shadows** | 设为可接受的最低值 |
| **Conservative Enclosing Sphere** | URP 资源 > **Shadows** | 启用 |
| **Technique** | [Decal 渲染器功能](renderer-feature-decal.md) | 设为 **Screen Space**，并将 **Normal Blend** 设置为 **Low** 或 **Medium** |
| **Fast sRGB/Linear conversion** | URP 资源 > **Post Processing** | 启用 |
| **Grading Mode** | URP 资源 > **Post Processing** | 设置为 **Low Dynamic Range** |
| **LOD Cross Fade Dither** | URP 资源 > **Quality** | 设置为 **Bayer Matrix** |
| **LUT size** | URP 资源 > **Post Processing** | 设为可接受的最低值 |
| **Main Light** > **Cast Shadows** | URP 资源 > **Lighting** | 禁用 |
| **Max Distance** | URP 资源 > **Shadows** | 降低 |
| **Opaque Downsampling** | URP 资源 > **Rendering** | 如果在 URP 资源中启用了 **Opaque Texture**，请设置为 **4x Bilinear** |
| **Render Scale** | URP 资源 > **Quality** | 设为小于 1.0 |
| **Soft Shadows** | URP 资源 > **Shadows** | 禁用，或设为 **Low** |
| **Upscaling Filter** | URP 资源 > **Quality** | 设置为 **Bilinear** 或 **Nearest-Neighbor** |

有关更多设置信息，请参考：

- [URP 中的延迟渲染路径](rendering/deferred-rendering-path.md)
- [Forward+ 渲染路径](rendering/forward-plus-rendering-path.md)
- [Decal 渲染器功能](renderer-feature-decal.md)
- [通用渲染管线资源（URP 资源）](universalrp-asset.md)
- [URP 渲染器](urp-universal-renderer.md)

## 其他资源

- [了解 URP 性能](understand-performance.md)
- [优化配置以提高性能](configure-for-better-performance.md)
- [图形性能和分析](https://docs.unity.cn/cn/tuanjiemanual/Manual/graphics-performance-profiling.html)
- [游戏性能分析最佳实践](https://unity.com/how-to/best-practices-for-profiling-game-performance)
- [性能分析和调试工具](https://unity.com/how-to/profiling-and-debugging-tools)
- [原生 CPU 性能分析：优化游戏性能的技巧](https://resources.unity.com/games/native-cpu-profiling-tips-to-optimize-your-game-performance)
