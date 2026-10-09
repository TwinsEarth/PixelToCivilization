# Set Position (Shape : Cone)

菜单路径：**Position > Set Position (Shape : Cone)**

**Set Position (Shape : Cone)** 代码块根据输入 \[ArcCone\](Type-ArcCone .md) 计算位置，并根据合成效果将结果存储在 [position 属性](Reference-Attributes.md) 之中。

ArcCone 形状将弧属性添加到锥体以确定其弧度角（以弧度为单位）。设置 pi 的弧值会创建一个半锥体。

此代码块可以从 ArcCone 的 **Surface**、**Volume** 或者 **Thick Surface** 计算位置，其中厚度可以是相对于形状的大小，也可以是绝对值。

此代码块还根据计算出的形状上的位置计算一个方向向量，并根据合成效果将其存储到 [direction attribute](Reference-Attributes.md)。该方向等于从锥体中心到计算位置的标准化矢量。

备注：[Velocity from Direction and Speed](Block-VelocityFromDirectionAndSpeed.md)  代码块以后可以处理方向属性。

![](Images/Block-SetPosition(Cone)Main.gif)

## 代码块兼容性

此代码块兼容于以下上下文：

+   [Initialize](Context-Initialize.md)
+   [Update](Context-Update.md)
+   任何输出上下文

## 代码块设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Height Mode** | Enum | 指定此代码块如何处理 ArcCone 的体积。选项：  <br/>• **Volume**：使用整个圆锥体来计算位置。  <br/>• **Base**：仅使用锥体底部的圆来计算位置。 |
| **Position Mode** | Enum | 指定此代码块如何使用形状来计算位置。选项有：  <br/>•**Surface**：仅计算形状表面上的位置。 <br/>• **Volume**：计算整个形状体积内的位置。  <br/>• **Thickness Absolute**：计算给定绝对厚度的厚表面上的位置。  <br/>• **Thickness Relative** 将计算给定形状大小百分比的厚表面上的位置。 |
| **Spawn Mode** | Enum | 此代码块用于沿形状的弧分布粒子的方法。  <br/>•**Random**：计算弧上的每粒子随机进度 (0..1)。  <br/>**Custom**：允许您在 **Arc Sequencer** 属性中指定进度。 |
| **Composition Position** | Enum | **（检查器）**指定此代码块如何合成位置属性。选项有：  <br/>• **Set**：用新值覆盖位置属性。  <br/>• **Add**：将新值添加到位置属性值。  <br/>• **Multiply**：将位置属性值乘以新值。  <br/>• **Blend**：在位置属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |
| **Composition Direction** | Enum | **（检查器）**指定此代码块如何合成方向属性。选项有：  <br/>• **Set**：用新值覆盖位置属性。  <br/>• **Add**：将新值添加到位置属性值。  <br/>• **Multiply**：将位置属性值乘以新值。  <br/>• **Blend**：在位置属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **ArcCone** | [ArcCone](Type-ArcCone.md) | 确定要从中计算位置的形状的 ArcCone。 |
| **Thickness** | Float | 用于位置计算的形状表面的厚度。  <br/>此属性仅在将 **Position Mode** 设置为 **Thickness Relative** 或 **Thickness Absolute** 时才显示。 |
| **Arc Sequencer** | Float | 弧中生成粒子的位置。  <br/>此属性仅在将 **Spawn Mode** 设置为 **Custom** 时显示。 |
| **Blend Position** | Float | 当前位置属性值与新计算的位置值之间的混合百分比。  <br/>此属性仅在将 **Composition Position** 设置为 **Blend** 时显示。 |
| **Blend Direction** | Float | 当前方向属性值与新计算的方向值之间的混合百分比。  <br/>此属性仅在将 **Composition Direction** 设置为 **Blend** 时显示。 |

