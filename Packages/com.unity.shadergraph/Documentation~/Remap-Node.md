
Remap 节点
========


描述
--


根据输入 **In** 值在输入 **In Min Max** 的 x 和 y 分量之间的线性插值，返回输入 **Out Min Max** 的 x 和 y 分量之间的值。


端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| In | 输入 | 动态矢量 | 输入值 |
| In Min Max | 输入 | Vector 2 | 输入插值的最小值和最大值 |
| Out Min Max | 输入 | Vector 2 | 输出插值的最小值和最大值 |
| Out | 输出 | 动态矢量 | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_Remap_float4(float4 In, float2 InMinMax, float2 OutMinMax, out float4 Out)
{
    Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

```

