
Color 节点
========

描述
--

使用 **Color** 字段在着色器中定义一个常量 **Vector 4** 值。可通过[节点](Node.md)的上下文菜单转换为 **Color** [属性类型](Property-Types.md)。生成属性时，也会考虑 **Mode** 参数的值。


注意：在 10\.0 之前的版本中，Shader Graph 假定来自 Color 节点的 HDR 颜色位于伽马空间。版本 10\.0 更正了此行为，现在，Shader Graph 在线性空间中解释 HDR 颜色。使用旧版本创建的 HDR Color 节点仍保持旧行为，但可以使用 [Graph Inspector](Internal-Inspector.md)  将其升级。要在新的 HDR Color 节点上模仿旧行为，可以使用 [Colorspace Conversion 节点](Colorspace-Conversion-Node.md)将 HDR 颜色从 **RGB** 转换为 **Linear**。


端口
--




| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Out | 输出 |  Vector 4 | 无 | 输出值 |


控件
--




| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
|  | Color |  | 定义输出值。 |
| Mode | 下拉选单 | Default、HDR | 设置 Color 字段的属性 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
float4 _Color = IsGammaSpace() ? float4(1, 2, 3, 4) : float4(SRGBToLinear(float3(1, 2, 3)), 4);

```

