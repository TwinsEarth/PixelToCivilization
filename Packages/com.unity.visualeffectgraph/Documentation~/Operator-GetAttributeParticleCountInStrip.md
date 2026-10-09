# Get Attribute: particleCountInStrip

菜单路径 : **Operator > Attribute > Get Attribute: particleCountInStrip**

**Get Attribute: particleCountInStrip** 根据 [Location](Attributes.md#attribute-locations) 返回模拟元素的[标准属性](Reference-Attributes.md)particleCountInStrip。此运算符输出当前粒子所属粒子条中的当前粒子数。

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| particleCountInStrip | int | 基于 **Location** 的 particleCountInStrip 属性的值。  
如果尚未写入此属性，则此运算符返回默认属性值。 |

## Details

属性使用系统空间（本地空间或世界空间）返回的值。

This operator can return wrong values when used in the Initialize context, if the strip index property is not constant.