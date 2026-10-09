
Multiply 节点
===========


描述
--


返回输入 **A** 乘以输入 **B** 的结果。
* 如果两个输入都是矢量（Vector）类型，则输出类型将是矢量类型，且其维度与这些输入的评估类型相同。
* 如果两个输入都是矩阵（Matrix）类型，则输出类型将是矩阵类型，且其维度与这些输入的评估类型相同。
* 如果一个输入是矢量类型而另一个是矩阵类型，则输出类型将是矢量类型，且其维度与矢量类型输入相同。


端口
--




| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| A | 输入 | Dynamic | 第一个输入值 |
| B | 输入 | Dynamic | 第二个输入值 |
| Out | 输出 | Dynamic | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点不同的可能结果。


**Vector \* Vector**



```
void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
    Out = A * B;
}

```
**Vector \* Matrix**



```
void Unity_Multiply_float4_float4x4(float4 A, float4x4 B, out float4 Out)
{
    Out = mul(A, B);
}

```
**Matrix \* Matrix**



```
void Unity_Multiply_float4x4_float4x4(float4x4 A, float4x4 B, out float4x4 Out)
{
    Out = mul(A, B);
}

```

