View Vector 节点
================

[](#description)描述
---------------------------

此节点提供网格顶点或片元的**视图方向**(View Direction)向量的**未标准化版本**，不对存储的任何值进行标准化。如需标准化选项，请参见 [View Direction 节点](View-Direction-Node.md)。

选择一个 **Space** 以更改输出值的坐标空间。

[](#ports)端口
---------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Out | 输出 | Vector 3 | 无 | 网格顶点/片元的 View Vector。 |

[](#controls)控件
---------------------

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Space | 下拉选单 | Object、View、World、Tangent | 选择 **View Direction** 输出的坐标空间。 |