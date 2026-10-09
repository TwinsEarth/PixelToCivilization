Simple Noise节点
=================

[](#description)描述
---------------------------

基于输入 **UV** 生成 Simple 噪声或 [Value 噪声](https://en.wikipedia.org/wiki/Value_noise)。生成的噪声比例由输入 **Scale** 控制。

您还可以选择使用两种不同的哈希方法来计算噪声。团结引擎的 Simple Noise 节点默认使用 **Deterministic** 哈希方法，以确保跨平台一致的噪声生成效果。

[](#ports)端口
---------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | 输入的 UV 值 |
| Scale | 输入 | Float | 无 | 噪声比例 |
| Out | 输出 | Float | 无 | 输出值 |

[](#controls)控件
---------------------

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Hash Type | 下拉菜单 | Deterministic, LegacySine | 选择用于噪声生成的随机数哈希函数。 |

[](#generated-code-example)生成的代码示例
-------------------------------------------------

以下示例代码表示此节点的一种可能实现结果。

```
inline float unity_noise_randomValue (float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453);
}

inline float unity_noise_interpolate (float a, float b, float t)
{
    return (1.0-t)*a + (t*b);
}

inline float unity_valueNoise (float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);

    uv = abs(frac(uv) - 0.5);
    float2 c0 = i + float2(0.0, 0.0);
    float2 c1 = i + float2(1.0, 0.0);
    float2 c2 = i + float2(0.0, 1.0);
    float2 c3 = i + float2(1.0, 1.0);
    float r0 = unity_noise_randomValue(c0);
    float r1 = unity_noise_randomValue(c1);
    float r2 = unity_noise_randomValue(c2);
    float r3 = unity_noise_randomValue(c3);

    float bottomOfGrid = unity_noise_interpolate(r0, r1, f.x);
    float topOfGrid = unity_noise_interpolate(r2, r3, f.x);
    float t = unity_noise_interpolate(bottomOfGrid, topOfGrid, f.y);
    return t;
}

void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
{
    float t = 0.0;

    float freq = pow(2.0, float(0));
    float amp = pow(0.5, float(3-0));
    t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    freq = pow(2.0, float(1));
    amp = pow(0.5, float(3-1));
    t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    freq = pow(2.0, float(2));
    amp = pow(0.5, float(3-2));
    t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    Out = t;
}

```