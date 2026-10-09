Rounded Polygon 节点 
================

描述
---  
生成基于输入 **UV** 的圆角多边形形状（Rounded Polygon）形状，大小由输入 **宽度（Width）** 和 **高度（Height）** 指定。输入 **Sides** 指定边数，输入 **Roundness** 定义每个角的圆度。

可以通过连接 [Tiling And Offset 节点](Tiling-And-Offset-Node.md) 来偏移或平铺生成的形状。请注意，为了保持在 UV 空间中偏移形状的能力，如果平铺，形状不会自动重复。要实现重复的点状效果，先通过 [Fraction 节点](Fraction-Node.md) 连接输入。

注意：此 [节点](Node.md) 仅可用于 **片段** [着色器阶段](Shader-Stage.md)。

端口
---  
| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | 输入的 UV 值 |
| Width | 输入 | Float | 无 | 圆角多边形的宽度 |
| Height | 输入 | Float | 无 | 圆角多边形的高度 |
| Sides | 输入 | Float | 无 | 多边形的边数 |
| Roundness | 输入 | Float | 无 | 角的圆度 |
| Out | 输出 | Float | 无 | 输出值 |

生成的代码示例 
-------- 
以下示例代码展示了此节点的可能实现结果：

```
void RoundedPolygon_Func_float(float2 UV, float Width, float Height, float Sides, float Roundness, out float Out)
{
    UV = UV * 2. + float2(-1.,-1.);
    float epsilon = 1e-6;
    UV.x = UV.x / ( Width + (Width==0)*epsilon);
    UV.y = UV.y / ( Height + (Height==0)*epsilon);
    Roundness = clamp(Roundness, 1e-6, 1.);
    float i_sides = floor( abs( Sides ) );
    float fullAngle = 2. * PI / i_sides;
    float halfAngle = fullAngle / 2.;
    float opositeAngle = HALF_PI - halfAngle;
    float diagonal = 1. / cos( halfAngle );
    // Chamfer values
    float chamferAngle = Roundness * halfAngle; // Angle taken by the chamfer
    float remainingAngle = halfAngle - chamferAngle; // Angle that remains
    float ratio = tan(remainingAngle) / tan(halfAngle); // This is the ratio between the length of the polygon's triangle and the distance of the chamfer center to the polygon center
    // Center of the chamfer arc
    float2 chamferCenter = float2(
        cos(halfAngle) ,
        sin(halfAngle)
    )* ratio * diagonal;
    // starting of the chamfer arc
    float2 chamferOrigin = float2(
        1.,
        tan(remainingAngle)
    );
    // Using Al Kashi algebra, we determine:
    // The distance distance of the center of the chamfer to the center of the polygon (side A)
    float distA = length(chamferCenter);
    // The radius of the chamfer (side B)
    float distB = 1. - chamferCenter.x;
    // The refence length of side C, which is the distance to the chamfer start
    float distCref = length(chamferOrigin);
    // This will rescale the chamfered polygon to fit the uv space
    // diagonal = length(chamferCenter) + distB;
    float uvScale = diagonal;
    UV *= uvScale;
    float2 polaruv = float2 (
        atan2( UV.y, UV.x ),
        length(UV)
    );
    polaruv.x += HALF_PI + 2*PI;
    polaruv.x = fmod( polaruv.x + halfAngle, fullAngle );
    polaruv.x = abs(polaruv.x - halfAngle);
    UV = float2( cos(polaruv.x), sin(polaruv.x) ) * polaruv.y;
    // Calculate the angle needed for the Al Kashi algebra
    float angleRatio = 1. - (polaruv.x-remainingAngle) / chamferAngle;
    // Calculate the distance of the polygon center to the chamfer extremity
    float distC = sqrt( distA*distA + distB*distB - 2.*distA*distB*cos( PI - halfAngle * angleRatio ) );
    Out = UV.x;
    float chamferZone = ( halfAngle - polaruv.x ) < chamferAngle;
    Out = lerp( UV.x, polaruv.y / distC, chamferZone );
    // Output this to have the shape mask instead of the distance field
    Out = saturate((1 - Out) / fwidth(Out));
}
```