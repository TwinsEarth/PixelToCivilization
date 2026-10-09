# 了解性能

项目的性能取决于你使用或启用的 Universal Render Pipeline (URP) 特性、场景的内容以及你所针对的平台。

你可以使用 [Unity Profiler](https://docs.unity.cn/cn/tuanjiemanual/Manual/Profiler.html) 或 GPU 分析工具，如 [RenderDoc](https://docs.unity.cn/cn/tuanjiemanual/Manual/RenderDocIntegration.html) 或 [Xcode](https://docs.unity.cn/cn/tuanjiemanual/Manual/XcodeFrameDebuggerIntegration.html)，检查 URP 在项目中使用的内存、CPU 和 GPU 资源。然后，你可以使用这些信息来启用或禁用对性能影响最大的特性和设置。

通常，通过改变以下设置，URP 的性能会更好：

- 减少 URP 使用的 CPU 资源。例如，你可以禁用每帧更新 volumes。
- 减少 URP 存储纹理所用的内存。例如，如果不需要高动态范围（HDR），可以禁用它，从而减少颜色缓冲区的大小。
- 减少 URP 复制到内存中的渲染纹理数量，这对移动平台的影响很大。例如，如果不需要深度纹理，可以禁用 URP 创建深度纹理。
- 减少渲染管线中的渲染通道数量。例如，如果不需要不透明纹理，可以禁用它，或者禁用额外的光源投射阴影。
- 减少 URP 向 GPU 发送的绘制调用数量。例如，你可以启用 SRP Batcher。
- 减少 URP 渲染到屏幕的像素数量，这对移动平台影响较大，因为移动平台的 GPU 性能较弱。例如，你可以减少渲染缩放比例。

有关如何禁用或更改设置以提高性能的更多信息，请参考以下文档：

- [配置以提高性能](configure-for-better-performance.md)
- [优化以提高性能](optimize-for-better-performance.md)
