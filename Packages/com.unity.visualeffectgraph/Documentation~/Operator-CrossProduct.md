# 叉积

菜单路径： **Operator > Math > Vector**

在两个输入矢量之间 **叉积**[均匀算子](Operators.md#uniform-operators)执行[叉积](https://docs.unity3d.com/ScriptReference/Vector3.Cross.html)，并输出结果。

两个向量的叉积是垂直于这两个向量的向量。其长度取决于两个输入矢量之间的角度以及它们的长度。

两个垂直的规格化向量的叉积是垂直于这两个向量的规格化向量。

## 运算符属性

| **Input** | **类型**| **Description**                         |
| --------- | --------------------------------------- | --------------------------------------- |
| **A**     | [可配置](#operator-configuration)| The first vector of the cross product.  |
| **B**     | [可配置](#operator-configuration)| The second vector of the cross product. |

| **Output** | **Type**  | **描述**|
| ---------- | --------- | ------------------------------------------------------------ |
| **Output** | Dependent |交叉乘积 <br/>**类型**的结果。更改以匹配和**乙**的**A**类型。|

## 运算符配置

要查看此[统一算子](Operators.md#uniform-operators)的配置，请单击**齿轮**节点标题中的图标。你可以在此处配置此运算符使用的数据类型。

### 可用类型

你可以将以下类型用于**输入值**和端口：

-  **Vector**
-  **Vector3**
-  **Position**
-  **Direction**