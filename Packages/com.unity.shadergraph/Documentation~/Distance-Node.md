
Distance 节点
===========


描述
--


返回输入 **A** 和输入 **B** 的值之间的欧几里德距离。除了其他方面的用途，这对于计算空间中两点之间的距离很有用，通常用于计算[有符号距离函数 (Signed Distance Function)](https://en.wikipedia.org/wiki/Signed_distance_function)。


端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| A | 输入 | 动态矢量 | 第一个输入值 |
| B | 输入 | 动态矢量 | 第二个输入值 |
| Out | 输出 | Float | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。

```
void Unity_Distance_float4(float4 A, float4 B, out float Out)
{
    Out = distance(A, B);
}

```

