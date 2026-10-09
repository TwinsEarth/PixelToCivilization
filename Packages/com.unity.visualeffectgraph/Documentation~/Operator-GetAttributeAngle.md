# Get Attribute: angle

菜单路径 : **Operator > Attribute > Get Attribute: angle**

**Get Attribute: angle** 根据 [Location](Attributes.md#attribute-locations) 返回模拟元素的[标准属性](Reference-Attributes.md) angle。此运算符输出模拟元素的欧拉旋转（以度为单位）。

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| angle | Vector3 | 基于 **Location** 的 angle 属性的值。  </br>如果尚未写入此属性，则此运算符返回默认属性值。 |

## Details

属性使用系统空间（本地空间或世界空间）返回的值。