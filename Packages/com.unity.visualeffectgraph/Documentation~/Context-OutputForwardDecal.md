# Output Particle Forward Decal

菜单路径 : **Context > Output Particle Forward Decal**

**Output Particle Forward Decal** 上下文使用贴花渲染粒子系统。贴花是 Visual Effect Graph 将纹理投射到其中的一个盒体。然后，Unity 在沿其 xy 平面的任何相交几何体上渲染纹理。这意味着不与任何几何体相交的贴花粒子均不可见。请注意，这些粒子虽然不可见，但它们仍然影响模拟和渲染系统所需的资源强度。

此输出实现了最简单的贴花形式。它仅限于混合单个反照率纹理并且不点亮。此输出不支持 [Shader Graph](https://docs.unity.cn/cn/Packages-cn/com.unity.shadergraph@latest)。

为 Visual Effect Graph 的未来版本计划了更多贴花功能。

以下是特定于 Output Particle Forward Decal 的设置和属性列表。有关此上下文与所有其他上下文共享的通用输出设置的信息，请参阅 [全局输出设置和属性](Context-OutputSharedSettings.md)。


## 上下文属性

| 输入            | 类型       | 描述                            |
| ---------------- | ---------- | -------------------------------------- |
| **Main Texture** | Texture 2D | 	每个粒子使用的贴花纹理。 |
