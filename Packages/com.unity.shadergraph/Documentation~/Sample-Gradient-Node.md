
Sample Gradient 节点
==================


描述
--

根据给定的 **Time** 输入对**渐变（Gradient）** 进行采样。返回 **Vector 4** 颜色值以便在着色器中使用。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Gradient | 输入 | Gradient | 无 | 要采样的渐变 |
| Time | 输入 | Float | 无 | 采样渐变的时间点 (0\.0–1\.0\) |
| Out | 输出 | Vector 4 | 无 | 输出值（Vector 4） |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_SampleGradient_float(float4 Gradient, float Time, out float4 Out)
{
    float3 color = Gradient.colors[0].rgb;
    [unroll]
    for (int c = 1; c < 8; c++)
    {
        float colorPos = saturate((Time - Gradient.colors[c-1].w) / (Gradient.colors[c].w - Gradient.colors[c-1].w)) * step(c, Gradient.colorsLength-1);
        color = lerp(color, Gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), Gradient.type));
    }
# ifndef UNITY_COLORSPACE_GAMMA
    color = SRGBToLinear(color);
# endif
    float alpha = Gradient.alphas[0].x;
    [unroll]
    for (int a = 1; a < 8; a++)
    {
        float alphaPos = saturate((Time - Gradient.alphas[a-1].y) / (Gradient.alphas[a].y - Gradient.alphas[a-1].y)) * step(a, Gradient.alphasLength-1);
        alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type));
    }
    Out = float4(color, alpha);
}

```

