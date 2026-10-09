# Inverse Lerp

菜单路径： **Operator > Math > Arithmetic > Inverse Lerp**

 **Inverse Lerp**运算符计算一个分数，该分数表示值在两个边界值之间的距离。[LERP运算符](Operator-Lerp.md)接受分数并输出两个值之间的混合，而此运算符接受混合值并输出分数。例如，如果**A**和**乙**是两个边界值，**测试**是分数，**价值**是和**乙**之间**A**的混合：

* LERP（**A**，**乙**，**测试**）= **价值**。
* Inverse Lerp（**A**，**乙**，**价值**）= **测试**。

此操作符接受许多不同类型的输入值。有关此运算符可以使用的类型的列表，请参见[可用类型](#available-types)。**X**和**是的**输入始终为同一类型。**S**输入要么是一个浮点数，要么是一个与**X**和**是的**大小相同的向量。

## 运算符属性

| **Input** | **类型**| **Description**                        |
| --------- | --------------------------------------- | -------------------------------------- |
| **X**     | [可配置](#operator-configuration)| The value to interpolate from.         |
| **Y**     | [可配置](#operator-configuration)| The value to interpolate to.           |
| **S**     | [可配置](#operator-configuration)| A value for the inverse interpolation. |

| **Output** | **Type**  | **描述**|
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent |和 **是的**<br/>**类型**之间**X**的**S**线性插值的逆。更改以匹配和**是的**的**X**类型。|

## 运算符配置

要查看运算符的配置，请单击**齿轮**运算符标题中的图标。**X**并且**是的**必须是相同类型[可用类型](#available-types)的。**S**要么是浮点型，要么与**X**和**是的**的类型相同。如果**S**是矢量类型Unity，则按值计算插值。

如果s在X和y之间，则结果在0和1之间。



### 可用类型

你可以为**输入**端口使用以下类型：

-  **float**
-  **Vector**
-  **Vector2**
-  **Vector3**
-  **Vector4**
-  **Position**
-  **Direction**