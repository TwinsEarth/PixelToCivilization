
Combine 节点
==========


描述
--

从四个输入 **R**、**G**、**B** 和 **A** 创建新矢量（Vector）。输出 **RGBA** 是由输入 **R**、**G**、**B** 和 **A** 组成的**Vector 4**。输出 **RGB** 是由输入 **R**、**G** 和 **B** 组成的**Vector 3**。输出 **RG** 是由输入 **R** 和 **G** 组成的**Vector 2**。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| R | 输入 | Float | 无 | 定义输出的红色通道 |
| G | 输入 | Float | 无 | 定义输出的绿色通道 |
| B | 输入 | Float | 无 | 定义输出的蓝色通道 |
| A | 输入 | Float | 无 | 定义输出的 Alpha 通道 |
| RGBA | 输出 | Vector 4 | 无 | 输出值（**Vector 4**） |
| RGB | 输出 | Vector 3 | 无 | 输出值（**Vector 3**） |
| RG | 输出 | Vector 2 | 无 | 输出值（**Vector 2**） |


生成的代码示例
-------

以下示例代码表示此节点的一种可能结果。

```
void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}

```

