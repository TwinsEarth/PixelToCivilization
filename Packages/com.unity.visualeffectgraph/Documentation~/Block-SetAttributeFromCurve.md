# Set Attribute from Curve/Gradient

菜单路径：**Attribute > Curve > [Add/Set] \<Attribute> \<Mode>**

**Set Attribute from Curve/Gradient** 代码块是一个通用代码块，它允许您使用合成，根据来自 **Animation Curve** 或 **Gradient** 的样本向属性中写入值。此代码块可以使用各种采样模式实现这一点。采样模式有：

+   **Over Life**：计算粒子的相对年龄 (Age/Lifetime ratio) 并用其对曲线/渐变进行采样从而获取值。
    
+   **From Speed**：根据速度属性的长度计算速度，并用它来曲线/渐变进行采样。
    
+   **Random** 和 **RandomUniformPerParticle**：计算 0 到 1 之间的随机值并用其对曲线/渐变进行采样。这种随机方法使您能够有机地创建非线性分布。
+   **Custom**：公开一个浮点输入端口，您可以结合使用属性或运算符，以自定义的方式对曲线进行采样。

## 代码块兼容性

此代码块兼容于以下上下文：

+   [Initialize](Context-Initialize.md)
+   [Update](Context-Update.md)
+   任何输出上下文

## 代码块设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Attribute** | Attribute | **（检查器）** 指定要写入的属性。 |
| **Composition** | Enum | **（检查器）** 指定此代码块如何合成属性。选项有：  <br/>• **Set**：用新值覆盖位置属性。  <br/>• **Add**：将新值添加到位置属性值。  <br/>• **Multiply**：将位置属性值乘以新值。  <br/>• **Blend**：在位置属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |
| **Alpha Composition** | Enum | **（检查器）** 指定此代码块如何合成颜色属性的 alpha 组件。选项：  <br/>• **Set**：用新值覆盖 alpha。  <br/>• **Add**：将新值添加到 alpha 值。  <br/>• **Multiply**：将 alpha 值乘以新值。  <br/>• **Blend**：在 alpha 值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |
| **Sample Mode** | Enum | 指定如何计算用于对曲线/渐变进行采样的值。选项：  <br/>• **OverLife**：使用年龄/生命属性比率。  <br/>• **BySpeed**：计算粒子的速度，并根据最小和最大速度范围值为速度指定一个介于 0 和 1 之间的值。  <br/>• **Random**：每次代码块执行时计算一个 0 到 1 之间的随机数。  <br/>• **RandomConstantPerParticle**：计算一个 0 到 1 之间的随机数。这个随机数对于每个粒子都是唯一的。  <br/>• **Custom** 使用您可以通过输入端口指定的自定义值。 |
| **Mode** | Enum | **（检查器）** 指定代码块如何计算合成属性的随机值。  <br/>• **PerComponent**：对不同曲线中每个组件的值或颜色属性的渐变进行采样。  <br/>• **Uniform**：从单个通用 AnimationCurve 中为每个组件采样值。  <br/>此设置仅在设置的 **Attribute** 包含多个组件时可见。 |
| **Channels** | Enum | 指定此代码块要影响的属性通道。此代码块不会影响您未包含在此属性中的通道。  <br/>此设置仅在您设置的 **Attribute** 包含通道时显示。 |
| **ColorMode** | Enum | 指定此代码块影响的颜色属性的组件。选项：  <br/>• **Color**：仅影响粒子的颜色。  <br/>• **Alpha**：仅影响粒子的 alpha。  <br/>• **Color And Alpha**：影响粒子的颜色和 alpha。  <br/>此设置仅在将 **Attribute** 设置为 **color** 时可见。 |

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **<Attribute>[_x/y/z]** | 取决于属性 | 提供代码块用于对值进行采样的 **AnimationCurve** 或 **Gradient**。 <br/>如果将 **Mode** 设置为 **Uniform**，则仅显示具有属性名称的单个曲线。  <br/>如果将 **Mode** 设置为 **PerComponent**，对于除颜色以外的所有属性，将显示每个组件的 **AnimationCurve** 端口。  <br/>如果将 **Mode** 设置为 **PerComponent**，对于颜色属性，这将显示渐变。 |
| **Speed Range** | Vector2 | 此代码块用于标准化粒子速度的最小和最大速度。这个过程本质上与 [InverseLerp](https://docs.unity3d.com/ScriptReference/Mathf.InverseLerp.html) 相同，其中最小值 (**x**) 是 **a**，最大值 (**y**) 是 **b**，粒子的速度为 **value**。  <br/>此属性仅在将 **Sample Mode** 设置为 **BySpeed** 时显示。 |
| **Sample Time** | float | 此代码块用于对曲线/渐变进行采样。  <br/>此属性仅在将 **Sample Mode** 设置为 **Custom** 时显示。 |
| **Blend** | float | 当前属性值与新计算的值之间的混合百分比。  <br/>此属性仅在将 **Composition** 或 **Alpha Composition** 设置为 **Blend** 时显示。 |
