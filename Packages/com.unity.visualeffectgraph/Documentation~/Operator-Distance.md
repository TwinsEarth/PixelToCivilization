# Distance

菜单路径： **Operator > Math > Vector**

 **Distance**[均匀算子](Operators.md#uniform-operators)计算1D、2D或3D空间中两点之间的Distance。

## 运算符属性

| **Input** | **类型**| **Description**               |
| --------- | --------------------------------------- | ----------------------------- |
| **A**     | [可配置](#operator-configuration)| The position to measure from. |
| **B**     | [可配置](#operator-configuration)| The position to measure to.   |

| **Output** | **Type**  | **描述**|
| ---------- | --------- | ------------------------------------------------------------ |
| **Output** | Dependent |两点之间的Distance。 <br/>**类型**更改以匹配和**乙**的**A**类型。|

## 运算符配置

要查看此[统一算子的](Operators.md#uniform-operators)配置，请单击**齿轮**节点标题中的图标。你可以在此处配置此运算符使用的数据类型。

### 可用类型

你可以将以下类型用于**输入值**和端口：

-  **float**
-  **Vector**
-  **Vector2**
-  **Vector3**
-  **Position**
-  **Direction**