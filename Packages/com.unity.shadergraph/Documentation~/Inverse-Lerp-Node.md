
Inverse Lerp 节点
===============


描述
--

返回在输入 **A** 到输入 **B** 范围内生成由输入 **T** 指定的插值的线性参数。


**Inverse Lerp** 是 [Lerp 节点](Lerp-Node.md)的逆运算。可用于确定 [Lerp](Lerp-Node.md) 的什么输入基于其输出。


例如，**T** 值为 1 时，0 和 2 之间的 **Lerp** 值为 0\.5。因此，**T** 值为 0\.5 时，0 和 2 之间的 **Inverse Lerp** 值为 1。


端口
--




| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| A | 输入 | 动态矢量 | 第一个输入值 |
| B | 输入 | 动态矢量 | 第二个输入值 |
| T | 输入 | 动态矢量 | 时间值 |
| Out | 输出 | 动态矢量 | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_InverseLerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = (T - A)/(B - A);
}

```

