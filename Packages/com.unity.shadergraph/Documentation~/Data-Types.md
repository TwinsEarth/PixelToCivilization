数据类型
====

描述
--

Shader Graph 中有许多**数据类型（Data Types）**。节点上的每个**端口**都有一个关联的**数据类型**，用于定义可以连接到哪些边。**数据类型**具有可用性颜色，这些颜色应用于该**数据类型**的端口和边。

有些**数据类型**具有关联的[属性类型](Property-Types.md)，用于将这些值显示给使用着色器的[材质](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-Material.html)的 [Inspector](https://docs.unity.cn/cn/tuanjiemanual/Manual/UsingTheInspector.html)。


数据类型
----

| 名称 | 颜色 | 描述 |
| --- | --- | --- |
| Float | 浅蓝 | 一个 **Float** 或标量值 |
| Vector 2 | 绿色 | 一个**Vector 2** 值。 |
| Vector 3 | 黄色 | 一个**Vector 3** 值。 |
| Vector 4 | 粉红色 | 一个**Vector 4** 值。 |
| Dynamic Vector | 浅蓝 | 请参阅下面的**动态数据类型** |
| Matrix 2 | 蓝色 | 一个**矩阵 2x2** 值 |
| Matrix 3 | 蓝色 | 一个**矩阵 3x3** 值 |
| Matrix 4 | 蓝色 | 一个**矩阵 4x4** 值 |
| Dynamic Matrix | 蓝色 | 请参阅下面的**动态数据类型** |
| Dynamic | 蓝色 | 请参阅下面的**动态数据类型** |
| Boolean | 紫色 | 一个**布尔**值。在生成的着色器中定义为浮点数 |
| Texture 2D | 红色 | 一个 [2D 纹理](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-TextureImporter.html) 资源 |
| Texture 2D Array | 红色 | 一个 [2D 纹理数组](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-TextureImporter.html)资源 |
| Texture 3D | 红色 | 一个 [3D 纹理](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-TextureImporter.html) 资源 |
| Cubemap | 红色 | 一个[立方体贴图](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-Cubemap.html)资源 |
| Virtual Texture | 灰色 | 一个[纹理堆栈](https://docs.unity.cn/cn/tuanjiemanual/Manual/svt-use-in-shader-graph.html) |
| Gradient | 灰色 | 一个**渐变**值。在生成的着色器中定义为结构 |
| SamplerState | 灰色 | 用于对纹理进行采样的状态。 |


提升/截断
-----


可以提升或截断所有**Vector**类型以匹配任何**Vector**类型端口。仅当相关的端口不是**动态Vector**类型时，才会发生此行为。截断时将会直接移除多余的通道。在提升时，所需的额外通道将填充默认值。这些值为 (0, 0, 0, 1\)。


动态数据类型
------


有些**数据类型**是动态的。这意味着使用这些**数据类型**的端口可以根据与其连接的**数据类型**更改其基础的**实际数据类型**。默认情况下，使用动态**数据类型**的节点只能有一个**实际数据类型**，这意味着一旦连接的边将其**数据类型**应用于该端口，该节点的所有其他**动态数据类型**字段将应用相同的**数据类型**。


一个值得注意的例外是 [Multiply 节点](Multiply-Node.md)，该节点允许**动态矩阵**和**Vector**类型。


### 动态矢量

**动态矢量（Dynamic Vector）** 类型允许任何 **Vector** 类型的连接边。除非最低维度为 1（在这种情况下将提升 **Float**），否则所有连接边将自动截断为具有最低维度的类型。


### 动态矩阵


**动态矩阵**类型允许任何**矩阵**类型的连接边。所有连接边将自动截断为具有最低维度的类型。


### 动态


**动态**类型是一种特殊情况。支持该类型的节点必须定义其验证方式。在 [Multiply 节点](Multiply-Node.md)的情况下，可允许任何 **Vector** 或**矩阵**类型的连接，从而确保根据**数据类型**的混合应用正确的乘法。



