# Get Attribute: mass

菜单路径：**Operator > Attribute > Get Attribute: mass**

**Get Attribute: mass** 根据 [Location](Reference-Attributes.md) 返回模拟元素的[标准属性](Reference-Attributes.md) mass。此运算符以 kg/dm<sup>3</sup> 为单位输出粒子质量。

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| mass | float | 基于 **Location** 的 mass 属性的值。</br>如果尚未写入此属性，则此运算符返回默认属性值。 |

## Details

属性使用系统空间（本地空间或世界空间）返回的值。
