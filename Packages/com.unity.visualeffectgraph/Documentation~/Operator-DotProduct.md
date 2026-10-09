# 点积

菜单路径： **Operator > Math > Vector**

在两个输入矢量之间 **点积**[均匀算子](https://docs.unity.cn/cn/Packages-cn/com.unity.visualeffectgraph@latest/manual/Operators.html#uniform-operators)执行[点积](https://docs.unity3d.com/ScriptReference/Vector3.Dot.html)，并输出结果。

此运算符可用于多种用途，包括：

- 计算给定向量的平方长度。为此，将同一矢量连接到两个输入端口。
- 以确定两个标准化矢量是否指向同一方向。如果矢量指向同一方向，则返回1.0；如果它们垂直，则返回0.0；如果它们指向相反方向，则返回 - 1.0。
- 将向量**A**投影到规格化的向量**乙**上，并返回其沿**乙**的投影长度。

## 运算符属性

| **Input** | **类型**| **Description**                       |
| --------- | --------------------------------------- | ------------------------------------- |
| **A**     | [可配置](#operator-configuration)| The first vector of the dot product.  |
| **B**     | [可配置](#operator-configuration)| The second vector of the dot product. |

| **Output** | **Type** | **描述**|
| ---------- | -------- | ------------------------------------------------------------ |
| **Output** | float    |点积 <br/>**类型**的结果。更改以匹配和**乙**的**A**类型。|

## 运算符配置

要查看此[统一算子的](Operators.md#uniform-operators)配置，请单击**齿轮**节点标题中的图标。你可以在此处配置此运算符使用的数据类型。

### 可用类型

你可以将以下类型用于**输入值**和端口：

-  **Vector**
-  **Vector2**
-  **Vector3**
-  **Position**
-  **Direction**