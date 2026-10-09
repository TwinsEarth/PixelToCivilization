Gradient Noise 节点
===================

[](#description)描述
---------------------------

基于输入 **UV** 生成渐变（Gradient）或 [Perlin](https://en.wikipedia.org/wiki/Perlin_noise) 噪声。生成的噪声比例由输入 **Scale** 控制。在性能开销方面，渐变噪声节点可能比采样纹理贴图稍微复杂一些。

您还可以选择使用两种不同的哈希方法来计算噪声。团结引擎的 Gradient Noise 节点默认使用 **Deterministic** 哈希方法，以确保跨平台的一致性噪声生成效果。由于 **UV** 值用于作为噪声生成的 seed，您可以通过偏移、缩放或扭曲 **UV** 值来生成不同的噪声模式。

[](#ports)端口
---------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | 输入的 UV 值 |
| Scale | 输入 | Float | 无 | 噪声比例 |
| Out | 输出 | Float | 无 | 输出值，范围为 0.0 到 1.0 |

[](#controls)控件
---------------------

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Hash Type | 下拉菜单 | Deterministic、LegacyMod | 选择用于噪声生成的随机数哈希函数。 |

生成的代码示例
-------

以下示例代码表示此节点的一种可能结果。

```
float2 unity_gradientNoise_dir(float2 p)
{
    p = p % 289;
    float x = (34 * p.x + 1) * p.x % 289 + p.y;
    x = (34 * x + 1) * x % 289;
    x = frac(x / 41) * 2 - 1;
    return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

float unity_gradientNoise(float2 p)
{
    float2 ip = floor(p);
    float2 fp = frac(p);
    float d00 = dot(unity_gradientNoise_dir(ip), fp);
    float d01 = dot(unity_gradientNoise_dir(ip + float2(0, 1)), fp - float2(0, 1));
    float d10 = dot(unity_gradientNoise_dir(ip + float2(1, 0)), fp - float2(1, 0));
    float d11 = dot(unity_gradientNoise_dir(ip + float2(1, 1)), fp - float2(1, 1));
    fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
    return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x);
}

void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
{
    Out = unity_gradientNoise(UV * Scale) + 0.5;
}

```