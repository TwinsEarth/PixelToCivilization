# Get Attribute: axisX

菜单路径 : **Operator > Attribute > Get Attribute: axisX**

**Get Attribute: axisX** 根据 [Location](Attributes.md#attribute-locations) 返回模拟元素的[标准属性](Reference-Attributes.md) axisX。此运算符输出模拟元素的右向轴。

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| axisX | Vector3 | 基于 **Location** 的 axisX 属性的值。  </br>如果尚未写入此属性，则此运算符返回默认属性值。 |

## Details

属性使用系统空间（本地空间或世界空间）返回的值。