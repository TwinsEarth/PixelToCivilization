# 脚本化渲染通道

使用 `ScriptableRenderPass` API 编写自定义渲染通道。然后，您可以通过 `RenderPipelineManager` API 或脚本化渲染器特性将该通道注入到通用渲染管线（URP）的渲染循环中。

|页面|描述|
|-|-|
|[Scriptable Render Pass 介绍](intro-to-scriptable-render-passes.md)|什么是脚本化渲染通道，以及如何将其注入到场景中。|
|[编写 Scriptable Render Pass](write-a-scriptable-render-pass.md)|一个使用 `Blit` 创建红色滤镜效果的 `ScriptableRenderPass` 实例示例。|
|[通过脚本注入渲染通道](../customize/inject-render-pass-via-script.md)|使用 `RenderPipelineManager` API 注入渲染通道，而无需使用脚本化渲染器特性。|

## 额外资源

- [通过 Scriptable Renderer Feature 注入 Pass](scriptable-renderer-features/inject-a-pass-using-a-scriptable-renderer-feature.md)