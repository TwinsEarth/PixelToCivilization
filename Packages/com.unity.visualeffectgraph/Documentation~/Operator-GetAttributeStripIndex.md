# Get Attribute: stripIndex

菜单路径：**Operator > Attribute > Get Attribute: stripIndex**

**Get Attribute: stripIndex** 根据 [Location](Attributes.md#attribute-locations) 返回模拟元素的[标准属性](Reference-Attributes.md)stripIndex。此运算符输出此粒子所属粒子条的索引。

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| stripIndex | uint | 基于 **Location** 的 stripIndex 属性的值。  </br>如果尚未写入此属性，则此运算符返回默认属性值。 |

## Details

属性使用系统空间（本地空间或世界空间）返回的值。
