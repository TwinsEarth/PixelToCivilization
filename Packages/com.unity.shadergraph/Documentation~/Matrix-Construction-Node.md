
Matrix Construction 节点
======================


描述
--


从四个输入矢量 **M0**、**M1**、**M2** 和 **M3** 构造方阵。此节点可用于生成**矩阵 2x2**、**矩阵 3x3** 和**矩阵 4x4** 类型的矩阵。


节点上的下拉选单可用于选择输入值是指定矩阵行还是列。


* **Row**：输入矢量从上到下指定矩阵行。
* **Column**：输入矢量从左到右指定矩阵列。


矩阵输出取自输入结构的左上角。这可用于从不同维度的矢量生成不同维度的方阵。


例如，将**矢量 2** 类型的值连接到输入 **M0** 和 **M1** 将会从输出 **2x2** 生成所需的矩阵。


端口
--




| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| M0 | 输入 | Vector 4 | 第一行或第一列 |
| M1 | 输入 | Vector 4 | 第二行或第二列 |
| M2 | 输入 | Vector 4 | 第三行或第三列 |
| M3 | 输入 | Vector 4 | 第四行或第四列 |
| 4x4 | 输出 | 4x4 矩阵 | 输出为 4x4 矩阵 |
| 3x3 | 输出 | 3x3 矩阵 | 输出为 3x3 矩阵 |
| 2x2 | 输出 | 2x2 矩阵 | 输出为 2x2 矩阵 |


控件
--




| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
|  | 下拉选单 | Row、Column | 选择应如何填充输出矩阵 |


生成的代码示例
-------


以下示例代码表示此节点在每个模式下的一种可能结果。


**Row**



```
void Unity_MatrixConstruction_Row_float(float4 M0, float4 M1, float4 M2, float3 M3, out float4x4 Out4x4, out float3x3 Out3x3, out float2x2 Out2x2)
{
    Out4x4 = float4x4(M0.x, M0.y, M0.z, M0.w, M1.x, M1.y, M1.z, M1.w, M2.x, M2.y, M2.z, M2.w, M3.x, M3.y, M3.z, M3.w);
    Out3x3 = float3x3(M0.x, M0.y, M0.z, M1.x, M1.y, M1.z, M2.x, M2.y, M2.z);
    Out2x2 = float2x2(M0.x, M0.y, M1.x, M1.y);
}

```
**Column**



```
void Unity_MatrixConstruction_Column_float(float4 M0, float4 M1, float4 M2, float3 M3, out float4x4 Out4x4, out float3x3 Out3x3, out float2x2 Out2x2)
{
    Out4x4 = float4x4(M0.x, M1.x, M2.x, M3.x, M0.y, M1.y, M2.y, M3.y, M0.z, M1.z, M2.z, M3.z, M0.w, M1.w, M2.w, M3.w);
    Out3x3 = float3x3(M0.x, M1.x, M2.x, M0.y, M1.y, M2.y, M0.z, M1.z, M2.z);
    Out2x2 = float2x2(M0.x, M1.x, M0.y, M1.y);
}

```

