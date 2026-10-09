
Compute Deformation 节点
======================


描述
--

此节点用于将计算变形的顶点数据传递给顶点着色器，并且仅适用于[Entities Graphics package](https://docs.unity.cn/cn/tuanjiemanual/Manual/com.unity.entities.graphics.html)。您必须在 `_DeformedMeshData` 缓冲区中提供 `DeformedVertexData`。该节点使用 `_ComputeMeshIndex` 属性来计算与当前网格关联的 `DeformedVertexData` 在 `_DeformedMeshData` 缓冲区中的位置。要输出数据，必须同时安装 Entities Graphics package 和 DOTS Animation packages 包，或者使用自定义解决方案。


端口
--




| 名称 | 方向 | 类型 | 阶段 | 描述 |
| --- | --- | --- | --- | --- |
| Position | 输出 | Vector 3 | 顶点 | 输出变形的顶点位置。 |
| Normal | 输出 | Vector 3 | 顶点 | 输出变形的顶点法线。 |
| Tangent | 输出 | Vector 3 | 顶点 | 输出变形的顶点切线。 |



