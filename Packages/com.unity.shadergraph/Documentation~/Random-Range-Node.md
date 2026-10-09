
Random Range 节点
===============


描述
--


根据输入 **Seed** 返回由输入 **Min** 和 **Max** 分别定义的最小值和最大值之间的伪随机数值。


虽然输入 **Seed** 中的相同值将始终产生相同的输出值，但输出值本身将显示为随机数值。为便于根据 UV 输入生成随机数，输入 **Seed** 是 **Vector 2** 值，但是对于大多数情况，**Float** 输入就足够了。


端口
--




| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| Seed | 输入 | Vector 2 | 用于生成的种子值 |
| Min | 输入 | Float | 最小值 |
| Max | 输入 | Float | 最大值 |
| Out | 输出 | Float | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
{
    float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
    Out = lerp(Min, Max, randomno);
}

```

