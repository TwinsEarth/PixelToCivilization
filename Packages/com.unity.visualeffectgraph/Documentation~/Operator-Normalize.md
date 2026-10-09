# Normalize

菜单路径： **Operator > Math > Vector**

对输入矢量 **Normalize**[均匀算子](Operators.md#uniform-operators)进行归一化。


任何零长度的输入向量都会产生非数字（Nan）结果，这会中断后续计算。

## 运算符属性

| **Input** | **类型**| **Description**  |
| --------- | --------------------------------------- | ---------------- |
| **X**     | [可配置](#operator-configuration)| The input vector |

| **Output** | **Type**  | **描述**|
| ---------- | --------- | ------------------------------------------------------------ |
| **Output** | Dependent |的规范化版本**X**。 <br/>**类型**更改以匹配的**X**类型。|

## 运算符配置

要查看此[统一算子的](Operators.md#uniform-operators)配置，请单击**齿轮**节点标题中的图标。你可以在此处配置此运算符使用的数据类型。

### 可用类型

你可以将以下类型用于**输入值**和端口：

-  **float**
-  **Vector**
-  **Vector2**
-  **Vector3**
-  **Vector4**
-  **Position**
-  **Direction**