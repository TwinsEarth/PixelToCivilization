Rounded Rectangle 节点 
================

描述
---  
生成基于输入 **UV** 的圆角矩形形状（Rounded Rectangle）形状，大小由输入 **宽度（Width）** 和 **高度（Height）** 指定。每个拐角的半径由输入 **半径（Radius）** 定义。

可以通过连接 [Tiling And Offset 节点](Tiling-And-Offset-Node.md) 来偏移或平铺生成的形状。请注意，为了保持在 UV 空间中偏移形状的能力，如果平铺，形状不会自动重复。要实现重复的点状效果，先通过 [Fraction 节点](Fraction-Node.md) 连接输入。

注意：此 [节点](Node.md) 仅可用于 **片段** [着色器阶段](Shader-Stage.md)。

端口
---  
| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | 输入的 UV 值 |
| Width | 输入 | Float | 无 | 圆角矩形的宽度 |
| Height | 输入 | Float | 无 | 圆角矩形的高度 |
| Radius | 输入 | Float | 无 | 角的半径 |
| Out | 输出 | Float | 无 | 输出值 |

生成的代码示例 
-------- 
以下示例代码展示了此节点的可能实现结果：

```
void Unity_RoundedRectangle_float(float2 UV, float Width, float Height, float Radius, out float Out)
{
    Radius = max(min(min(abs(Radius * 2), abs(Width)), abs(Height)), 1e-5);
    float2 uv = abs(UV * 2 - 1) - float2(Width, Height) + Radius;
    float d = length(max(0, uv)) / Radius;
    Out = saturate((1 - d) / fwidth(d));
}
```