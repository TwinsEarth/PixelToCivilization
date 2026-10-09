# URP blit 最佳实践

Blit 操作是将源纹理复制到目标纹理的过程。

本页概述了在 URP 中执行 blit 操作的不同方式，以及在编写自定义渲染通道时应遵循的最佳实践。

## 传统的 CommandBuffer.Blit API

避免在 URP 项目中使用 [CommandBuffer.Blit](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/Rendering.CommandBuffer.Blit.html) API。

[CommandBuffer.Blit](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/Rendering.CommandBuffer.Blit.html) API 是传统 API。它隐式运行与更改状态、绑定纹理和设置渲染目标相关的额外操作。这些操作在 SRP 项目中在后台发生，对用户不透明。

该 API 与 URP XR 集成存在兼容性问题。使用 `cmd.Blit` 可能会隐式启用或禁用 XR 着色器关键字，从而破坏 XR SPI 渲染。

[CommandBuffer.Blit](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/Rendering.CommandBuffer.Blit.html) API 与 `NativeRenderPass` 和 `RenderGraph` 不兼容。

类似的考虑适用于任何内部依赖 `cmd.Blit` 的实用程序或包装器，`RenderingUtils.Blit` 就是其中之一。

## SRP Blitter API

在 URP 项目中使用 [Blitter API](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.core@latest/api/UnityEngine.Rendering.Blitter.html)。该 API 不依赖传统逻辑，并且与 XR、原生渲染通道和其他 SRP API 兼容。

## 自定义全屏 blit 示例

[如何在 URP 中执行全屏 blit](../renderer-features/how-to-fullscreen-blit.md) 示例展示了如何创建执行全屏 blit 的自定义渲染器特性。该示例在 XR 中工作，并且与 SRP API 兼容。