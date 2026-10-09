Rectangle 节点 
================

描述  
----
生成基于输入 **UV** 的矩形形状（Rectangle），大小由输入 **宽度（Width）** 和 **高度（Height）** 指定。可以通过连接 [Tiling And Offset 节点](Tiling-And-Offset-Node.md) 来偏移或平铺生成的形状。请注意，为了保持在 UV 空间中偏移形状的能力，如果平铺，形状不会自动重复。要实现重复的点状效果，先通过 [Fraction 节点](Fraction-Node.md) 连接输入。

注意：此 [节点](Node.md) 仅可用于 **片段** [着色器阶段](Shader-Stage.md)。


端口
------  
| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | 输入的 UV 值 |
| Width | 输入 | Float | 无 | 矩形的宽度 |
| Height | 输入 | Float | 无 | 矩形的高度 |
| Out | 输出 | Float | 无 | 输出值 |

控件
----
|名称|类型|选项|描述|
|--|--|--|--|
|  |下拉菜单|Fastest, Nicest|计算的鲁棒性|


生成的代码示例  
------------
以下示例代码展示了此节点的可能实现结果：

```
void Unity_Rectangle_float(float2 UV, float Width, float Height, out float Out)
{
    float2 d = abs(UV * 2 - 1) - float2(Width, Height);
    d = 1 - d / fwidth(d);
    Out = saturate(min(d.x, d.y));
}
```