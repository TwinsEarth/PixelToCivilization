# Get Attribute: lifetime

菜单路径 : **Operator > Attribute > Get Attribute: lifetime**

**Get Attribute: lifetime** 根据 [Location](Attributes.md#attribute-locations) 返回模拟元素的[标准属性](Reference-Attributes.md) lifetime。此运算符输出模拟元素应存活的时间量。

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| lifetime | float | 基于 **Location** 的 lifetime 属性的值。  </br>如果尚未写入此属性，则此运算符返回默认属性值。 |

## Details

属性使用系统空间（本地空间或世界空间）返回的值。