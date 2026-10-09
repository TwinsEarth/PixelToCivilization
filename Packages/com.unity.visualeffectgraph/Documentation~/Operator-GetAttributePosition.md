# Get Attribute: position

菜单路径：**Operator > Attribute > Get Attribute: position**

**Get Attribute: position** 根据 [Location](Attributes.md#attribute-locations) 返回模拟元素的[标准属性](Reference-Attributes.md) position。此运算符输出模拟元素在 [系统](Systems.md) 模拟空间中的位置。

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| position | Vector3 | 基于 **Location** 的 position 属性的值。  </br>如果尚未写入此属性，则此运算符返回默认属性值。 |

## Details

属性使用系统空间（本地空间或世界空间）返回的值。

# Get Attribute: position

Menu Path : **Operator > Attribute > Get Attribute: position**

The **Get Attribute: position** returns the position, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This Operator outputs the position of the simulated element, in the simulation space of the [System](Systems.md).

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| position   | Vector3  | The value of the position attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).
