# 自定义渲染与后处理

在 通用渲染管线（URP） 中，你可以自定义和扩展渲染流程。  
URP 通过 Renderer Features 实现特定的渲染效果。它包含一些预构建的 Renderer Features，同时支持创建自定义的 Scriptable Renderer Features。

| 页面 | 描述 |
|----------------------|------------------------------------------------------------|
| [自定义渲染 Pass](renderer-features/custom-rendering-passes.md) | 在 C# 脚本中创建自定义 Render Pass，并将其注入 URP 的渲染循环。 |
| [注入点参考](customize/custom-pass-injection-points.md) | 你可以使用哪些 Injection Points 将 Render Pass 注入 渲染循环。 |
| [Scriptable Renderer Feature 和 Scriptable Render Pass API 参考](renderer-features/scriptable-renderer-features/scriptable-renderer-feature-reference.md) | 编写 Scriptable Renderer Pass 和 Scriptable Renderer Feature 的常用方法。 |


## 额外资源

- [预构建效果（Renderer Features）](urp-renderer-feature.md)
- [如何创建自定义后处理效果](post-processing/post-processing-custom-effect-low-code.md)