# Get Attribute: scale

菜单路径：**Operator > Attribute > Get Attribute: scale**

**Get Attribute: scale** 根据 [Location](Attributes.md#attribute-locations) 返回模拟元素的[标准属性](Reference-Attributes.md) scale。此运算符输出模拟元素的非均匀比例乘数。

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| scale | Vector3 | 基于 **Location** 的 scale 属性的值。  </br>如果尚未写入此属性，则此运算符返回默认属性值。 |

## Details

属性使用系统空间（本地空间或世界空间）返回的值。




# Get Attribute: scale

Menu Path : **Operator > Attribute > Get Attribute: scale**

The **Get Attribute: scale** returns the scale, which is a [standard attribute](Reference-Attributes.md), of a simulated element depending on [Location](Attributes.md#attribute-locations). This Operator outputs the non-uniform scale multiplier of the simulated element.

[!include[](Snippets/Operator-GetAttributeOperatorSettings.md)]

## Operator properties

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| scale      | Vector3  | The value of the scale attribute, based on **Location**.<br/>If this attribute has not been written to, this Operator returns the default attribute value. |

## Details

The value the attribute returns uses the system’s space (either local-space or world-space).
