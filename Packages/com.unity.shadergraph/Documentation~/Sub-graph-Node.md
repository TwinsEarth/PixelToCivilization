
Sub Graph 节点
============

描述
--

提供对[子图资源](Sub-graph-Asset.md)的引用。参考节点上的所有端口都由[子图资源](Sub-graph-Asset.md)中定义的属性和输出进行定义。这对于在图之间共享功能或在图中复制相同功能非常有用。

用于 [Sub Graph](Sub-graph.md) 节点的预览由其 Output 节点的第一个端口决定。第一个端口有效的[数据类型](Data-Types.md)是 `Float`、`Vector 2`、`Vector 3`、`Vector 4`、`Matrix2`、`Matrix3`、`Matrix4` 和 `Boolean`。任何其他数据类型在预览着色器和[子图](Sub-graph.md)中都会产生错误。


子图节点和着色器阶段
-----------

如果[子图](Sub-graph.md)中的节点指定了一个[着色器阶段](Shader-Stage.md)（例如 [Sample Texture 2D 节点](Sample-Texture-2D-Node.md)指定**片元**着色器阶段），则整个[子图](Sub-graph.md)现在将锁定到该阶段。因此，引用该图的 [Sub Graph 节点](Sub-graph-Node.md)也将锁定到该着色器阶段。


此外，当连接到 **Sub Graph 节点**上的输出[端口](Port.md)的[边](Edge.md)流入主节点上的端口时，该 **Sub Graph 节点**现在将锁定到该主节点端口的着色器阶段。
