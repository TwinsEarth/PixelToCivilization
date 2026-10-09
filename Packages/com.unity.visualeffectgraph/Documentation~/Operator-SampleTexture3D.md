# Sample Texture3D

菜单路径： **Operator > Sampling > Sample Texture3D**

该**Sample Texture3D**操作符针对指定的UV和MIP级别对Texture3D进行采样。此操作符使用与输入纹理[导入设置](https://docs.unity3d.com/Manual/class-TextureImporter.html)相同**过滤器模式**的和**换行模式**。

这将转换为高级着色语言（HLSL）中对纹理的sample（）调用。有关加载与采样之间差异的信息，请参阅[装载和取样](#loading-and-sampling)。

[!include[](Snippets/Operator-LoadingAndSampling.md)]

## 运算符属性

| **Property**  | **Type**  | **描述**|
| ------------- | --------- | -------------------------------------------------- |
| **Texture**   | Texture3D |此操作符采样的纹理。|
| **UVW**       | Vector3   |“ UV This”操作符用于对纹理进行采样。|
| **Mip Level** | float     |此运算符用于采样的MIP级别。|

| **Property** | **Type** | **描述**|
| ------------ | -------- | ---------------------------------- |
| **s**        | Vector4  |纹理中的采样值|

## 局限性

此操作符仅在GPU上运行，因此在插入**产卵器上下文**端口时无法正常工作。
