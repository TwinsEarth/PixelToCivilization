# Output Distortion

菜单路径 : **Context > Output [Strip/Particle] Distortion [Quad/Mesh]**

*(Output Strip Distortion Quad, Output Particle Distortion Quad, Output Particle Distortion Mesh)*

**Distortion** 输出是特定于高清渲染管线 (HDRP) 的上下文节点。它们利用 HDRP 的 Distortion 通道来模拟光线的弯曲。Distortion 输出可用于粒子和粒子条数据类型，粒子失真上下文支持四边形和网格失真。Distortion 输出可用于模拟火灾热变形等效果。

![](F:/Graphics/com.unity.visualeffectgraph/Documentation~/Images/Context-OutputDistortion.png)

以下是特定于 Distortion 上下文的设置和属性列表。有关此上下文与所有其他上下文共享的通用输出设置的信息，请参阅[全局输出设置和属性](Context-OutputSharedSettings.md)。

## 上下文设置

| **设置**         | **类型** | **描述**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| **Distortion Mode** | 指定如何处理失真。选项有：<br/>&#8226; **Screen Space**: 将失真处理为屏幕空间效果。<br/>&#8226; **Normal Based**: 在切线空间中使用法线贴图来计算失真。此法线贴图表示失真四边形或网格的表面方向。 |

## 上下文属性

| **输入**               | **类型** | **描述**                                              |
| ----------------------- | -------- | ------------------------------------------------------------ |
| **Scale By Distance**   | Bool     | 	指示此上下文是否根据与摄像机的距离缩放失真效果以保持一致的视觉外观。 |
| **Distortion Blur Map** | Texture  | 	要用于失真的贴图。到失真的 X 和 Y 偏移的 **R** 和 **G** 通道（以 0.5 为中心）贴图，**B** 通道是失真模糊的遮罩。<br/>请注意，要使此纹理正常工作，您必须禁用纹理 Import Settings 中的 **sRGB**。<br/>此属性仅在将 **Distortion Mode** 设置为 **Screen Space** 时显示。 |
| **Normal Map**          | Texture  | 用于失真的法线贴图。<br/>此属性仅在将 **Distortion Mode** 设置为 **Normal Based** 时显示。 |
| **Smoothness Map**      | Texture  | 控制失真模糊的纹理。遮罩使用此纹理的 alpha 通道。<br/>此属性仅在将 **Distortion Mode** 设置为 **Normal Based** 时显示。 |
| **Alpha Mask**          | Texture  | 缩放失真矢量的纹理。遮罩使用纹理的 Alpha 通道。此属性仅在将 **Distortion Mode** 设置为 **Normal Based** 时显示。 |
| **Distortion Scale**    | Vector2  | 	用于屏幕空间失真的每个轴的比例。x 轴对应于水平比例，y 轴对应于垂直比例。<br/>此属性仅在将 **Distortion Mode** 设置为 **Screen Space** 时显示。 |
| **Distortion Scale**    | Float    | 用于基于法线的失真的比例。使用 **Screen Space** 模式时为屏幕空间；使用 **Normal Based** 模式时为世界空间。<br/>此属性仅在将 **Distortion Mode** 设置为 **Normal Based** 时显示。 |
| **Blur Scale**          | Float    | 	模糊的比例。                                      |
