
Noise Sine Wave 节点
==================


描述
--

返回输入 **In** 的值的正弦值。为表现变化，正弦波的幅度中将添加伪随机噪声（处于输入 **Min Max** 确定的范围内）。


端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| In | 输入 | 动态矢量 | 输入值 |
| Min Max | 输入 | Vector 2 | 噪声强度的最小值和最大值 |
| Out | 输出 | 动态矢量 | 输出值 |


生成的代码示例
-------

```
void Unity_NoiseSineWave_float4(float4 In, float2 MinMax, out float4 Out)
{
    float sinIn = sin(In);
    float sinInOffset = sin(In + 1.0);
    float randomno =  frac(sin((sinIn - sinInOffset) * (12.9898 + 78.233))*43758.5453);
    float noise = lerp(MinMax.x, MinMax.y, randomno);
    Out = sinIn + noise;
}

```

