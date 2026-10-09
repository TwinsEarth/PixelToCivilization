属性类型
====


描述
--


**属性类型**是[属性](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-Properties.html)的类型，可以在 [Blackboard](Blackboard.md) 上定义，然后在**图形**中使用。这些属性将会显示在使用着色器的[材质](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-Material.html)的 [Inspector](https://docs.unity.cn/cn/tuanjiemanual/Manual/UsingTheInspector.html) 中。


每个属性都有一个关联的**数据类型**。请参阅[数据类型](Data-Types.md)以了解更多信息。


通用参数
----

除了特定于[数据类型](Data-Types.md)的值之外，大多数属性都具有以下通用参数。




| 名称 | 类型 | 描述 |
| --- | --- | --- |
| Display Name | 字符串 | 属性的显示名称 |
| Exposed | 布尔值 (Boolean) | 如果为 true，此属性将在材质检视面板上显示 |
| Reference Name | 字符串 | 在着色器内用于属性的内部名称 |
| Override Property Declaration | 布尔值 (Boolean) | 一个高级选项，能够显式控制此属性的着色器声明 |
| Shader Declaration | 枚举 | 控制此属性的着色器声明 |


**注意**：如果要覆盖 **Reference Name** 参数，请注意以下情况：

* 如果 **Reference Name** 不以下划线开头，则会自动附加一个下划线字符。
* 如果 **Reference Name** 包含 HLSL 不支持的任何字符，则会删除这些字符。
* 可以恢复到默认的 **Reference Name**，方法是右键单击该名称并选择 **Reset Reference**。


Float
-----


定义一个 **Float** 值。




| 数据类型 | 模式 |
| --- | --- |
| Float | 默认值、滑动条、整数 |


#### 默认值


在材质检视面板中显示一个标量输入字段。




| 字段 | 类型 | 描述 |
| --- | --- | --- |
| Default | Float | [属性](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-Properties.html)的默认值。 |


#### 滑动条


在材质检视面板中显示一个滑动条字段。




| 字段 | 类型 | 描述 |
| --- | --- | --- |
| Default | Float | 属性的默认值。 |
| Min | Float | 滑动条的最小值。 |
| Max | Float | 滑动条的最大值。 |


#### 整数


在材质检视面板中显示一个整数输入字段。




| 字段 | 类型 | 描述 |
| --- | --- | --- |
| Default | 整数 | 属性的默认值。 |


Vector 2
----


定义一个 **Vector 2** 值。在材质检视面板中显示一个 **Vector 4** 输入字段，其中不使用 z 和 w 分量。




| 数据类型 | 模式 |
| --- | --- |
| Vector 2 |  |




| 字段 | 类型 | 描述 |
| --- | --- | --- |
| Default | Vector 2 | 属性的默认值。 |


Vector 3
----


定义一个**Vector 3** 值。在材质检视面板中显示一个**Vector 4** 输入字段，其中不使用 w 分量。




| 数据类型 | 模式 |
| --- | --- |
| Vector 3 |  |




| 字段 | 类型 | 描述 |
| --- | --- | --- |
| Default | Vector 3 | 属性的默认值。 |


Vector 4
----


定义一个**Vector 4** 值。在材质检视面板中显示一个**Vector 4** 输入字段。




| 数据类型 | 模式 |
| --- | --- |
| Vector 4 |  |




| 字段 | 类型 | 描述 |
| --- | --- | --- |
| Default | Vector 4 | 属性的默认值。 |


颜色
--


定义一个**颜色**值。如果属性检查器显示 **Main Color**，则这是着色器的 MainColor。要将此节点选择或取消选择为 **Main Color**，可在图中或 Blackboard 上右键单击节点，然后选择 **Set as Main Color** 或 **Clear Main Color**。该属性对应于 ShaderLab 属性中的 [`MainColor` 属性](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-Properties.html)。


| 数据类型 | 模式 |
| --- | --- |
| Color | Default、HDR |


#### 默认值

在材质检视面板中显示一个 sRGB 颜色字段。

| 字段 | 类型 | 描述 |
| --- | --- | --- |
| Default | Vector 4 | 属性的默认值。 |


#### HDR

在材质检视面板中显示一个 HDR 颜色字段。


| 字段 | 类型 | 描述 |
| --- | --- | --- |
| Default | Vector 4 | 属性的默认值。 |


注意：在 10\.0 之前的版本中，Shader Graph 未校正项目色彩空间的 HDR 颜色。10\.0 版本更正了此行为。使用旧版本创建的 HDR 颜色属性仍保持旧行为，但可以使用 [Graph Inspector](Internal-Inspector.md)  将其升级。要在伽马空间项目中模仿旧行为，可以使用 [Colorspace Conversion 节点](Colorspace-Conversion-Node.md)将新的 HDR **颜色**属性从 **RGB** 转换到 **Linear** 空间。


2D 纹理
-----


定义一个 [2D 纹理](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-TextureImporter.html)值。在材质检视面板中显示一个[纹理](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-TextureImporter.html)类型的对象字段。如果属性检查器显示 **Main Texture**，则这是着色器的 `Main Texture`。要将此节点选择或取消选择为 `Main Texture`，在图中或 Blackboard 上右键单击它，然后选择 **Set as Main Texture** 或 **Clear Main Texture**。对应于 ShaderLab 属性中的 [`MainTexture` 属性](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-Properties.html)。

| 数据类型 | 模式 |
| --- | --- |
| Texture | 白色、黑色、灰色、凹凸 |


| 字段 | 类型 | 描述 |
| --- | --- | --- |
| Default | Texture | 属性的默认值。 |
| Use Tiling and Offset | 布尔值（Boolean） | 设置为 false 时，激活属性 [NoScaleOffset](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-Properties.html)，以便独立于其他纹理属性操作缩放和偏移。参见 [SplitTextureTransformNode](Split-Texture-Transform-Node.md)。 |


3D 纹理
-----

定义一个 [3D 纹理](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-TextureImporter.html)值。在材质检视面板中显示一个 3D 纹理类型的对象字段。




| 数据类型 | 模式 |
| --- | --- |
| Texture |  |




| 字段 | 类型 | 描述 |
| --- | --- | --- |
| Default | Texture | 属性的默认值。 |


2D 纹理数组
-------

定义一个 [2D 纹理数组](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-TextureImporter.html)值。在材质检视面板中显示一个 2D 纹理数组类型的对象字段。


| 数据类型 | 模式 |
| --- | --- |
| Texture |  |

| 字段 | 类型 | 描述 |
| --- | --- | --- |
| Default | Texture | 属性的默认值。 |


立方体贴图
-----

定义一个[立方体贴图](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-Cubemap.html)值。在材质检视面板中显示一个纹理类型的对象字段。

| 数据类型 | 模式 |
| --- | --- |
| 立方体贴图 |  |

| 字段 | 类型 | 描述 |
| --- | --- | --- |
| Default | 立方体贴图 | 属性的默认值。 |



## 虚拟纹理 <a name="virtual-texture"></a>


定义一个[纹理堆栈](https://docs.unity.cn/cn/tuanjiemanual/Manual/svt-use-in-shader-graph.html)，在材质检视面板中显示为纹理类型的对象字段。字段数对应于属性中的层数。

| 数据类型 | 模式 |
| --- | --- |
| Virtual Texture |  |


| 字段 | 类型 | 描述 |
| --- | --- | --- |
| Default | Texture | 属性的默认值。 |


布尔值
---

定义一个**布尔值**。在材质检视面板中显示一个 **ToggleUI** 字段。请注意，在着色器内部，此值为 **Float**。Shader Graph 中的**布尔值**类型仅仅是为了便于使用。


| 数据类型 | 模式 |
| --- | --- |
| 布尔值 （Boolean） |  |


| 字段 | 类型 | 描述 |
| --- | --- | --- |
| Default | 布尔值 （Boolean）| 属性的默认值。 |



