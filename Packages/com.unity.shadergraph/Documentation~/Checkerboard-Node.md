Checkerboard 节点
=================

[](#description)描述
---------------------------

根据输入的 **UV**，生成一个由 **Color A** 和 **Color B** 交替组成的棋盘格（Checkerboard）。棋盘格的比例由输入 **频率（Frequency）** 来定义。

[](#ports)端口
---------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | 输入 UV 值 |
| Color A | 输入 | 颜色 RGB | 无 | 棋盘格的第一个颜色 |
| Color B | 输入 | 颜色 RGB | 无 | 棋盘格的第二个颜色 |
| Frequency | 输入 | Vector 2 | 无 | 棋盘格在每个轴上的比例 |
| Out | 输出 | Vector 2 | 无 | 输出的 UV 值 |

[](#generated-code-example)生成的代码示例
-------------------------------------------------

以下示例代码代表了该节点的一个可能输出结果。

```
void Unity_Checkerboard_float(float2 UV, float3 ColorA, float3 ColorB, float2 Frequency, out float3 Out)
{
    UV = (UV.xy + 0.5) * Frequency;
    float4 derivatives = float4(ddx(UV), ddy(UV));
    float2 duv_length = sqrt(float2(dot(derivatives.xz, derivatives.xz), dot(derivatives.yw, derivatives.yw)));
    float width = 1.0;
    float2 distance3 = 4.0 * abs(frac(UV + 0.25) - 0.5) - width;
    float2 scale = 0.35 / duv_length.xy;
    float freqLimiter = sqrt(clamp(1.1f - max(duv_length.x, duv_length.y), 0.0, 1.0));
    float2 vector_alpha = clamp(distance3 * scale.xy, -1.0, 1.0);
    float alpha = saturate(0.5f + 0.5f * vector_alpha.x * vector_alpha.y * freqLimiter);
    Out = lerp(ColorA, ColorB, alpha.xxx);
}

```