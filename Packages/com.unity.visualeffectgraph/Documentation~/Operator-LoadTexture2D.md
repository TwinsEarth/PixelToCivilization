# Load Texture2D

菜单路径 : **Operator > Sampling > Load Texture2D**

**Load Texture2D** Operator 允许您读取指定坐标和 mip 级别的 Texture2D 纹素值。此运算符返回 float4 纹素值，而不进行任何筛选。

这转换为高级着色语言 （HLSL） 中对纹理的 Load（） 调用。有关加载和采样之间差异的信息，请参阅 [加载和采样](#loading-and-sampling)。

[!include[](Snippets/Operator-LoadingAndSampling.md)]

## 运算符属性

| **Input**     | **类型**  | **描述**                                              |
| ------------- | --------- | ------------------------------------------------------------ |
| **Texture**   | Texture2D | 要从中读取的 Texture。                                 |
| **X**         | uint      | 要读取的纹素的 X 坐标。这是在 0 到纹理宽度减 1 的范围内。 |
| **Y**         | uint      | 要读取的纹素的 Y 坐标。该范围为 0 到纹理高度减 1。 |
| **Mip Level** | uint      | 要从中读取的 mip 级别。                                |

| **Output** | **类型** | **描述**         |
| ---------- | -------- | ----------------------- |
| **s**      | Vector4  | 纹素的值。 |

## 局限性

这是仅限 GPU 的 Operator，因此在插入 **Spawn Context** 端口时不起作用。
