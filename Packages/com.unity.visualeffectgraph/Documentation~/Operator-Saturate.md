# Saturate

菜单路径： **Operator > Math > Clamp > Saturate**

 **Saturate**操作符将返回值限制在0和1之间。

此操作符接受各种类型的输入值。有关此运算符可以使用的类型的列表，请参见[可用类型](#available-types)。

## 运算符属性

| **Input** | **类型**| **Description**                    |
| --------- | --------------------------------------- | ---------------------------------- |
| **Input** | [可配置](#operator-configuration)| The value this Operator evaluates. |

| **Input** | **Type**  | **描述**|
| --------- | --------- | ------------------------------------------------------------ |
| **Out**   | Dependent |输入值限制在0和1 <br/>**类型**之间。更改以匹配的**投入**类型。|

## 操作员配置

要查看操作员的配置，请单击**齿轮**操作员标题中的图标。你可以选择一种超越所有[可用类型](#available-types)的类型。



### 可用类型

你可以为**输入**端口使用以下类型：

-  **float**
-  **Vector**
-  **Vector2**
-  **Vector3**
-  **Vector4**
-  **Position**
-  **Direction**