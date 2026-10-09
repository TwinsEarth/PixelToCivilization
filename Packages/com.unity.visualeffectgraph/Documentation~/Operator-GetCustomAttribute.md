# Get Custom Attribute

菜单路径：**Operator > Attribute > Get Custom Attribute**

**Get Custom Attribute** 运算符根据属性位置返回给定类型的命名自定义属性的值。

## 运算符设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Attribute** | string | （**检查器**）自定义属性的名称。此名称区分大小写。 |
| **Location** | Location Enum | 属性的位置 |
| **Attribute Type** | Enum（[属性兼容的基本类型](VisualEffectGraphTypeReference.md#attribute-compatible-types)） | （**检查器**）用于属性的类型。 |

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **CustomAttribute** | Dependent | 基于属性位置的自定义属性的值。如果尚未写入该属性，则这是输出类型的默认值。  </br>此输出的类型与您在 **Attribute Type** 中指定的类型相匹配。 |

## Details

如果此运算符读取的自定义属性尚未写入，则输出值是输出类型的默认值。
