# Get Attribute: targetPosition

菜单路径：**Operator > Attribute > Get Attribute: targetPosition**

**Get Attribute: targetPosition** 根据 [Location](Attributes.md#attribute-locations) 返回模拟元素的[标准属性](Reference-Attributes.md)targetPosition。这个 targetPosition 属性有多种用途。例如，如果要存储一个可以访问的位置，可以将其用作存储助手，或者，对于 Line Renderer，将其用作每个线粒子的端点。

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| targetPosition | Vector3 | 基于 **Location** 的 targetPosition 属性的值。  </br>如果尚未写入此属性，则此运算符返回默认属性值。 |

## Details

属性使用系统空间（本地空间或世界空间）返回的值。
