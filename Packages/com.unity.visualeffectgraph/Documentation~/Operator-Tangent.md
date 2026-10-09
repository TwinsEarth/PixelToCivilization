# Tangent

菜单路径： **Operator > Math > Trigonometry > Tangent**

 **Tangent**运算符以弧度计算[tangent](https://docs.unity3d.com/ScriptReference/Mathf.Tan.html)输入的。

此操作符接受各种类型的输入值。有关此运算符可以使用的类型的列表，请参见[可用类型](#available-types)。

## 运算符属性

| **Input** | **Type**                                | **描述**|
| --------- | --------------------------------------- | ------------------------------------------------------------ |
| **X**     | [Configurable](#operator-configuration) |以弧度为单位的值，此运算符计算的Tangent。|

| **Output** | **Type**  | **描述**|
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent |输入的Tangent。 <br/>**类型**更改以匹配的**X**类型。|

## 运算符配置

要查看运算符的配置，请单击**齿轮**运算符标题中的图标。使用下拉列表选择**X**端口类型。有关此属性支持的类型的列表，请参见[可用类型](#available-types)。



### 可用类型

你可以为输入端口使用以下类型：

-  **float**
-  **Vector**
-  **Vector2**
-  **Vector3**
-  **Vector4**
-  **Position**
-  **Direction**