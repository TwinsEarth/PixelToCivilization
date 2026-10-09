# Load Texture3D

菜单路径： **Operator > Sampling > Load Texture3D**

 **Load Texture3D**操作符允许你读取指定坐标和MIP级别的Texture3D的纹元值。返回float4纹素值，而不进行任何过滤。

这将转换为高级着色语言（HLSL）中对纹理的load（）调用。有关加载与采样之间差异的信息，请参阅[装载和取样](#loading-and-sampling)。

[!include[](Snippets/Operator-LoadingAndSampling.md)]

## 运算符属性

| **Input**     | **Type**  | **描述**|
| ------------- | --------- | ------------------------------------------------------------ |
| **Texture**   | Texture3D |要读取的纹理。|
| **X**         | uint      |要读取的纹理元素的X坐标。这是在0到纹理的宽度减去1的范围内。|
| **Y**         | uint      |要读取的纹理元素的Y坐标。这在0到纹理高度减去1的范围内。|
| **Z**         | uint      |要读取的纹理元素的Z坐标。这是在0到纹理的深度减去1的范围内。|
| **Mip Level** | uint      |要读取的MIP级别。|

| **Output** | **Type** | **描述**|
| ---------- | -------- | ---------------------- |
| **s**      | Vector4  |纹理元素的值|

## 局限性

这是一个仅限GPU的操作符，因此在插入**生成上下文**端口时不起作用。
