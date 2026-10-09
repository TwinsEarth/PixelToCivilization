# Set Position (Depth)

菜单路径：**Position > Set Position (Depth)**

**Set Position (Depth)** 代码块根据摄像机及其深度缓冲区计算位置，然后将位置存储到 [position 属性](Reference-Attributes.md)。

为了计算位置，此代码块首先基于不同的模式（**Random**、**Sequential** 或 **Custom** 提供的值）确定屏幕空间中的位置，然后使用深度缓冲区和摄像机的属性将位置转换为世界空间。

您可以使用场景中的第一个 [主摄像机](https://docs.unity3d.com/ScriptReference/Camera-main.html)，也可以提供自己的摄像机。

要提供您自己的摄像机，请在 [Blackboard](Blackboard.md) 中新建一个 Camera 属性并公开该属性。然后使用一个 [HDRP Camera Property Binder](PropertyBinders.md)。

（可选）此代码块可以使用计算出的屏幕空间位置对最后一个颜色缓冲区中的像素颜色进行采样，并将结果传输到颜色属性。最后一个颜色缓冲区是后期处理通道之前的颜色缓冲区，在渲染期间可用。根据摄像机的 [帧设置](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Frame-Settings.html) ，可能得到预折射或失真效果。

剔除选项允许您根据所选模式将活动属性设置为 true 或 false。您可以使用它来杀死远平面上或特定深度范围之外的粒子。

**重要提示：**要使此代码块计算位置，必须启用指定的摄像机并且游戏视图必须可见。

![](Images/Block-SetPosition(Depth)Main.gif)

## 代码块兼容性

此代码块兼容于以下上下文：

+   [Initialize](Context-Initialize.md)

## 代码块设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Camera** | Enum | 决定使用哪个摄像机来计算深度碰撞的方法。选项有：  <br/>• **Main**：使用场景中的主摄像机。这要求场景中的摄像机拥有 **MainCamera** 标签。  <br/>• **Custom**：允许您指定要使用的特定摄像机。 |
| **Mode** | Enum | 此代码块用于在屏幕上分布采样点的方法。选项：  <br/>• **Random**：计算屏幕上的随机位置并根据摄像机的属性确定世界空间位置。  <br/>• **Sequential**：计算屏幕上的顺序位置（先行后列）并根据摄像机的属性确定世界空间位置。  <br/>• **Custom**：确定您指定的屏幕空间坐标的世界空间位置。 |
| **Cull Mode** | Enum | 此代码块用于剔除位置的方法。选项：  <br/>• **None**：不剔除任何位置。  <br/>• **Far Plane**：剔除位于相机远平面上的位置。  <br/>• **Range**：剔除指定范围之外的位置。 |
| **Inherit Scene Color** | Bool | **（检查器）**指定颜色属性是否继承计算位置处的场景颜色。 |
| **Composition Position** | Enum | **（检查器）**指定此代码块如何合成位置属性。选项有：  <br/>• **Set**：用新值覆盖位置属性。  <br/>• **Add**：将新值添加到位置属性值。  <br/>• **Multiply**：将位置属性值乘以新值。  <br/>• **Blend**：在位置属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |
| **Composition Color** | Enum | **（检查器）**指定此代码块如何合成颜色属性。选项： <br/>• **Set**：用新值覆盖颜色属性。  <br/>• **Add**：将新值添加到颜色属性值。  <br/>• **Multiply**：将颜色属性值乘以新值。 <br/>• **Blend**：在颜色属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Camera** | Camera | 用于计算位置的摄像机。此代码块使用来自此摄像机的深度缓冲区来计算粒子位置。  <br/>此属性仅在将 **Camera** 设置为 **Custom** 时显示。 |
| **Z Multiplier** | float | 应用于粒子的乘数，使其看起来更加靠近或远离它们的深度。 |
| **UV Spawn** | Vector2 | 标准化屏幕空间位置（0..1 范围）以对深度采样并计算位置。  <br/>此属性仅在将 **Mode** 设置为 **Custom** 时显示。 |
| **Depth Range** | Vector2 | 指定生成粒子的深度范围。位置在此深度范围之外的粒子将其 **alive** 属性设置为 false。  <br/>此属性仅在将 **Cull Mode** 设置为 **Range** 时显示。 |
| **Blend Position** | float | 当前位置属性值与新计算的位置值之间的混合百分比。  <br/>此属性仅在将 **Composition Position** 设置为 **Blend** 时显示。 |
| **Blend Color** | float | 当前方向属性值与新计算的方向值之间的混合百分比。 <br/>此属性仅在将 **Composition Direction** 设置为 **Blend** 时显示。 |

## Details

**兼容性：**此代码块目前仅与 [高清渲染管线](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/index.html) 兼容，可使用任何图形 API（金属除外）。
