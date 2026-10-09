# Lens Flare (SRP) 组件

![](../../images/shared/lens-flare/lens-flare-header.png)

Unity 的 Scriptable Render Pipeline (SRP) 包含 **Lens Flare (SRP)** 组件，该组件可在场景中渲染 Lens Flare。这是 SRP 版本的内置渲染管线 [Lens Flare](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-LensFlare.html) 组件，该组件与 SRP 不兼容。您可以将 Lens Flare (SRP) 组件附加到任何 GameObject，但某些属性仅在将组件附加到光源时可见。

![](../../images/shared/lens-flare/lens-flare-comp.png)

## 在 SRP 中创建 Lens Flare

Lens Flare (SRP) 组件控制 Lens Flare 的位置，以及衰减和是否考虑遮挡等属性。SRP 使用 [Lens Flare (SRP) 数据](lens-flare-asset.md) 资源来定义 Lens Flare 的外观。每个 Lens Flare (SRP) 组件必须引用一个 Lens Flare (SRP) 数据资源，以便在屏幕上显示 Lens Flare。

在场景中创建 Lens Flare 的步骤如下：

1. 创建或选择一个 GameObject 以附加 Lens Flare。
2. 在 **Inspector** 中，点击 **Add Component**。
3. 选择 **Rendering** > **Lens Flare (SRP)**。此时 Lens Flare 尚未渲染，因为组件的 **Lens Flare Data** 属性尚未引用 Lens Flare (SRP) 数据资源。
4. 创建一个新的 Lens Flare (SRP) 数据资源（菜单路径：**Assets** > **Create** > **Lens Flare (SRP)**）。
5. 在 Lens Flare (SRP) 组件的 **Inspector** 中，将新创建的 Lens Flare (SRP) 数据资源分配给 **Lens Flare Data** 属性。
6. 选择 Lens Flare (SRP) 数据资源，在 **Inspector** 中的 **Elements** 列表中添加一个新元素。默认情况下，一个白色的 Lens Flare 将出现在 Lens Flare (SRP) 组件的位置。要自定义 Lens Flare 的外观，请参考 [Lens Flare (SRP) 数据](lens-flare-asset.md)。

## 属性

### 常规

| **属性** | **描述** |
| --------------- | ------------------------------------------------------------ |
| Lens Flare Data | 选择此组件控制的 [Lens Flare (SRP) 数据](lens-flare-asset.md) 资源。 |
| Intensity | 乘算 Lens Flare 的强度。 |
| Scale | 乘算 Lens Flare 的缩放比例。 |
| Attenuation by Light Shape | 启用此属性以根据附加此组件的光源类型自动更改 Lens Flare 的外观。<br/>例如，如果此组件附加到聚光灯，并且摄像机从光源后方观察，则 Lens Flare 将不可见。<br/>此属性仅在组件附加到光源时可用。 |
| Attenuation Distance | 衰减距离的起点和终点之间的距离。<br/>此值在世界空间中以 0 到 1 之间的范围操作。 |
| Attenuation Distance Curve | 根据 GameObject 与摄像机之间的距离，淡化 Lens Flare 的外观。 |
| Scale Distance | **Scale Distance Curve** 的起点和终点之间的距离。<br/>此值在世界空间中以 0 到 1 之间的范围操作。 |
| Scale Distance Curve | 根据 GameObject 与摄像机之间的距离，更改 Lens Flare 的大小。 |
| Screen Attenuation Curve | 根据 Lens Flare 距离屏幕边缘的远近减少其效果。您可以使用此属性在屏幕边缘显示 Lens Flare。 |

### 遮挡

| **属性** | **描述** |
| --------------- | ------------------------------------------------------------ |
| Enable | 启用此属性以基于深度缓冲区部分遮挡 Lens Flare。 |
| Occlusion Radius | 定义 Unity 遮挡 Lens Flare 的范围。此值以世界空间为单位。 |
| Sample Count | CPU 用于计算 **Occlusion Radius** 的随机采样数量。 |
| Occlusion Offset | 偏移遮挡操作的平面。较高的值会将此平面向摄像机靠近。此值以世界空间为单位。<br/>例如，如果 Lens Flare 在灯泡内部，您可以使用此属性在灯泡外部进行遮挡采样。 |
| Occlusion Remap Curve | 允许将遮挡值（0 到 1）重新映射为任何期望的曲线形状。 |
| Allow Off Screen | 启用此属性以允许屏幕外的 Lens Flare 影响当前视野范围。 |
