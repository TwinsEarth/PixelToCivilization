# Set Position (Shape : Line)

菜单路径：**Position > Set Position (Shape : Line)**

**Set Position (Shape : Line)** 代码块根据输入 Line 计算位置，并根据合成效果将结果存储在 [position 属性](Reference-Attributes.md) 之中。

此代码块还根据计算出的形状上的位置计算一个方向向量，并根据合成效果将其存储到 [direction attribute](Reference-Attributes.md)。该方向等于从直线起点到终点的归一化矢量。

备注：[Velocity from Direction and Speed](Block-VelocityFromDirectionAndSpeed.md)代码块以后可以处理方向属性。

![](Images/Block-SetPosition(Line)Main.gif)

## 代码块兼容性

此代码块兼容于以下上下文：

+   [Initialize](Context-Initialize.md)
+   [Update](Context-Update.md)
+   任何输出上下文

## 代码块设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Spawn Mode** | Enum | 指定如何沿线分布粒子：选项包括：  <br/>• **Random**：计算沿线的每粒子随机进度（从 0 到 1）。  <br/>• **Custom**：允许您在 **Line Sequencer** 属性中手动指定线上的进度。 |
| **Composition Position** | Enum | **（检查器）**指定此代码块如何合成位置属性。选项有：  <br/>• **Set**：用新值覆盖位置属性。  <br/>• **Add**：将新值添加到位置属性值。  <br/>• **Multiply**：将位置属性值乘以新值。  <br/>• **Blend**：在位置属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |
| **Composition Direction** | Enum | **（检查器）**指定此代码块如何合成方向属性。选项有：  <br/>• **Set**：用新值覆盖位置属性。  <br/>• **Add**：将新值添加到位置属性值。  <br/>• **Multiply**：将位置属性值乘以新值。  <br/>• **Blend**：在位置属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Line** | Line | 确定要从中计算位置的形状的 Line。 |
| **Line Sequencer** | Float | 确定粒子在线上的位置（以其进度的百分比表示）。  <br/>此属性仅在将 **Spawn Mode** 设置为 **Custom** 时显示。 |
| **Blend Position** | Float | 当前位置属性值与新计算的位置值之间的混合百分比。  <br/>此属性仅在将 **Composition Position** 设置为 **Blend** 时显示。 |
| **Blend Direction** | Float | 当前方向属性值与新计算的方向值之间的混合百分比。  <br/>此属性仅在将 **Composition Direction** 设置为 **Blend** 时显示。 |
