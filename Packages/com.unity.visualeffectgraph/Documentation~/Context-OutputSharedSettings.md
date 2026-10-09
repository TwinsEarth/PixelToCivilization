# 共享输出设置和属性

所有输出共享这些设置和属性端口。唯一的例外是 Shader Graph 设置，大多数输出都支持，但不是全部都支持。

| **Setting**      | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Shader Graph** | 指定[Shader Graph](https://docs.unity.cn/cn/Packages-cn/com.unity.shadergraph@latest) Unity 用于渲染此输出生成的粒子。如果将 Shader Graph 分配给此属性，则 Inspector 会隐藏一些与 Shader Graph 中的设置和属性重复的设置和属性。此外，Inspector 还公开了 Shader Graph 中的所有 Surface Options，允许您在 Inspector 中为上下文编辑 Shader Graph 属性。有关此添加到 Inspector 的 Surface Options 的信息，请参阅您分配的 Shader Graph 类型的文档。例如，如果您分配了 HDRP 光照 Shader Graph，请参阅 [光照 Shader Graph](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/master-stack-lit.html)。 |

## 渲染状态设置

| **设置**                 | **类型** | **描述**                                              |
| --------------------------- | -------- | ------------------------------------------------------------ |
| **Blend Mode**              | Enum     | 指定系统的混合方法并确定如何使用 Alpha。选项包括 ：<br/>&#8226; **Opaque**: 不使用 Alpha 值，并在 Opaque 通道中渲染输出。这也会写入深度缓冲区。<br/>&#8226; **Alpha**: 使用[标准 Alpha 混合模式](https://docs.unity3d.com/Manual/SL-Blend.html)(https://docs.unity3d.com/Manual/SL-Blend.html)，并在透明通道中渲染输出。默认情况下，这将为粒子输出启用 GPU 排序（您可以使用 **Sort** 设置覆盖）。<br/>&#8226; **Premultiplied Alpha**: 使用[预乘 Alpha 混合模式]并在透明通道中渲染输出。默认情况下，这将为粒子输出启用 GPU 排序（您可以使用 **Sort** 设置覆盖）。<br/>&#8226; **Additive**: 将 [additive混合模式](https://docs.unity3d.com/Manual/SL-Blend.html) 用于 Alpha 并在透明通道中渲染输出。 |
| **Use Alpha Clipping**      | Bool     | 指示系统是否使用 alpha 裁剪并丢弃特定 alpha 阈值以下的像素。如果启用此属性，请使用 **Alpha Threshold** 输入设置 alpha 值，系统将丢弃该值以下的像素。 |
| **Sorting Priority**        | Int      | **(检查器)** 偏移 Unity 适用于渲染顺序。此设置对应于 Material [排序优先级](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/Renderer-And-Material-Priority.html) 在 HDRP 和材质中 [排序优先级](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest?subfolder=/manual/simple-lit-shader.html) 在 URP 中。它相当于修改 [Material.renderQueue](https://docs.unity3d.com/ScriptReference/Material-renderQueue.html) 属性。此值可以是负数或正数。HDRP 和 URP 将此值限制在 -50 和 50 之间。 |
| **Generate Motion Vectors** | Bool     | **(检查器)** 指示系统中的粒子是否写入运动矢量，从而指示系统是否生成运动矢量通道。当您希望将时间效果（如时间抗锯齿或运动模糊）考虑在内时，这非常有用。并非所有输出都可以生成运动矢量，在这种情况下，此设置不可见。<br/>如果使用透明输出启用此设置，则会显示 **Alpha Threshold** 输入端口。使用此端口可指定系统要求像素为其写入运动矢量的最小 Alpha。这是因为运动矢量无法混合。 |
| **Exclude From TU And AA** **<sub>[HDRP]</sub>**  | Bool     | **(检查器)** 指示渲染管道是否从时间放大器 （TU） 和时间抗锯齿 （AA） 中排除粒子。当启用 TAA 时粒子看起来模糊时，这非常有用，对于快速移动的粒子尤其有用。并非所有输出都可以排除 TAA，在这种情况下，此设置不可见。 |
| **Cast Shadows**            | Bool     | **(检查器)** 指示系统中的粒子是否投射阴影，从而指示系统是否生成阴影过程。<br/>如果使用透明输出启用此设置，则会显示 **Alpha Threshold** 端口。使用此端口可指定系统要求像素写入阴影通道的最小 Alpha。这是因为阴影不能透明。 |
| **Cull Mode**               | Enum     | **(检查器)** 指定要用于系统的剔除模式。选项包括：<br/>&#8226; **Default**: 使用系统的默认剔除模式。<br/>&#8226; **Front**: 剔除系统中粒子的正面。<br/>&#8226; **Back**: 剔除系统中粒子的背面。<br/>&#8226; **Off**: 不剔除面。此模式使输出同时呈现正面和背面。 |
| **Z Write Mode**            | Enum     | **(检查器)** 指定要用于系统的深度写入模式。选项包括：<br/>&#8226; **Default**: 使用系统的默认深度写入模式。这通常是 **On** 表示不透明，**Off** 表示透明。<br/>&#8226; **On**: 为系统启用深度写入。<br/>&#8226; **Off**: 禁用系统的深度写入。 |
| **Z Test Mode**             | Enum     | **(检查器)** 指定要用于系统的深度测试模式。选项包括：<br/>&#8226; **Default**: 使用系统的默认深度测试模式。这通常是 **On**。<br/>&#8226; **On**: 启用系统的深度测试。<br/>&#8226; **Off**: 禁用系统的深度测试。这意味着无论像素是否被更近的几何体遮挡，都会渲染输出。 |

## 粒子选项设置

| **设置**           | **类型** | **描述**                                              |
| --------------------- | -------- | ------------------------------------------------------------ |
| **Color Mapping**     | Enum     | 指定如何使用主纹理为粒子像素着色。选项包括：<br/>&#8226; **Default**: 直接使用主纹理颜色 （并将其与粒子颜色相乘）。<br/>&#8226; **GradientMapped**: 使用 Texture alpha 从创建的 **Gradient** 端口获取颜色（然后将其与粒子颜色相乘）。<br/>此属性仅显示在支持纹理的输出上下文上。 |
| **UV Mode**           | Enum     | 指定如何将 UV 用于纹理。选项包括：<br/>&#8226; **Default**: 按原样使用 UV 对纹理进行采样。<br/>&#8226; **Flipbook**: 使用子 UV 在 Flipbook 纹理中采样。要指定 Flipbook 纹理的尺寸（每个轴的帧数），请使用 **Flip Book Size** 属性。为了确定 flipbook 的当前帧，此 Context 使用粒子的 **tex index** 属性。在此模式下，Context 不会在 flipbook 帧之间进行插值。<br/>&#8226; **FlipbookBlend**: 与 **Flipbook** 相同，但使用线性插值在帧之间混合。此模式使用 **tex index** 属性的小数部分来计算插值量。<br/>&#8226; **ScaleAndBias**: 在上下文对纹理进行采样之前缩放和偏置 UV。<br/>&#8226; **FlipbookMotionBlend**: 与 **FlipbookBlend** 相同，但使用额外的运动矢量纹理。要指定运动矢量纹理，请使用 Motion **Vector Map** （运动矢量贴图） 输入端口。要缩放运动向量，请使用 **Motion Vector Scale** （运动向量缩放） 输入端口。<br/>此属性仅显示在支持纹理的输出上下文上。 |
| **Use Soft Particle** | Bool     | 指示是否使用软粒子。软粒子可移除粒子和几何体之间的硬交集。要指定粒子淡出的几何体的最大距离，请使用 **Soft Particle Fade Distance**。<br/>仅当 **Blend Mode** 未设置为 **Opaque** 时，才会显示此属性。 |
| **Flipbook Layout**   | Enum     | 指定 Flipbook 的布局。选项包括：<br/>&#8226; **Texture 2D**: 使用具有多个帧的单个纹理。<br/>&#8226; **Texture 2D Array**: 使用具有多个切片的 Texture 数组。<br/>仅当将 **UV Mode** 设置为 **Flipbook**、**FlipbookBlend** 或 **FlipbookMotionBlend** 时，才会显示此属性。 |
| **Sort**              | Enum     | **(检查器)** 指定此 VFX Graph 系统是否使用排序。<br/>当 VFX Graph 对系统的粒子进行排序时，它会使用额外的计算通道，并根据您在 **Sort Mode** 中设置的选项计算它如何对粒子进行排序。<br/>此属性的选项包括：<br/>&#8226; **Auto**: TVFX Graph 确定是否对系统的粒子进行排序。Unity 会自动将非交换混合模式 **Alpha** 和 **Premulitplied Alpha** 的排序设置为 **On**。<br/>&#8226; **On**: 启用粒子排序。<br/>&#8226; **Off**: 禁用粒子排序。使用此模式时，启用 **Indirect Draw**，并使用 **Alpha** 或 **Premultiplied Alpha** 混合模式，这会导致闪烁效果。这是因为每个帧的粒子顺序不是确定性的。<br/><br/>**注:**  VFX Graph 需要排序的粒子越多，排序过程使用的资源就越多。这会使排序成为一项资源密集型操作。<br/>启用粒子排序时，Unity 会自动启用 **Indirect Draw**。 |
| **Sort Mode**         | Enum     | **(检查器)** 指定 Unity 如何对 Output 上下文的粒子进行排序。选项包括：<br/>&#8226; **Distance To Camera** :Unity 在所有其他粒子之前渲染最接近 Camera 的粒子。<br/>&#8226; **Youngest in Front** : Unity 在所有其他粒子之前渲染具有最小 **Age** 属性的粒子。 <br/>&#8226; **Camera Depth** : Unity 在所有其他粒子之前渲染最接近 Camera 平面的粒子。 <br/>&#8226; **Custom** : 此模式在 Output 上下文中创建一个 **Sort Key** 字段。您可以使用它来输入 Unity 图形为每个粒子计算的值。Unity 将具有最大 **Sort Key** 值的粒子渲染在具有较低 **Sort Key** 值的粒子之前。<br/><br/>**注意**: 当 Unity 对粒子进行排序时，它不会考虑您在 Output 上下文中对粒子属性所做的任何更改。此规则的例外情况是使用 **Compute Culling、Frustrum Culling、Generate Motion Vectors、Multi Meshes** 或 **LOD** 的输出上下文。 |
| **Revert Sorting**    | Bool     | **(检查器)** 反转 Unity 从 Output 上下文中绘制粒子的顺序。 |
| **Indirect Draw**     | Bool     | **(检查器)** 指示系统是否只输出活动粒子，而不是在顶点着色器中输出所有粒子并剔除死粒子。启用此设置可在粒子数未达到系统容量或每个粒子的顶点数较高（通常具有网格输出）时提高性能。<br/>其他设置可以隐式启用 **Indirect Draw**。在这种情况下，此设置已启用，但不可见。可以隐式启用 **Indirect Draw** 的设置包括 **Sort、Compute Culling** 和 **Frustum Culling**。 |
| **Compute Culling**   | Bool     | **(检查器)** 指示系统是否剔除在计算通道中而不是在顶点着色器中不活动的粒子。如果在输出中设置了 **alive** 属性，并且每个粒子的顶点数较高，则启用此选项可提高性能。<br/>如果启用 **Frustum Culling**，则也会隐式启用 **Indirect Draw**。在这种情况下，此设置已启用，但不可见。 |
| **Frustum Culling**   | Bool     | **(检查器)** 指示是否在计算通道中剔除摄像机视锥体之外的粒子。启用此选项可以提高大型系统的性能，其中大量粒子可能超出摄像机视锥体。<br/>请注意，视锥体剔除可能会导致阴影投射问题，因为在阴影通道期间，系统不会考虑摄像机视锥体之外的剔除粒子。|

## 上下文属性

| **输入**                       | **类型**  | **描述**                                              |
| ------------------------------- | --------- | ------------------------------------------------------------ |
| **Alpha Threshold**             | float     | 指定用于像素剪辑的 Alpha 阈值。系统仅渲染 Alpha 值高于此阈值的像素。<br/>仅当至少满足以下条件之一时，才会显示此端口：<br/>&#8226; 已启用 **Use Alpha Clipping**：Alpha 剪裁对所有渲染通道（包括主颜色通道）均处于活动状态。<br/>&#8226;  已启用 **Generate Motion Vectors** 并且 **Blend Mode** 不为 **Opaque**：Alpha 裁剪在运动矢量通道中处于活动状态。<br/>&#8226; 已启用 **Cast Shadows** 并且 **Blend Mode** 不是 **Opaque**：Alpha 裁剪在阴影通道中处于活动状态。 |
| **Gradient**                    | Gradient  | 用于从纹理 alpha 采样颜色的渐变。<br/>此属性仅在将 **Color Mapping** 设置为 **GradientMapped** 时显示。 |
| **Flip Book Size**              | float2    | 翻页中沿 x 和 y 维度的帧数。<br/>仅当将 **UV Mode** 设置为指定了翻页的模式时，才会显示此属性。 |
| **Motion Vector Map**           | Texture2D | 用于为给定像素采样 2D 运动矢量的纹理。<br/>此属性仅在将 **UV Mode** 设置为 **FlipbookMotionBlend** 时显示。 |
| **Motion Vector Scale**         | float     | 应用于采样运动矢量的比例。<br/>此属性仅在将 **UV Mode** 设置为 **FlipbookMotionBlend** 时显示。 |
| **UV Scale**                    | float2    | 在采样纹理之前应用于 UV 的比例。<br/>此属性仅在将 **UV Mode** 设置为 **ScaleAndBias** 时显示。 |
| **UV Bias**                     | float2    | 在采样纹理之前应用于 UV 的偏差。<br/>此属性仅在将 **UV Mode** 设置为 **ScaleAndBias** 时显示。 |
| **Soft Particle Fade Distance** | float     | 粒子开始消失的世界空间中的最大距离。<br/>此属性仅在您启用 **Use Soft Particle** 时才显示。 |
| **Sort Key**                    | float     | Unity 用于对粒子进行排序的值。Unity 渲染 **Sort Key** 值最大的粒子。<br/>仅当启用 **Sort Mode** 并将其设置为 **Custom** 时，才会显示此属性。 |
