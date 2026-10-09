# Get Attribute: seed

菜单路径：**Operator > Attribute > Get Attribute: seed**

**Get Attribute: seed** 根据 [Location](Attributes.md#attribute-locations) 返回模拟元素的[标准属性](Reference-Attributes.md) seed。此运算符输出一个唯一种子，Visual Effect Graph 将其用于随机数计算。

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| seed | uint | 基于 **Location** 的 seed 属性的值。  </br>如果尚未写入此属性，则此运算符返回默认属性值。 |

## Details

属性使用系统空间（本地空间或世界空间）返回的值。
