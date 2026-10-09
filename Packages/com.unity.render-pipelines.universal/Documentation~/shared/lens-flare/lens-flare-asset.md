# Lens Flare (SRP) 数据资源

Unity 的 [Scriptable Render Pipeline (SRP)](https://docs.unity.cn/cn/tuanjiemanual/Manual/ScriptableRenderPipeline.html) 包含 **Lens Flare Data** 资源。您可以使用此资源来控制场景中 [Lens Flares](lens-flare-component.md) 的外观。这是 SRP 版本的内置渲染管线 [Flare](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-Flare.html) 资源，该资源与 SRP 不兼容。

有关如何使用 Lens Flares 的示例，请参阅 [URP Package Samples 中的 Lens Flare 示例](../../package-sample-urp-package-samples.md#lens-flares)。

要创建 Lens Flare Data 资源，请选择 **Assets > Create > Lens Flare (SRP)**。要使用此资源，请将其分配给 [Lens Flare (SRP) 组件](lens-flare-component.md) 的 **Lens Flare Data** 属性。

## Properties

Lens Flare Element 资源具有以下属性：

- [Lens Flare (SRP) 数据资源](#lens-flare-srp-数据资源)
  - [Properties](#properties)
    - [Type](#type)
      - [Image](#image)
      - [Circle](#circle)
      - [Polygon](#polygon)
  - [Color](#color)
  - [Transform](#transform)
  - [AxisTransform](#axistransform)
  - [Distortion](#distortion)
  - [Multiple Elements](#multiple-elements)
    - [Uniform](#uniform)
    - [Curve](#curve)
    - [Random](#random)

### <a name="type"></a>Type

| **属性** | **描述**                                              |
| ------------ | ------------------------------------------------------------ |
| Type         | 选择此资源创建的 Lens Flare 元素类型： <br />&#8226; [Image](#image) <br />&#8226; [Circle](#circle) <br />&#8226; [Polygon](#polygon) |

#### <a name="image"></a>Image

![](../../images/shared/lens-flare/lens-flare-shape-image.png)

| **属性** | **描述**                                              |
| ------------ | ------------------------------------------------------------ |
| Flare Texture | 此 Lens Flare 元素使用的纹理。 |
| Preserve Aspect Ratio | 固定 **Flare Texture** 的宽高比。您可以使用 [Distortion](#Distortion) 来更改此属性。 |

#### <a name="circle"></a>Circle

![](../../images/shared/lens-flare/lens-flare-shape-circle.png)

| **属性** | **描述** |
| ------------ | ------------------------------------------------------------ |
| Gradient | 控制圆形光晕渐变的偏移量。该值范围为 0 到 1。 |
| Falloff | 控制圆形光晕渐变的衰减。值范围为 0 到 1，其中 0 表示没有色调衰减，1 表示衰减均匀分布在整个圆形上。 |
| Inverse | 启用此属性以反转渐变方向。 |

#### <a name="polygon"></a>Polygon

![](../../images/shared/lens-flare/lens-flare-shape-polygon.png)

| **属性** | **描述** |
| ------------ | ------------------------------------------------------------ |
| Gradient | 控制多边形光晕渐变的偏移量。该值范围为 0 到 1。 |
| Falloff | 控制多边形光晕渐变的衰减。值范围为 0 到 1，其中 0 表示没有色调衰减，1 表示衰减均匀分布在整个多边形上。 |
| Side Count | 确定多边形光晕的边数。 |
| Roundness | 定义多边形光晕边缘的平滑度。值范围为 0 到 1，其中 0 是尖锐的多边形，1 是圆形。 |
| Inverse | 启用此属性以反转渐变方向。 |

## <a name="color"></a>Color

![](../../images/shared/lens-flare/lens-flare-Color.png)

| **属性** | **描述** |
| ----------------------- | ------------------------------------------------------------ |
| Tint | 更改 Lens Flare 的色调。如果此资源附加到光源，则此属性基于光源色调。 |
| Modulate By Light Color | 允许光源颜色影响此 Lens Flare 元素。仅当该资源用于附加到点光、聚光或区域光的 [Lens Flare (SRP) 组件](lens-flare-component.md) 时才适用。 |
| Intensity | 控制此元素的强度。 |
| Blend Mode | 选择此资源创建的 Lens Flare 元素的混合模式：<br />• Additive  <br />• Screen  <br />• Premultiplied <br />• Lerp |


## <a name="transform"></a>Transform

![](../../images/shared/lens-flare/lens-flare-Transform.png)

| **属性** | **描述** |
| ----------------------- | ------------------------------------------------------------ |
| Position Offset | 定义 Lens Flare 在屏幕空间中相对于其源的偏移量。 |
| Auto Rotate | 启用此属性以根据屏幕上的角度自动旋转 Lens Flare 纹理。Unity 使用 **Auto Rotate** 角度覆盖 **Rotation** 参数。<br/><br/> 要确保 Lens Flare 可以旋转，请将 [**Starting Position**](#axistransform) 属性设置为大于 0 的值。 |
| Rotation | 旋转 Lens Flare。此值以度为单位。 |
| Size | 调整此 Lens Flare 元素的缩放比例。<br/><br/> 当 [Type](#type) 设置为 [Image](#image) 并启用了 **Preserve Aspect Ratio** 时，此属性不可用。 |
| Scale | 此 Lens Flare 元素在世界空间中的大小。 |

## <a name="axistransform"></a>AxisTransform

![](../../images/shared/lens-flare/lens-flare-axis-transform.png)

| **属性** | **描述** |
| ----------------- | ------------------------------------------------------------ |
| Starting Position | 定义 Lens Flare 相对于其源的起始位置。此值在屏幕空间中操作。 |
| Angular Offset | 控制 Lens Flare 相对于其当前位置的角度偏移。此值以度为单位。 |
| Translation Scale | 限制 Lens Flare 偏移的大小。例如，(1, 0) 使光晕水平扩展，(0, 1) 使光晕垂直扩展。<br/><br/>您还可以使用此属性来控制 Lens Flare 运动的速度。例如，(0.5, 0.5) 使 Lens Flare 元素的运动速度看起来为正常速度的一半。 |


<a name="Distortion"></a>

## Distortion

![](../../images/shared/lens-flare/lens-flare-radial-distortion.png)

| **属性** | **描述** |
| --------------- | ------------------------------------------------------------ |
| Enable | 设置此属性为 True 以启用扭曲。 |
| Radial Edge Size | 控制扭曲效果从屏幕边缘扩展的大小。 |
| Radial Edge Curve | 沿着从屏幕中心到边缘的曲线混合扭曲效果。 |
| Relative To Center | 设置此值为 True 以使扭曲相对于屏幕中心。否则，扭曲相对于 Lens Flare 的屏幕位置。 |


<a name="Multiple-Elements"></a>

## Multiple Elements

| **属性** | **描述** |
| --------------- | ------------------------------------------------------------ |
| Enable | 启用此选项以在场景中允许多个 Lens Flare 元素。 |
| Count | 确定 Unity 生成的相同 Lens Flare 元素的数量。<br/>值为 **1** 时，Lens Flare 元素与单个元素相同。 |
| Distribution | 选择 Unity 生成多个 Lens Flare 元素的方法：<br/>•[Uniform](#uniform)<br/>•[Curve](#Curve)<br/>•[Random](#random) |
| Length Spread | 控制多个 Lens Flare 元素的扩散程度。 |
| Relative To Center | 如果启用，则扭曲相对于屏幕中心，否则相对于 Lens Flare 源的屏幕位置。 |


### <a name="uniform"></a>Uniform
![](../../images/shared/lens-flare/lens-flare-multiple-elements-uniform.png)

| **属性** | **描述** |
| --------------- | ------------------------------------------------------------ |
| Colors | 此资源应用于 Lens Flare 的颜色范围。 |
| Rotation | 以增量方式应用于每个元素的旋转角度（以度为单位）。 |

### <a name="Curve"></a>Curve

![](../../images/shared/lens-flare/lens-flare-multiple-elements-curve.png)

| **属性** | **描述** |
| ---------------- | ------------------------------------------------------------ |
| Colors | 此资源应用于 Lens Flare 的颜色范围。您可以使用 **Position Spacing** 曲线来确定此范围如何影响每个 Lens Flare。 |
| Position Variation | 调整此曲线以更改 Lens Flare 元素在 **Lens Spread** 中的位置。 |
| Rotation | 以均匀角度（以度为单位）应用于沿曲线分布的每个元素。此值范围为 -180° 到 180°。 |
| Scale | 调整此曲线以控制 Lens Flare 元素的尺寸范围。 |

### <a name="random"></a>Random

![](../../images/shared/lens-flare/lens-flare-multiple-elements-random.png)


| **属性** | **描述** |
| ------------------- | ------------------------------------------------------------ |
| Seed | 此资源用于生成随机性的基值。 |
| Intensity Variation | 控制 Lens Flare 元素亮度的变化。较高的值可能会导致某些元素不可见。 |
| Colors | 此资源应用于 Lens Flare 的颜色范围。此属性基于 **Seed** 值。 |
| Position Variation | 控制 Lens Flare 的位置。**X** 值沿与 **Length Spread** 相同的轴扩展。值为 0 表示 Lens Flare 位置没有变化。**Y** 值沿基于 **Seed** 值的垂直屏幕空间轴扩展。 |
| Rotation Variation | 控制 Lens Flare 的旋转变化，基于 **Seed** 值。**Rotation** 和 **Auto Rotate** 参数继承此属性。 |
| Scale Variation | 控制 Lens Flare 的缩放，基于 **Seed** 值。 |
