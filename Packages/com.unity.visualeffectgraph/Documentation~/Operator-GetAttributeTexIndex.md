# Get Attribute: texIndex

菜单路径： **Operator > Attribute > Get Attribute: texIndex**

根据 [Location](Attributes.md#attribute-locations)**Get Attribute: texIndex**返回模拟元素的TexIndex，它是一个[标准属性](Reference-Attributes.md)。此操作符输出动画帧，用于对模拟元素的动画书UV进行采样。

[！包括 [](Snippets/Operator-GetAttributeOperatorSettings.md)]

## 运算符属性

| **Output** | **Type** | **描述**|
| ---------- | -------- | ------------------------------------------------------------ |
| texIndex   | float    |TexIndex属性的值，基于**位置**。如果<br/>尚未写入此属性，则此运算符将返回默认属性值。|

## 细节

属性返回的值使用系统的空间（局部空间或世界空间）。
