# Get Attribute: oldPosition

菜单路径 : **Operator > Attribute > Get Attribute: oldPosition**

**Get Attribute: oldPosition** 根据 [Location](Attributes.md#attribute-locations)返回模拟元素的[标准属性](Reference-Attributes.md) oldPosition。这个 oldPosition 属性是一个帮助器，您可以在集成元素的速度之前使用它来存储模拟元素的当前位置。

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]


## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| oldPosition | Vector3 | 基于 **Location** 的 oldPosition 属性的值。  
如果尚未写入此属性，则此运算符返回默认属性值。 |

## Details

属性使用系统空间（本地空间或世界空间）返回的值。
