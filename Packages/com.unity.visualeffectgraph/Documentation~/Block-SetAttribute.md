# Set Attribute

菜单路径：**Attribute > Set > [Add/Blend/Inherit/Multiply/Set] \<Attribute> **

**Set Attribute** 代码块是通用代码块，允许您使用合成将值写入属性。

您可以直接设置属性的值，也可以使用两种不同的随机模式，**Uniform** 和 **Per-Component**，将属性设置为随机值。

+   **Uniform** 计算 0 到 1 范围内的单个随机数，然后生成最终值，使用随机值在两个范围值（**A** 和 **B**）之间进行插值。
    
+   **Per-Component** 为属性类型中的每个组件计算 0 到 1 范围内的随机数，然后生成最终值，使用每个随机值在两个范围值（**A** 和 **B**）的每个组件之间进行插值。
    

![](Images/Block-SetAttributeExample.gif)

您还可以使用此代码块来继承源属性并将其直接设置为当前属性。例如，您可以继承父粒子的位置，并将其设置为从 GPU 事件生成的粒子。如果将 **Source** 设置为 **Source**，则代码块不显示属性的任何输入属性，而是使用 [source 属性](Attributes.md#attribute-locations)值（如果属性不存在，则为默认源属性值）。

## 代码块兼容性

此代码块兼容于以下上下文：

+   [Initialize](Context-Initialize.md)
+   [Update](Context-Update.md)
+   任何输出上下文

## 代码块设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Attribute** | Attribute（仅限检查器） | **（检查器）** 指定要写入的属性。 |
| **Composition** | Enum（仅限检查器） | **（检查器）** 指定此代码块如何合成属性。选项有：  <br/>• **Set**：用新值覆盖位置属性。  <br/>• **Add**：将新值添加到位置属性值。  <br/>• **Multiply**：将位置属性值乘以新值。  <br/>• **Blend**：在位置属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |
| **Source** | Enum（仅限检查器） | **（检查器）** 指定属性的来源。选项：  <br/>• **Slot**：从代码块的输入属性端口计算值。<br/>• **Source**：从同名源属性中获取值。 |
| **Random** | Enum | **（检查器）** 指定代码块如何计算要合成属性的值。选项：  <br/>• **Off**：不计算属性的随机值。使用您在输入中直接提供的值。  <br/>• **PerComponent**：为每个属性的组件计算一个随机值。  <br/>• **Uniform**：计算单个随机值并将其用于每个属性的组件。 |
| **Channels** | Enum | 指定要影响的属性通道。此运算符对您未使用此设置指定的通道没有影响。此设置仅在您设置的 **Attribute** 包含通道时显示。 |

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **\<Attribute>** | 取决于属性 | 要合成属性的值。  <br/>此属性仅在将 **Source** 设置为 **Slot** 且 **Random** 设置为 **Off** 时显示。 |
| **A** | 取决于属性 | 代码块用于计算属性值的第一个随机范围末端。  <br/>此属性仅在将 **Source** 设置为 **Slot** 且 **Random** 设置为 **PerComponent** 或 **Uniform** 时显示。 |
| **B** | 取决于属性 | 代码块用于计算属性值的另一个随机范围末端。  <br/>此属性仅在将 **Source** 设置为 **Slot** 且 **Random** 设置为 **PerComponent** 或 **Uniform** 时显示。 |
| **Blend** | Float (Range 0..1) | 当前位置属性值与新计算的位置值之间的混合百分比。<br/>此属性仅在将 **Composition** 设置为 **Blend** 时显示。 |
