
Linear Blend Skinning 节点
========================


描述
--


此节点用于应用线性混合顶点蒙皮，并且仅适用于 [Entities Graphics package](https://docs.unity.cn/cn/tuanjiemanual/Manual/com.unity.entities.graphics.html)。您必须在 `_SkinMatrices` 缓冲器提供蒙皮矩阵。该节点使用 `_SkinMatrixIndex` 属性来计算与当前网格关联的矩阵在 `_SkinMatrices` 缓冲区中的位置。


端口
--

| 名称 | 方向 | 类型 | 阶段 | 描述 |
| --- | --- | --- | --- | --- |
| Position | 输入 | Vector 3 | 顶点 | 顶点在对象空间中的位置。 |
| Normal | 输入 | Vector 3 | 顶点 | 顶点在对象空间中的法线。 |
| Tangent | 输入 | Vector 3 | 顶点 | 顶点在对象空间中的切线。 |
| Position | 输出 | Vector 3 | 顶点 | 输出带蒙皮的顶点位置。 |
| Normal | 输出 | Vector 3 | 顶点 | 输出带蒙皮的顶点法线。 |
| Tangent | 输出 | Vector 3 | 顶点 | 输出带蒙皮的顶点切线。 |



