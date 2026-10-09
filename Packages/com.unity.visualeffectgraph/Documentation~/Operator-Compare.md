# Compare

菜单路径：**Operator > Logic > Compare**

**Compare** 运算符根据条件比较两个浮点数，并返回布尔值结果。它按`Left \[Condition\] Right`条件评估两个浮点值，如果 **Left** 是 0.5，**Right** 是 1.0，**Condition** 是 **Greater**，评估结果为`0.5 大于 1.0`，因此返回 false。

## 运算符设置

| **属性** | **描述** |
| --- | --- |
| **条件** | 指定此运算符用于评估的 **Left** 和 **Right** 的条件。选项： </br>•**Equal**：如果 **Left** 等于 **Right**，则返回`true`。否则返回`false`。此条件对应于 C# 中的`==`。  </br>•**Not Equals**：如果 **Left** 不等于 **Right**，则返回`true`。否则返回`false`。此条件对应于 C# 中的`!=`。  </br>•**Less**：如果 **Left** 小于 **Right**，则返回`true`。否则返回`false`。此条件对应于 C# 中的`<`。  </br>•**Less Or Equal**：如果 **Left** 小于或等于 **Right**，则返回`true`。否则返回`false`。此条件对应于 C# 中的`<=`。  </br>•**Greater**：如果 **Left** 大于 **Right**，则返回`true`。否则返回`false`。此条件对应于 C# 中的`>`。  </br>•**Greater Or Equal**：如果 **Left** 大于或等于 **Right**，则返回`true`。否则返回`false`。此条件对应于 C# 中的`>=`。 |

## 运算符属性

| **输入** | **类型** | **描述** |
| --- | --- | --- |
| **Left** | float | 比较算式左侧的值。 |
| **Right** | float | 比较算式右侧的值。 |

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **o** | bool | 比较的结果。 |
