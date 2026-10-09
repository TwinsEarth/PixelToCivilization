# AABox

菜单路径：**Operator > Inline > AABox**

**AABox** 内联运算符允许您存储 [AABox](Type-AABox.md) 实例。这是 Visual Effect Graph 的高级[类型](VisualEffectGraphTypeReference.md)之一。要修改此内联运算符中的值，请在运算符正文中显式设置该值，或将其连接到兼容的输出。


[!include[](Snippets/Operator-InlineIntro.md)]

## 运算符属性

| **输入** | **类型** | **描述** |
| --- | --- | --- |
| **输入** | AABox | 运算符的值。您可以在运算符正文中设置它，也可以从另一个运算符的输出连接到它。 |

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **输出** | AABox | 运算符的值。 |

[!include[](Snippets/Operator-InlineNotes.md)]
