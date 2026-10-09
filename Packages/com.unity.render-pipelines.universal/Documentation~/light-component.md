# Light component reference

光照决定了物体的阴影和它所投射的阴影。

本页面包含有关 **Universal Render Pipeline (URP)** 中光照组件的信息。有关 Unity 中光照的一般介绍以及常见光照工作流的示例，请参阅 [Unity 手册中的光照部分](https://docs.unity.cn/cn/tuanjiemanual/Manual/LightingOverview.html)。

## 属性

Light 检查器包含以下几组属性：

- [Light component reference](#light-component-reference)
  - [属性](#属性)
    - [General](#general)
    - [Shape](#shape)
    - [Emission](#emission)
  - [Rendering](#rendering)
  - [Shadows](#shadows)
  - [Preset](#preset)

### <a name="General"></a>General

| Property:| Function: |
|:---|:---|
| **Type**| 当前光源的类型。可能的值为 **Directional**、**Point**、**Spot** 和 **Area**。|
| **Mode**| 指定用于确定光源是否以及如何“烘焙”的 [Light Mode](https://docs.unity.cn/cn/tuanjiemanual/Manual/LightModes.html)。<br/><br/>选项：<ul><li>**Realtime**</li><li>**Mixed**</li><li>**Baked**</li></ul><br/>**注意**：如果 **Type** 设置为 **Area**，则此属性会自动设置为 **Baked**。|

### <a name="Shape"></a>Shape

| Property:| Function: |
|:---|:---|
| **Inner/Outer Spot Angle**| Spot 光源圆锥的内外角度（以度为单位）。<br/><br/>此属性仅在 **Type** 设置为 **Spot** 时可用。|
| **Shape** | 面积光源的形状。<br/><br/>可用选项：<ul><li>**Rectangle**</li><li>**Disc**</li></ul><br/>此属性仅在 **Type** 设置为 **Area** 时可用。|
| &#160;&#160;&#160;&#160;**Width** | 面积光源的宽度。<br/><br/>**注意**：仅当 **Shape** 设置为 **Rectangle** 时此属性可用。|
| &#160;&#160;&#160;&#160;**Height** | 面积光源的高度。<br/><br/>**注意**：仅当 **Shape** 设置为 **Rectangle** 时此属性可用。|
| &#160;&#160;&#160;&#160;**Radius** | 面积光源的半径。<br/><br/>**注意**：仅当 **Shape** 设置为 **Disc** 时此属性可用。|


### <a name="Emission"></a>Emission

| Property:| Function: |
|:---|:---|
| **Light Appearance** | 选择用于创建光源颜色的方法。<br/><br/>可用选项：<ul><li>**Color**</li><li>**Filter and Temperature**</li></ul> |
| &#160;&#160;&#160;&#160;**Color**| 光源发出的颜色。使用颜色滑块设置此属性。<br/><br/>**注意**：此属性仅在 **Light Apperance** 设置为 **Color** 时可用。 |
| &#160;&#160;&#160;&#160;**Filter**| 光源的颜色滤镜。使用颜色滑块设置此属性。<br/><br/>**注意**：此属性仅在 **Light Apperance** 设置为 **Filter and Temperature** 时可用。 |
| &#160;&#160;&#160;&#160;**Temperature**| 光源的温度（以开尔文为单位）。使用滑块设置此属性或输入特定值。<br/><br/>**注意**：此属性仅在 **Light Apperance** 设置为 **Filter and Temperature** 时可用。 |
| **Intensity**| 设置光源的亮度。默认值：**Directional** 光源为 0.5，**Point**、**Spot** 或 **Area** 光源为 1。|
| **Indirect Multiplier**| 使用此值调整间接光的强度。间接光是指从一个物体反射到另一个物体的光。**Indirect Multiplier** 定义了由全局光照（GI）系统计算出的反射光的亮度。如果将 **Indirect Multiplier** 设置为低于 1 的值，反射光会随着每次反射而变暗。设置高于 1 的值则会使光在每次反射时变得更亮。此功能非常适合在阴影中的黑暗表面（例如洞穴内部）需要更亮以便查看细节时使用。|
| **Range**| 定义从物体中心发出的光传播的距离（仅对 **Point** 和 **Spot** 光源有效）。|
| **Cookie** | 光源在场景中投射的 RGB 纹理。使用 Cookie 创建轮廓或有图案的光照。不同类型的光源使用不同的纹理格式：<br/> &#8226; **Directional**: 2D 纹理<br/> &#8226; **Spot**: 2D 纹理<br/> &#8226; **Point**: [立方体贴图](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-Cubemap.html)<br/><br/>**注意**：URP 不支持 **Area** 光源的 Cookie。<br/><br/>有关光源 Cookie 的更多信息，请参阅 [Cookies](https://docs.unity.cn/cn/tuanjiemanual/Manual/Cookies.html)。|
| &nbsp;&nbsp;**Cookie Size** | Unity 应用于 Cookie 纹理的每轴缩放。使用此属性设置 Cookie 的大小。<br/><br/>**注意**：仅当 **Type** 设置为 **Directional** 并且为 **Cookie** 分配了纹理时，此属性可用。|
| &nbsp;&nbsp;**Cookie Offset** | Unity 应用于 Cookie 纹理的每轴偏移。使用此属性可以在不移动光源本身的情况下移动 Cookie。您还可以通过动画此属性来滚动 Cookie。<br/><br/>**注意**：仅当 **Type** 设置为 **Directional** 并且为 **Cookie** 分配了纹理时，此属性可用。|

## <a name="Rendering"></a>Rendering

| Property:| Function: |
|:---|:---|
| **Culling Mask**| 用于选择性地排除不受光照影响的对象组。有关更多信息，请参阅 [Layers](https://docs.unity.cn/cn/tuanjiemanual/Manual/Layers.html)。|

## <a name="Shadows"></a>Shadows

| Property:| Function: |
|:---|:---|
| **Shadow Type**| 确定此光源是否投射硬阴影、软阴影或不投射阴影。有关硬阴影和软阴影的详细信息，请参阅 [Lights](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-Light.html) 文档。 |
|&nbsp;&nbsp;&nbsp;&nbsp;**Baked Shadow Angle** | 如果 **Type** 设置为 **Directional** 且 **Shadow Type** 设置为 **Soft Shadows**，此属性会在阴影的边缘添加一些人工软化，使其看起来更自然。<br/><br/>**注意**：仅当 **Mode** 设置为 **Mixed** 或 **Baked** 时，此属性可用。 |
|&nbsp;&nbsp;&nbsp;&nbsp;**Baked Shadow Radius** | 如果 **Type** 设置为 **Point** 或 **Spot** 且 **Shadow Type** 设置为 **Soft Shadows**，此属性会在阴影的边缘添加一些人工软化，使其看起来更自然。<br/><br/>**注意**：仅当 **Mode** 设置为 **Mixed** 或 **Baked** 时，此属性可用。 |
|&nbsp;&nbsp;&nbsp;&nbsp;**Realtime Shadows**| 当 **Shadow Type** 设置为 **Hard Shadows** 或 **Soft Shadows** 时，这些属性可用。使用这些属性控制实时阴影渲染设置。 |
|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;**Strength**| 使用滑块控制光源投射的阴影有多暗，值的范围为 0 到 1，默认值为 1。 |
|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;**Bias**| 控制是否使用 URP Asset 中的阴影偏差设置，或者为此光源定义自定义阴影偏差设置。可选值为 **Use Pipeline Settings** 或 **Custom**。 |
|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;**Depth**| 控制阴影从光源推离的距离。对于避免虚假的自阴影伪影非常有用。仅当 **Bias** 设置为 **Custom** 时，此属性可见。 |
|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;**Normal**| 控制阴影投射表面沿表面法线的缩放距离。对于避免虚假的自阴影伪影非常有用。仅当 **Bias** 设置为 **Custom** 时，此属性可见。 |
|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;**Near Plane**| 使用滑块控制阴影渲染时近裁剪平面的值，范围为 0.1 到 10。该值会被限制为 0.1 单位或光源 **Range** 属性的 1%，以较小者为准。默认值为 0.2。 |
|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;**Soft Shadows Quality** | 选择软阴影的质量。在选择 **Use Pipeline Settings** 选项时，Unity 会使用 URP Asset 中的值。选项 **Low**、**Medium** 和 **High** 让您为此光源指定软阴影质量值。有关详细信息，请参阅 [Soft Shadows](universalrp-asset.md#soft-shadows) 部分。 |

## Preset

当使用光源组件的预设时，只有部分属性被支持，未支持的属性会被隐藏。
