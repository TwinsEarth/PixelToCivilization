# Lerp

菜单路径： **Operator > Math > Arithmetic > Lerp**

 **Lerp**运算符计算两个边界值之间的值的线性插值。

此操作符接受各种类型的输入值。有关此运算符可以使用的类型的列表，请参见[可用类型](#available-types)。**X**和**是的**输入始终为同一类型。**S**输入要么是一个浮点数，要么是一个与**X**和**是的**大小相同的向量。如果**S**介于0和1之间，则结果介于**X**和**是的**之间。

## 运算符属性

| **Input** | **类型**| **Description**                |
| --------- | --------------------------------------- | ------------------------------ |
| **X**     | [可配置](#operator-configuration)| The value to interpolate from. |
| **Y**     | [可配置](#operator-configuration)| The value to interpolate to.   |
| **S**     | [可配置](#operator-configuration)| A value for the interpolation. |

| **Output** | **Type**  | **描述**|
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent |s在X和 <br/>**类型**y之间的线性插值。更改以匹配和**是的**的**X**类型。|

## 运算符配置

要查看运算符的配置，请单击**齿轮**运算符标题中的图标。中[可用类型](#available-types)的类型 **X****是的**必须相同。如果**S**是向量类型，则Unity按值计算插值。



### 可用类型

你可以为**输入**端口使用以下类型：

-  **float**
-  **Vector**
-  **Vector2**
-  **Vector3**
-  **Vector4**
-  **Position**
-  **Direction**