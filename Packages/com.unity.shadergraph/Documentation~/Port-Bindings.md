
端口绑定
====


描述
--


某些输入[端口](Port.md)可能具有**端口绑定（Port Bindings）**。这意味着应该有提供给端口的数据，例如**法线矢量**或 **UV**。但是，**端口绑定**仅影响没有连接[边](Edge.md)的端口。这些端口仍然具有常规[数据类型](Data-Types.md)来定义可以连接的边。


实际上，这意味着如果没有边连接到端口，则该端口中使用的默认数据将取自其**端口绑定**。**端口绑定**及其相关默认选项的完整列表如下所示。


端口绑定列表
------

| 名称 | 数据类型 | 选项 | 描述 |  |
| --- | --- | --- | --- | --- |
| Bitangent | Vector 3 |  | 顶点或片元副切线，标签描述了期望的变换空间 |  |
| Color | Vector 4 |  | RGBA 颜色选取器 |  |
| ColorRGB | Vector 3 |  | RGB 颜色选取器 |  |
| Normal | Vector 3 |  | 顶点或片元法线Vector，标签描述了期望的变换空间 |  |
| Position | Vector 3 |  | 顶点或片元位置，标签描述了期望的变换空间 |  |
| Screen Position | Vector 4 |  | Default、Raw、Center、Tiled | 屏幕空间中的顶点或片段位置。下拉菜单用于选择模式。详细信息请参见[屏幕位置节点](Screen-Position-Node.md)。 |
| Tangent | Vector 3 |  | 顶点或片元切线Vector，标签描述了期望的变换空间 |  |
| UV | Vector 2 |  | UV0、UV1、UV2、UV3 | 网格 UV 坐标。下拉选择 UV 通道。 |
| Vertex Color | Vector 4 |  | RGBA 顶点颜色值。 |  |
| View Direction | Vector 3 |  | 顶点或片元位置，标签描述了期望的变换空间 |  |



