## Get Attribute: direction

菜单路径：**Operator > Attribute > Get Attribute: direction**

**Get Attribute: direction** 根据 [Location](Attributes.md#attribute-locations) 返回模拟元素的[标准属性](Reference-Attributes.md) direction。此运算符根据模拟元素的形状输出其方向。这会驱动 Set Velocity 代码块中的初始方向。

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| direction | Vector3 | 基于 **Location** 的 direction 属性的值。</br> 如果尚未写入此属性，则此运算符返回默认属性值。 |

## Details

属性使用系统空间（本地空间或世界空间）返回的值。
