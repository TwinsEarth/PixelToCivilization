<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"> <b>实验性：</b>此功能目前是实验性的，在以后的主要版本中可能会发生变化。</div>

# Sample Mesh

菜单路径： **Operator > Sampling > Sample Mesh**

菜单路径： **Operator > Sampling > Sample Skinned Mesh**

“采样网格”或“蒙皮网格”操作符允许你获取静态或蒙皮几何体的顶点数据。

## 运算符设置

| **Property**            | **Type** | **描述**|
| ----------------------- | -------- | ------------------------------------------------------------ |
| **Output**              | Enum     | **（检查器）**选择要从顶点读取的顶点属性。|
| **Mode**                | Enum     |用于序列的环绕模式。选项包括：<br/>•**夹钳**：钳制第一个顶点和最后一个顶点之间的索引。<br/>•**包裹**：将索引环绕到顶点列表的另一侧。<br/>•**镜子**：镜像顶点列表，以便超出范围的索引在列表中来回移动。|
| **Placement mode**      | Enum     |指定要从网格的哪个基本体部分采样：<br/>•**顶点**：从所有列出的顶点采样位置。<br/>•**边缘**：从两个连续顶点之间的插值采样，这两个连续顶点是网格上三角形的一部分。<br/>•**表面**：从网格上定义三角形的三个顶点之间的插值采样。|
| **Surface coordinates** | Enum     |指定此块用于对三角形的曲面进行采样的方法。<br/>•**重心的**：使用原始重心坐标对曲面进行采样。使用此方法，采样位置不受三角形边的约束，如果已在Visual Effect Graph外部烘焙了一个位置，这将非常有用。<br/>•**制服**：在三角形区域<br/>内均匀地对曲面进行采样。此属性仅在设置**放置模式**为**表面**和 **产卵模式****习俗**时显示。|
| **Source**              | Enum     | **（检查器）**指定要从中采样的几何图形的种类。选项包括：<br/>•**网眼**：网格资源中的采样。<br/>•**蒙皮网格渲染器**：网格资源中的[蒙皮网格渲染器](https://docs.unity3d.com/Manual/class-SkinnedMeshRenderer.html)采样。|

### 运算符属性

| **Input**                 | **Type**              | **描述**|
| ------------------------- | --------------------- | ------------------------------------------------------------ |
| **Mesh**                  | Mesh                  |要采样的源网格资源。仅当设置**源头**为**网眼**时，才会显示该<br/>属性|
| **Skinned Mesh Renderer** | Skinned Mesh Renderer |要采样的源蒙皮网格渲染器组件。这是对场景中组件的引用。若要将蒙皮网格渲染器指定给此端口，请在中[Blackboard](Blackboard.md)创建蒙皮网格渲染器属性并显示该属性。此<br/>属性仅在设置**源头**为**蒙皮网格渲染器**时才会显示。|
| **Vertex**                | uint                  |要采样的顶点的索引。此<br/>属性仅在设置**放置模式**为**顶点**时显示。|
| **Index**                 | uint                  |要从其进行采样的边的起始索引。块使用此索引和下一个索引来选择要从中<br/>采样的线。此属性仅在设置**放置模式**为**边缘**和 **产卵模式****习俗**时显示。|
| **Triangle**              | uint                  |要采样的三角形的索引，假设索引缓冲区包含三角形列表。此<br/>属性仅在设置**放置模式**为**表面**、**产卵模式**为**习俗**和**产卵模式**为**习俗**时才显示。|
| **Edge**                  | float                 |块用于沿边采样的插值值。这是沿边从开始位置到结束位置所取的采样位置的百分比。此<br/>属性仅在设置**放置模式**为**边缘**和 **产卵模式****习俗**时才显示。|
| **Barycentric**           | Vector2               |要从三角形采样的原始重心坐标。输入是二维的（**X**和**是的**），块**Z**使用你输入的值计算值： **Z**= **1**- **X**- **是的**。此采样方法不约束三角形边缘内的采样位置。<br/>此属性仅在设置**放置模式**为**表面**、**表面坐标**为**重心的**和**产卵模式**为**习俗**时才显示。|
| **Square**                | Vector2               |对三角形进行采样的统一坐标。块获取此值并将其从正方形坐标映射到三角形空间。为此，它使用论文[介于三角形和正方形之间的低失真贴图](https://hal.archives-ouvertes.fr/hal-02073696v2)（Heitz 2019）中概述的方法。此<br/>属性仅在你将设置 **放置模式****表面**为、**表面坐标**设置为**制服**和**产卵模式**设置为**习俗**时才会出现。|

| **Output**       | **Type** | **描述**|
| ---------------- | -------- | ------------------------------------------------------------ |
| **Position**     | Vector3  |仅当在中**输出**选择**位置**时，才会显示此特性的顶点属性 [Position](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Position.html)<br/>|
| **Normal**       | Vector3  |仅当在中**输出**选择**正常的**时，才会显示此特性的顶点属性 [Normal](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Normal.html)<br/>|
| **Tangent**      | Vector3  |仅当在中**输出**选择**正切**时，才会显示此特性的顶点属性 [Tangent](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Tangent.html)<br/>|
| **Color**        | Vector4  |仅当在中**输出**选择**颜色**时，才会显示此特性的顶点属性 [Color](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Color.html)<br/>|
| **TexCoord0-7**  | Vector4  |顶点属性[TEXCOORD 0至7](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.TexCoord0.html)。对于未指定的尺寸标注，此为**0**。只有在选择了“**TexCoord0 - 7**内**输出**”时，才会显示此<br/>属性|
| **BlendWeight**  | Vector4  |顶点属性[混合重量](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.BlendWeight.html)。仅当在中**输出**选择**混合重量**时，才会显示该<br/>属性|
| **BlendIndices** | Vector4  |顶点属性[混合指数](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.BlendIndices.html)。仅当在中**输出**选择**混合指数**时，才会显示该<br/>属性|

#### 局限性

“网格”（Mesh）采样功能具有以下限制：

- 它只支持 [顶点属性格式。float32](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.Float32.html)ALL [顶点属性](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.html)EXCEPT [Color](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Color.html)，它必须是使用[VertexAttributeFormat.uint8](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.UInt8.html)或[顶点属性格式。float32](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.Float32.html)格式的四个组件属性。
- 如果网格不[readable](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html)是，则**位置（网格）**块和**Sample Mesh**操作符在尝试从中采样时将返回零值。有关如何使网格可读的信息，请参见[模型导入设置](https://docs.unity3d.com/Manual/FBXImporter-Model.html)。

 ![](Images/ReadWrite.png)

## 参考列表

* 海茨，埃里克。2019.“介于三角形和正方形之间的低失真贴图”。 [Hal-02073696V2](https://hal.archives-ouvertes.fr/hal-02073696v2)
