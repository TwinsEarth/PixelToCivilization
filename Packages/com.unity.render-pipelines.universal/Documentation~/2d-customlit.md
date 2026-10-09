# 2D 自定义光照

2D 渲染器的默认光照模型旨在提供一定的灵活性，以适用于通用用途。

然而，它并非无限灵活，可能无法满足更高级或自定义效果的需求。

现在，您可以创建自己的 2D 光照模型。

## Sprite 自定义光照 Shader Graph

新的 Shader Graph 目标 **“Custom Lit Shader Graph”** 提供了一个很好的起点，可用于创建自定义光照模型的 Shader。该 Shader 不会采样光照纹理（Light Textures），但它具有一个法线（Normal）通道，并在非 2D 渲染器中提供一个回退的前向渲染（Forward）通道。

## 2D 光照纹理

2D 光照纹理是由 **2D 渲染器** 生成的渲染纹理（Render Textures），其中包含场景中的可见光源。在 [2D 渲染器数据](2DRendererData_overview.md) 中，每个混合样式（Blend Style）最多可拥有 4 种光照纹理。

内置的 **Lit Shader** 会采样这些光照纹理，并将其与 **Sprite 纹理** 结合，以创建光照效果。

## 2D 光照纹理节点

要在 Shader Graph 中采样光照纹理，可使用新的 **“2D Light Texture”** 节点。该节点的输出与 **“Texture 2D”** 节点的输出相同，并应连接到 **“Texture Sampler”** 进行采样。

# 使用自定义光照 Shader 创建发光（Emissive）效果

**发光效果** 是使用 **Custom Lit Shader** 创建自定义效果的典型示例。通过结合 **遮罩纹理（Mask Texture）**，可以识别 Sprite 上不应受到光照影响的区域。

**“Secondary Texture”**（次级纹理）功能是加载发光遮罩的理想方式。
