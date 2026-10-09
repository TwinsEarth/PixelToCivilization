
Matrix Transpose 节点
================


描述
--

返回由输入 **In** 定义的矩阵的转置值。这可以看作是在对角线上翻转矩阵的操作。结果是切换矩阵的行和列索引。

端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| In | 输入 | 动态矩阵 | 输入值 |
| Out | 输出 | 动态矩阵 | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_MatrixTranspose_float4x4(float4x4 In, out float4x4 Out)
{
    Out = transpose(In);
}

```

