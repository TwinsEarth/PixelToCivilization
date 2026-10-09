# Output Particle HDRP 光照贴花

**Output Particle HDRP 光照贴花l**上下文使用贴花来渲染粒子系统。贴花是 Visual Effect Graph 将纹理投影到的框。Unity 在沿贴花 xy 平面相交的任何几何体上渲染该纹理。这意味着不与任何几何体相交的贴花粒子不可见。当贴花不可见时，它仍然会增加模拟和渲染系统所需的资源强度。

此上下文可以使用 Base Color map （albedo）、Normal Map 或 Mask Map 将其属性投影到表面上。它不支持 [Shader Graph](https://docs.unity.cn/cn/Packages-cn/com.unity.shadergraph@latest)。要使用此上下文，请在 [HDRP 资源](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/HDRP-Asset.html) 和 [HDRP 设置](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/Frame-Settings.html)中启用 Decals。

菜单路径 : **Context > Output Particle HDRP Lit Decal**

**Output Particle HDRP Lit Decal** 上下文 可以影响其投影到的表面的以下属性：

- Base Color
- Metalness
- Ambient Occlusion
- Smoothness
- Normal

粒子沿其 Z 轴正向投影其属性。

以下是特定于 Output Particle HDRP 光照贴花上下文的设置和属性列表。有关此上下文与所有其他上下文共享的通用输出设置的信息，请参阅 [Output Lit Settings 和 Properties](Context-OutputLitSettings.md)。

# 上下文设置

| **输入**                    | **类型** | **描述**                                              |
| ---------------------------- | -------- | ------------------------------------------------------------ |
| **Normal Opacity Channel**   | Enum     | 使用此下拉列表选择控制法线贴图不透明度的贴图：• **Base Color Map Alpha**: 使用**Base Map**颜色选择器的 Alpha 通道来控制不透明度。• **Mask Map Blue**: 使用**蒙版贴图**的蓝色通道控制不透明度。 |
| **Mask Opacity Channel**     | Enum     | 使用此下拉列表可选择**蒙版贴图**不透明度的来源：• **Base Color Map Alpha**: 使用**Base Map**颜色选择器的 Alpha 通道来控制蒙版贴图的不透明度。• **Mask Map Blue**: 使用**蒙版贴图**的蓝色通道控制不透明度。 |
| **Affect Base Color**        | Bool     | 启用此复选框可使此贴花使用 **Base Color** 属性。禁用此属性后，贴花对 Base Color 没有影响。启用或禁用此属性时，HDRP 仍使用基色的 Alpha 通道作为其他属性的不透明度。 |
| **Affect Metal**             | Bool     | 启用此复选框可使贴花使用其 **Mask Map** 的金属属性。否则，贴花没有金属效果。使用 **Mask Map** 的红色通道。 |
| **Affect Ambient Occlusion** | Bool     | 启用此复选框可使贴花使用其 **Mask Map** 的环境光遮蔽属性。禁用此属性时，贴花没有环境光遮蔽效果。此属性使用 **Mask Map** 的绿色通道。 |
| **Affect Smoothness**        | Bool     | 启用此复选框可使贴花使用其 **Mask Map** 的 smoothness 平滑度属性。禁用此属性时，贴花没有平滑效果。此属性使用 **Mask Map** 的 **Alpha** 通道。 |
| **Decal Layer**              | Enum     | 指定 Unity 将贴花投影到哪些材质的图层。Unity 会将贴花显示在使用匹配贴花层的任何网格渲染器或地形上。 |


# 上下文属性

| **输入**             | **类型** | **描述**                                              |
| --------------------- | -------- | ------------------------------------------------------------ |
| **Fade Factor**       | Float    | 更改此值可淡入和淡出贴花。值为 0 时，贴花完全透明，值为 1 时不会更改整体不透明度。 |
| **Angle Fade**        | Vector2  | 使用 min-max 滑块可根据 Decal 向后方向与接收表面的顶点法线之间的角度来控制贴花的淡出范围（以度为单位）。此属性仅在[贴花图层](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/Decal.html)功能已启用。此值限制在 0 到 180 度之间。  |
| **Ambient Occlusion** | Float    | 使用滑块设置贴花的环境光遮蔽效果的强度。仅当在 [HDRP 资源](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/HDRP-Asset.html#Decals) 中启用 **Metal 和 Ambient Occlusion 属性** 复选框时，此属性才有效。 |



# 局限

- 此输出不支持 Shader Graph。
- 要使此上下文正常工作，请在 HDRP 资源和 HDRP Settings 中启用 Decals。
- 在 HDRP 资源和 HDRP Settings 中禁用 **Decal Layers** 时，Unity 不会考虑 **Angle Fade** 值。
- 如果在 HDRP 资源中启用了 **Rendering > Decals > Metal** 和 **Ambient Occlusion Properties**，则 Unity 仅考虑 Metalness 和 Ambient Occlusion。禁用此属性后，Metalness 和 Ambient Occlusion 可见，但它们不会产生任何影响。
