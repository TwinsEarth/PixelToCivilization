# Texture2DArray

菜单路径： **Operator > Sampling > Load Texture2DArray**

 **Texture2DArray**操作符读取指定坐标和切片的Texture2dArray的纹元值。此运算符返回float4纹素值，而不进行任何过滤。

这将转换为高级着色语言（HLSL）中对纹理的load（）调用。有关加载与采样之间差异的信息，请参阅[装载和取样](#loading-and-sampling)。

[!include[](Snippets/Operator-LoadingAndSampling.md)]

## 运算符属性

| **Input**     | **Type**       | **描述**|
| ------------- | -------------- | ------------------------------------------------------------ |
| **Texture**   | Texture2DArray |此运算符从中加载的纹理数组。|
| **X**         | uint           |要读取的纹理元素的X坐标。这是在0到纹理的宽度减去1的范围内。|
| **Y**         | uint           |要读取的纹理元素的Y坐标。这在0到纹理高度减去1的范围内。|
| **Z**         | uint           |要读取的切片。|
| **Mip Level** | uint           |此运算符从中读取的MIP级别。|

| **Output** | **Type** | **描述**|
| ---------- | -------- | ----------------------- |
| **s**      | Vector4  |纹理元素的值。|

## 局限性

此操作符仅在GPU上运行，因此在插入**spwan器上下文**端口时无法正常工作。
