<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>实验性:</b> 此功能目前处于试验阶段，在以后的主要版本中可能会发生更改。</div>

# Set Position (Mesh)

菜单路径 : **Position > Set Position (Mesh)**

菜单路径 : **Position > Set Position (Skinned Mesh)**

**Set Position (Mesh)** 代码块根据网格顶点数据计算位置，并根据合成将结果存储在 [position 属性](Reference-Attributes.md),中。

此块还根据网格法线计算方向向量，并根据合成将其存储到 [direction 属性](Reference-Attributes.md)。

注: [Direction 和 Speed 的 Velocity](Block-VelocityFromDirection&Speed(NewDirection).md) 可以处理方向属性。


## 块兼容性

此 Block 与以下上下文兼容：

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)
- 任何输出上下文

## 块设置

| **设置**               | **类型** | **描述**                                              |
| ------------------------- | -------- | ------------------------------------------------------------ |
| **Spawn Mode**            | Enum     | 指定如何在网格内分布粒子。选项包括:<br/>&#8226; **Random**: 计算 **Placement Mode** 中所选基元之间的每粒子随机进度均匀采样。<br/>&#8226; **Custom**: 允许您手动指定采样参数。 |
| **Placement mode**        | Enum     | 指定要从中采样的网格的基本体部分:<br/>&#8226; **Vertex**: 从所有列出的顶点中采样位置。<br/>&#8226; **Edge**: 来自网格上三角形的两个连续顶点之间的插值的样本。 <br/>&#8226; **Surface**: 来自定义网格上三角形的三个顶点之间的插值的样本。 |
| **Surface coordinates**   | Enum     | 指定此 Block 用于对三角形表面进行采样的方法。<br/>&#8226; **Barycentric**: 使用原始重心坐标对曲面进行采样。使用此方法，采样位置不受三角形边缘的约束，如果您在 Visual Effect Graph 之外烘焙了位置，这将非常有用。<br/>&#8226; **Uniform**: 在三角形区域内均匀地对曲面进行采样。<br/>仅当将 **Placement mode** 设置为 **Surface** 并将 **Spawn Mode** 设置为 **Custom** 时，才会显示此属性。 |
| **Source**                | Enum     | **(检查器)** 指定要从中采样的几何体类型。选项包括:<br/>&#8226; **Mesh**: 网格资产中的样本。<br/>&#8226; **Skinned Mesh Renderer**: 来自 [蒙皮网格渲染器](https://docs.unity3d.com/Manual/class-SkinnedMeshRenderer.html)的示例。 |
| **Composition Position**  | Enum     | **(检查器)** 指定此 Block 如何组合 position 属性。选项包括：:<br/>&#8226; **Set**: 使用新值覆盖 position 属性。<br/>&#8226; **Add**: 将新值添加到 position 属性值。<br/>&#8226; **Multiply**: 将 position 属性值乘以新值。<br/>&#8226; **Blend**: 在 position 属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |
| **Composition Direction** | Enum     | **(检查器)** 指定此 Block 如何组成 direction 属性。选项包括：<br/>&#8226; **Set**: 使用新值覆盖 position 属性。<br/>&#8226; **Add**: 将新值添加到 position 属性值。<br/>&#8226; **Multiply**: 将 position 属性值乘以新值。<br/>&#8226; **Blend**: 在 position 属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |

## 块属性

| **Input**                 | **类型**              | **描述**                                              |
| ------------------------- | --------------------- | ------------------------------------------------------------ |
| **Mesh**                  | Mesh                  | 要采样的源网格资源。<br/>仅当将 **Source** 设置为 **Mesh** 时，才会显示此属性。 |
| **Skinned Mesh Renderer** | Skinned Mesh Renderer | 要采样的源 Skinned Mesh Renderer （蒙皮网格渲染器） 组件。这是对场景中组件的引用。要将蒙皮网格渲染器分配给此端口，请在 [Blackboard](Blackboard.md) 中创建蒙皮网格渲染器属性并公开该属性。<br/>仅当将 **Source** 设置为 **Skinned Mesh Renderer** 时，才会显示此属性。 |
| **Vertex**                | uint                  | 要采样的顶点的索引。<br/>仅当将 **Placement mode** 设置为 **Vertex** and **Spawn Mode** 设置为 **Custom** 时，才会显示此属性。 |
| **Index**                 | uint                  | 要从中采样的边缘的起始索引。Block 使用此索引和以下索引选择要从中采样的行。<br/>仅当将 **Placement mode** 设置为 **Edge** and **Spawn Mode** 设置为 **Custom** 时，才会显示此属性。 |
| **Triangle**              | uint                  | 要采样的三角形的索引，假设索引缓冲区包含一个三角形列表。<br/>仅当将 **Placement mode** 设置为 **Surface**, **Spawn Mode** 设置为 **Custom**, and **Spawn Mode** 设置为 **Custom** 时，才会显示此属性。 |
| **Edge**                  | float                 | Block 用于沿边缘采样的插值。这是沿边缘（从开始位置到结束位置）采用采样位置的百分比。<br/>仅当将 **Placement mode** 设置为 **Edge** ，并将 **Spawn Mode** 设置为 **Custom** 时，才会显示此属性。 |
| **Barycentric**           | Vector2               | 要从三角形 at 采样的原始重心坐标。输入是二维的（**X** 和 **Y**），模块使用您输入的值计算 **Z** 值：`Z = 1 - X - Y`。此采样方法不限制三角形边缘内的采样位置。<br/>仅当将 **Placement mode** 设置为 **Surface**, **Surface coordinates** 设置为 **Barycentric**, 并将 **Spawn Mode** 设置为 **Custom**时，才会显示此属性。 |
| **Square**                | Vector2               | 对三角形进行采样的 uniform 坐标。Block 获取此值并将其从平方坐标映射到三角形空间。为此，它使用了论文 [A Low-Distortion Map Between Triangle and Square](https://hal.archives-ouvertes.fr/hal-02073696v2) (Heitz 2019) 中的方法大纲。<br/>仅当将 **Placement mode** 设置为 **Surface**, **Surface coordinates** 设置为 **Uniform**, and **Spawn Mode** 设置为 **Custom** 时，才会显示此属性。 |
| **Blend Position**        | Float                 | 当前位置属性值与新计算的位置值之间的混合百分比。<br/>仅当将 **Composition Position** 设置为 **Blend** 时，才会显示此属性。 |
| **Blend Direction**       | Float                 | 当前方向属性值与新计算的方向值之间的混合百分比。<br/>仅当将 **Composition Direction** 设置为 **Blend** 时，才会显示此属性。 |

## 局限性

Mesh sampling （网格采样） 功能具有以下限制：

- 它仅支持除 [Color](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Color.html) 以外的所有 [VertexAttributes](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.html) 的 [VertexAttributeFormat.Float32](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.Float32.html), [Color](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Color.html)必须是使用[VertexAttributeFormat.UInt8](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.UInt8.html) 或 [VertexAttributeFormat.Float32](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.Float32.html) 格式的四个组件属性。
- 如果网格是不可 [读](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html)的, 则 **Position (Mesh)** Block 和 **Sample Mesh** Operator 在尝试从中采样时返回零值。有关如何使网格可读的信息，请参阅 [模型导入设置](https://docs.unity3d.com/Manual/FBXImporter-Model.html)。

![](Images/ReadWrite.png)

## 参考列表

* Heitz, Eric. 2019. "A Low-Distortion Map Between Triangle and Square". [hal-02073696v2](https://hal.archives-ouvertes.fr/hal-02073696v2)
