
Matrix Determinant 节点
==================


描述
--

返回由输入 **In** 定义的矩阵的行列式。可以视为矩阵描述的变换的缩放因子。


端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| In | 输入 | 动态矩阵 | 输入值 |
| Out | 输出 | Float | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_MatrixDeterminant_float4x4(float4x4 In, out float Out)
{
    Out = determinant(In);
}

```

