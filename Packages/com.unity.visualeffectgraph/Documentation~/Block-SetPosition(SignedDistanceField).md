# Set Position (Signed Distance Field)

菜单路径 : **Position > Set Position (Signed Distance Field)**

**Set Position (Signed Distance Field)** 块根据输入的 Signed Distance Field （SDF） 计算位置，并根据合成将结果存储在[position 属性](Reference-Attributes.md)中。

此块可以从 SDF 的  **Surface**、 **Volume** 或 **Thick Surface** 计算位置，其中厚度可以相对于形状的大小，也可以是绝对值。


此 Block 还根据计算出的形状位置计算方向向量，并根据合成将其存储到[direction 属性](Reference-Attributes.md)中。该方向等于计算的粒子位置处的表面法线。

注: [Direction 和 Speed 的 Velocity](Block-VelocityFromDirectionAndSpeed.md)  可以处理方向属性。

![](Images/Block-SetPosition(SDF)Example.gif)

## 块兼容性

此 Block 与以下上下文兼容：

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)
- 任何输出上下文

## 块设置

| **Setting**          | **类型** | **描述**                                              |
| -------------------- | -------- | ------------------------------------------------------------ |
| **Position Mode**    | Enum     | 指定此 Block 如何使用形状计算位置。选项包括：<br> &#8226; **Surface**: 仅计算形状表面上的位置。<br> &#8226; **Volume**: 计算整个形状体积内的位置。<br> &#8226; **Thickness Absolute**: 计算给定绝对厚度的厚表面上的位置。<br> &#8226; **Thickness Relative** 将厚表面上的位置计算为最大轴大小的给定百分比。 |
| **Spawn Mode**       | Enum     | 指定此 Block 如何在形状的弧度之间分配粒子。<br/>&#8226; **Random**: 计算弧上每粒子的随机进度 （0..1）。<br/>&#8226; **Custom**: 允许您在 **Arc Sequencer** 属性端口中指定进度。 |
| **Kill Outliers**    | Bool     | (**检查器**) 指示是否终止其位置不粘附在曲面/体积上的粒子。 |
| **Projection Steps** | uint     | (**检查器**) 此 Block 用于将粒子投影到 SDF 表面的步数。这可能会影响性能，但可以产生较少的异常值。 |

## 块属性

| **Input**         | **类型**               | **描述**                                              |
| ----------------- | ---------------------- | ------------------------------------------------------------ |
| **Box**           | [AABox](Type-AABox.md) | Axis-Aligned Box t用于确定要从中计算位置的形状。|
| **Thickness**     | float                  | 用于位置计算的形状表面的厚度。<br/>仅当您将 **Position Mode** 设置为 **Thickness Relative** 或 **Thickness Absolute** 时，才会显示此属性。 |
| **Arc Sequencer** | float                  | 在弧中生成粒子的位置。<br/>仅当将 **Spawn Mode** 设置为 **Custom** 时，才会显示此属性。 |

## 注意

此块根据 SDF 的最大轴计算相对厚度，该轴不一定是 SDF 所表示的对象的大小。因此，即使相对厚度小于 1，此 Block 计算的位置也可以位于整个形状的体积内。
