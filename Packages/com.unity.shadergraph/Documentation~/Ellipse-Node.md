Ellipse 节点 
================

描述
---  
生成基于输入 **UV** 的椭圆（Ellipse）形状，大小由输入 **宽度（Width）** 和 **高度（Height）** 指定。可以通过连接 [Tiling And Offset 节点](Tiling-And-Offset-Node.md) 来偏移或平铺生成的形状。请注意，为了保持在 UV 空间中偏移形状的能力，如果平铺，形状不会自动重复。要实现重复的点状效果，先通过 [Fraction 节点](Fraction-Node.md) 连接输入。

注意：此 [节点](Node.md) 仅可用于 **片段** [着色器阶段](Shader-Stage.md)。

端口
---  
| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | 输入的 UV 值 |
| 宽度 | 输入 | Float | 无 | 椭圆的宽度 |
| 高度 | 输入 | Float | 无 | 椭圆的高度 |
| 输出 | 输出 | Float | 无 | 输出值 |

生成的代码示例 
-------- 
以下示例代码展示了此节点的可能实现结果：

```
void Unity_Ellipse_float(float2 UV, float Width, float Height, out float4 Out)
{
    float d = length((UV * 2 - 1) / float2(Width, Height));
    Out = saturate((1 - d) / fwidth(d));
}
```