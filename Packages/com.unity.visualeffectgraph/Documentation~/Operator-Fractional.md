# Fractional

菜单路径： **Operator > Math > Arithmetic > Fractional**

 **Fractional**运算符返回输入的小数部分。例如，输入（4.5，0，2.2）输出（0.5，0，0.2）。

此操作符接受各种类型的输入值。有关此运算符可以使用的类型的列表，请参见[可用类型](#available-types)。

## 节点属性

| **Input** | **类型**| **Description**                    |
| --------- | --------------------------------------- | ---------------------------------- |
| **X**     | [可配置](#operator-configuration)| The value this Operator evaluates. |

| **Output** | **Type**  | **描述**|
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent |输入的Fractional。 <br/>**类型**更改以匹配运算符输入的最大向量类型。|

## 运算符配置

要查看节点的配置，请单击**齿轮**节点标题中的图标。你可以选择一种超越所有的类型[可用类型](#available-types)。



### 可用类型

你可以为**输入**端口使用以下类型：

-  **float**
-  **Vector**
-  **Vector2**
-  **Vector3**
-  **Vector4**
-  **Position**
-  **Direction**