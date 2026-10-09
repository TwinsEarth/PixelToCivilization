# Vector

菜单路径： **Operator > Inline > Vector**

 **Vector**内联操作符允许你存储[Vector](Type-Vector.md)。这是视觉效果图的进阶[Types](VisualEffectGraphTypeReference.md)之一。若要修改此内联运算符中的值，请在运算符主体中显式设置该值，或将其连接到兼容输出。

[!include[](Snippets/Operator-InlineIntro.md)]


## 运算符属性

| **Input** | **Type** | **描述**|
| --------- | -------- | ------------------------------------------------------------ |
| **Input** | Vector   |运算符的值。你可以在操作符的主体中设置它，也可以从另一个操作符的输出连接到它。|

| **Output** | **Type** | **描述**|
| ---------- | -------- | -------------------------- |
| **Output** | Vector   |运算符的值。|

[!include[](Snippets/Operator-InlineNotes.md)]
