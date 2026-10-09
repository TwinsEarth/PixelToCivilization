# Lit Shader

Lit Shader 可用于渲染石材、木材、玻璃、塑料和金属等真实世界表面，以实现照片级逼真的质量。它可使光照水平和反射效果更具真实感，并能在各种光照条件下正确响应，例如明亮的阳光或黑暗的洞穴。此 Shader 使用通用渲染管线（URP）中计算量最大[着色模型](shading-model.md)。

关于 Lit Shader 的使用示例，请参阅 [URP Package Samples 中的 Shader 示例](package-sample-urp-package-samples.md#shaders)。

## 在编辑器中使用 Lit Shader

要选择并使用此 Shader，请按照以下步骤操作：

1. 在项目中创建或找到要应用此 Shader 的材质（Material）。选择该 __Material__，Material Inspector 窗口将打开。
2. 点击 __Shader__，然后选择 __Universal Render Pipeline__ > __Lit__。

## UI 概览

此 Shader 的 Inspector 窗口包含以下内容：

- __[Surface Options](#surface-options)__
- __[Surface Inputs](#surface-inputs)__
- __[Advanced](#advanced)__

![Inspector for the Lit Shader](Images/Inspectors/Shaders/Lit.png)



### Surface Options

__Surface Options__ 控制 URP 如何在屏幕上渲染材质。

| 属性               | 描述                                                         |
| ---------------- | ------------------------------------------------------------ |
| __Workflow Mode__ | 使用此下拉菜单选择适用于材质贴图的工作流：[__Metallic__](https://docs.unity.cn/cn/tuanjiemanual/Manual/StandardShaderMaterialParameterMetallic.html) 或 [__Specular__](https://docs.unity.cn/cn/tuanjiemanual/Manual/StandardShaderMaterialParameterSpecular.html)。<br/>选择后，Inspector 其余部分的主要贴图选项将遵循所选的工作流。有关金属度（Metallic）或高光（Specular）工作流的详细信息，请参阅 [Unity 内置 Standard Shader 文档](https://docs.unity.cn/cn/tuanjiemanual/Manual/StandardShaderMetallicVsSpecular.html)。 |
| __Surface Type__  | 使用此下拉菜单选择材质的表面类型：__Opaque__ 或 __Transparent__。这决定了 URP 使用哪个渲染通道。<br/>__Opaque__ 材质始终完全可见，无论其后是否有其他对象，并且 URP 会优先渲染不透明材质。<br/>__Transparent__ 材质受背景影响，并根据所选的透明表面类型变化。URP 在不透明对象之后的独立通道中渲染透明材质。选择 __Transparent__ 后，将出现 __Blending Mode__ 选项。 |
| __Blending Mode__ | 选择 Unity 在混合材质和背景时如何计算每个像素的颜色。<br/><br/>在混合模式的上下文中，Source 指的是设置了混合模式的透明材质，而 Destination 指的是该材质所覆盖的任何背景对象。 |
|&#160;&#160;&#160;&#160;Alpha | ![Alpha 混合模式示例](./Images/blend-modes/blend-mode-alpha.png)<br/>*Alpha 混合模式。*<br/><br/>__Alpha__ 使用材质的 Alpha 值来调整对象的透明度。0 表示完全透明，255 表示完全不透明（在混合计算中转换为 1）。<br/>无论 Alpha 值如何，该材质始终在透明渲染通道中渲染。<br/>此模式允许使用 [Preserve Specular Lighting](#preserve-specular) 选项。<br/><br/>Alpha 计算公式：<br/>*OutputRGBA* = (*SourceRGB* &#215; *SourceAlpha*) + *DestinationRGB* &#215; (1 &#8722; *SourceAlpha*) |
|&#160;&#160;&#160;&#160;Premultiply | ![Premultiply 混合模式示例](./Images/blend-modes/blend-mode-premultiply.png)<br/>*Premultiply 混合模式。*<br/><br/>__Premultiply__ 先将透明材质的 RGB 值与其 Alpha 值相乘，然后应用类似于 Alpha 模式的效果。<br/>此模式的计算公式允许透明材质中 Alpha 值为 0 的区域具有加法混合效果，可减少在不透明和透明像素交界处出现的伪影。<br/><br/>Premultiply 计算公式：<br/>*OutputRGBA* = *SourceRGB* + *DestinationRGB* &#215; (1 &#8722; *SourceAlpha*) |
|&#160;&#160;&#160;&#160;Additive | ![Additive 混合模式示例](./Images/blend-modes/blend-mode-additive.png)<br/>*Additive 混合模式。*<br/><br/>__Additive__ 通过相加材质的颜色值和背景颜色值来创建混合效果。Alpha 值决定源材质颜色的强度，然后进行混合计算。<br/>此模式允许使用 [Preserve Specular Lighting](#preserve-specular) 选项。<br/><br/>Additive 计算公式：<br/>*OutputRGBA* = (*SourceRGB* &#215; *SourceAlpha*) + *DestinationRGB* |
|&#160;&#160;&#160;&#160;Multiply | ![Multiply 混合模式示例](./Images/blend-modes/blend-mode-multiply.png)<br/>*Multiply 混合模式。*<br/><br/>__Multiply__ 通过将材质的颜色与其背后的颜色相乘来创建混合效果。这种模式类似于透过彩色玻璃观察物体时的效果，使颜色变暗。<br/>此模式使用材质的 Alpha 值来调整颜色混合程度。Alpha 值为 1 时，颜色直接相乘；较低的 Alpha 值则使颜色向白色过渡。<br/><br/>Multiply 计算公式：<br/>*OutputRGBA* = *SourceRGB* &#215; *DestinationRGB* <a name="preserve-specular"></a> |
| __Preserve Specular Lighting__ | 指定 Unity 是否保留 GameObject 上的高光反射。即使表面为透明状态，该选项仍会使反射光可见。<br/><br/>此属性仅在 __Surface Type__ 设置为 Transparent 且 __Blending Mode__ 设置为 Alpha 或 Additive 时可见。<br/><br/>![关闭 Preserve Specular Lighting](./Images/blend-modes/preserve-specular-lighting-off.png)<br/>*关闭 __Preserve Specular Lighting__ 的材质效果。*<br/><br/>![开启 Preserve Specular Lighting](./Images/blend-modes/preserve-specular-lighting-on.png)<br/>*开启 __Preserve Specular Lighting__ 的材质效果。* |
| __Render Face__     | 使用此下拉菜单选择几何体的渲染面。<br/>__Front Face__ 渲染几何体的正面，并[剔除](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-CullAndDepth.html)背面（默认设置）。<br/>__Back Face__ 渲染几何体的背面，并剔除正面。<br/>__Both__ 使 URP 渲染几何体的正反两面，适用于叶子等小型、扁平对象，使其两侧均可见。 |
| __Alpha Clipping__  | 使材质表现为[Cutout](https://docs.unity.cn/cn/tuanjiemanual/Manual/StandardShaderMaterialParameterRenderingMode.html)（剪切）Shader，以创建透明区域与不透明区域之间具有硬边界的效果。例如，可用于创建草叶的效果。<br/>启用后，URP 将不渲染低于指定 __Threshold__（阈值）的 Alpha 值。__Threshold__ 通过滑块调整，范围为 0 到 1。所有高于阈值的区域完全不透明，低于阈值的区域完全透明。例如，设置阈值为 0.1，则 URP 不会渲染 Alpha 值低于 0.1 的部分。默认阈值为 0.5。 |
| __Receive Shadows__ | 选中此选项可使 GameObject 接收其他对象投射的阴影。取消选中此选项，则 GameObject 不会显示阴影。 |


### Surface Inputs

__Surface Inputs__ 描述材质表面的特性。例如，可使用这些属性使表面呈现湿润、干燥、粗糙或光滑的效果。

**注意：** 如果你熟悉 Unity 内置渲染管线中的 [Standard Shader](https://docs.unity.cn/cn/tuanjiemanual/Manual/Shader-StandardShader.html)，这些选项类似于 [材质编辑器](https://docs.unity.cn/cn/tuanjiemanual/Manual/StandardShaderContextAndContent.html)中的主贴图设置。

| 属性                    | 描述                                                         |
| ---------------------- | ------------------------------------------------------------ |
| __Base Map__           | 赋予表面颜色，也称为漫反射贴图。<br/>点击旁边的对象选择器可为 __Base Map__ 选择纹理，这将打开资源浏览器，以选择项目中的贴图。<br/>也可以使用 [颜色拾取器](https://docs.unity.cn/cn/tuanjiemanual/Manual/EditingValueProperties.html) 来调整颜色，颜色值会叠加在所选贴图之上。<br/>如果在 __Surface Options__ 中选择了 __Transparent__ 或 __Alpha Clipping__，材质将使用贴图的 Alpha 通道或颜色。 |
| __Metallic / Specular Map__ | 根据 __Surface Options__ 中选择的 __Workflow Mode__（Metallic 或 Specular）提供相应的贴图输入。<br/>- 在 [__Metallic Map__](https://docs.unity.cn/cn/tuanjiemanual/Manual/StandardShaderMaterialParameterMetallic.html) 工作流下，金属度由 __Base Map__ 颜色控制，并可通过滑块调整金属度（0 表示完全非金属，如塑料或木材，1 表示完全金属，如铜或银）。<br/>- 在 [__Specular Map__](https://docs.unity.cn/cn/tuanjiemanual/Manual/StandardShaderMaterialParameterSpecular.html) 工作流下，可点击对象选择器为其分配贴图，或者使用 [颜色拾取器](https://docs.unity.cn/cn/tuanjiemanual/Manual/EditingValueProperties.html) 来调整颜色。<br/>- 在两种工作流中，__Smoothness__（平滑度）滑块可控制表面高光的扩散程度。值为 0 时高光较粗糙，值为 1 时高光较锐利，如玻璃。中间值可用于创建半光泽效果，如塑料表面（0.5）。<br/>- __Source__ 下拉菜单用于选择 Shader 采样平滑度贴图的通道，可选项：__Metallic Alpha__（Metallic 贴图的 Alpha 通道）或 __Albedo Alpha__（Base Map 的 Alpha 通道）。默认值为 __Metallic Alpha__。 |
| __Normal Map__         | 为表面添加法线贴图，以模拟表面细节，如凹凸、划痕和沟槽。<br/>点击对象选择器可分配贴图，该贴图会影响环境光照的交互方式。<br/>旁边的浮点值为法线贴图的强度调整系数，较低的值降低法线贴图的影响，较高的值增强凹凸效果。 |
| __Height Map__         | URP 使用视差映射（Parallax Mapping）技术，通过[高度贴图](https://docs.unity.cn/cn/tuanjiemanual/Manual/StandardShaderMaterialParameterHeightMap.html)实现表面遮挡效果，使纹理表面具有更真实的层次感。<br/>点击对象选择器可分配高度贴图。<br/>旁边的浮点值用于调整高度贴图的影响，较低的值减少效果，较高的值增强遮挡感。 |
| __Occlusion Map__      | 选择[环境光遮蔽（AO）贴图](https://docs.unity.cn/cn/tuanjiemanual/Manual/StandardShaderMaterialParameterOcclusionMap.html)，用于模拟环境光和反射造成的阴影，使光照更加真实。AO 贴图可减少光线照射到物体边缘和缝隙的程度。点击对象选择器可分配 AO 贴图。 |
| __Emission__           | 使材质表面具有自发光效果。启用后，可配置 __Emission Map__（发光贴图）和 __Emission Color__（发光颜色）。<br/>点击对象选择器可选择发光贴图。<br/>__Emission Color__ 可通过[颜色拾取器](https://docs.unity.cn/cn/tuanjiemanual/Manual/EditingValueProperties.html)进行调整，颜色值可以超过 100% 的白色，以用于发光强度较高的效果，如熔岩。<br/>如果未分配 __Emission Map__，则仅使用 __Emission Color__ 的颜色作为发光颜色。<br/>如果未启用 __Emission__，URP 会将发光颜色设为黑色，并不计算发光效果。 |
| __Tiling__             | 2D 纹理缩放值，控制纹理在 U 和 V 轴上的重复次数。适用于地板、墙壁等表面。默认值为 1，表示无缩放。<br/>增加该值可使纹理在网格上重复出现，减少该值可拉伸纹理。建议尝试不同的数值以达到最佳视觉效果。 |
| __Offset__             | 2D 纹理偏移量，控制纹理在网格上的 U 和 V 轴方向上的偏移位置。调整此值可改变贴图在网格上的位置。 |

### Detail Inputs

__Detail Inputs__ 设置用于向表面添加额外的细节。

__要求：__ 需要支持 Shader Model 2.5 或更高版本的 GPU。

| 属性                    | 描述                                                         |
| ---------------------- | ------------------------------------------------------------ |
| __Mask__              | 选择一张遮罩贴图，用于定义 Unity 在 Surface Inputs 贴图上叠加 Detail 贴图的区域。<br/>遮罩贴图使用 Alpha 通道来控制叠加区域。<br/>__Tiling__ 和 __Offset__ 设置对遮罩贴图无效。 |
| __Base Map__          | 选择包含表面细节的纹理贴图。Unity 以叠加模式（Overlay）将此贴图与 Surface Base Map 进行混合。 |
| __Normal Map__        | 选择包含法线数据的贴图，使用[法线贴图](https://docs.unity.cn/cn/tuanjiemanual/Manual/StandardShaderMaterialParameterNormalMap.html?) 来增加表面细节，例如凹凸、划痕和沟槽。<br/>旁边的滑块可调整法线贴图的强度，默认值为 1。 |
| __Tiling__            | 调整 __Base Map__ 和 __Normal Map__ 在网格上的 U 和 V 轴缩放，使贴图更适配网格表面。<br/>默认值为 1，表示无缩放。<br/>选择大于 1 的值可使贴图在网格上重复，选择小于 1 的值可拉伸贴图。 |
| __Offset__            | 调整 __Base Map__ 和 __Normal Map__ 在 U 和 V 轴上的偏移量，以改变贴图在网格上的位置。 |

### Advanced

__Advanced__ 设置影响渲染的底层计算方式，但不会直接影响表面的可见效果。

| 属性                     | 描述                                                         |
| ---------------------- | ------------------------------------------------------------ |
| __Specular Highlights__ | 启用此选项可使材质从直接光源（如 [定向光、点光源和聚光灯](https://docs.unity.cn/cn/tuanjiemanual/Manual/Lighting.html)）中产生高光反射。<br/>这意味着材质能够反射这些光源的高光效果。<br/>禁用此选项可以跳过高光计算，从而提高 Shader 的渲染速度。<br/>默认情况下，此功能是启用的。 |
| __Environment Reflections__ | 允许材质采样最近的[反射探针](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-ReflectionProbe.html)，或在 [Lighting](https://docs.unity.cn/cn/tuanjiemanual/Manual/GlobalIllumination.html) 窗口中设置的[光照探针](https://docs.unity.cn/cn/tuanjiemanual/Manual/LightProbes.html)。<br/>禁用此选项可减少 Shader 计算量，但会使表面失去环境反射效果。 |
| __Enable GPU Instancing__ | 允许 URP 在可能的情况下，将具有相同几何体和材质的网格合并到一个批次中进行渲染，以提高渲染效率。<br/>如果网格使用了不同的材质，或硬件不支持 GPU 实例化（Instancing），URP 无法进行批量渲染。 |
| __Sorting Priority__ | 使用滑块调整材质的渲染顺序。URP 会优先渲染较低数值的材质。<br/>可以利用此设置减少设备上的过度绘制（Overdraw），使渲染管线优先渲染前景材质，以避免重复渲染被遮挡的区域。<br/>此功能类似于 Unity 内置渲染管线中的 [Render Queue](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/Material-renderQueue.html)。 |

## Channel Packing

此 Shader 使用[通道打包](http://wiki.polycount.com/wiki/ChannelPacking)技术，使金属度（Metallic）、平滑度（Smoothness）和遮蔽（Occlusion）属性可以合并到单个 RGBA 贴图中，从而减少内存占用。

使用通道打包时，仅需加载一张纹理贴图，而不是单独加载三张不同的贴图。例如，在 Substance 或 Photoshop 等软件中，可以按照以下方式进行贴图通道分配：

| 通道     | 属性       |
| -------- | ---------- |
| **Red**  | Metallic   |
| **Green** | Occlusion  |
| **Blue**  | None       |
| **Alpha** | Smoothness |
