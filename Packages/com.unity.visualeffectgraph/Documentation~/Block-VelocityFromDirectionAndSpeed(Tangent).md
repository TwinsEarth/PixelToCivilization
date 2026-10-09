# Velocity from Direction & Speed (Tangent)

菜单路径 : **Velocity > [Set/Add] Velocity from Direction & Speed (Tangent)**

**Velocity from Direction And Speed (Tangent)** 块根据 direction 属性和切线向量之间的混合比计算粒子的速度。

切线向量基于粒子的当前方向和给定的轴。矢量垂直于此轴。

然后，Block 按速度缩放最终方向向量，并使用 velocity 属性对其进行组合。

![](Images/Block-VelocityFromDirectionAndSpeed(Tangent)Example.gif)

## 块兼容性

此 Block 与以下上下文兼容：

- [初始化](Context-Initialize.md)
- [更新](Context-Update.md)

## 块设置

| **设置**     | **类型** | **描述**                                              |
| --------------- | -------- | ------------------------------------------------------------ |
| **Composition** | Enum     | **(检查器)** 指定此 Block 如何组成 velocity 属性。选项包括：<br/>&#8226; **Set**: 用新值覆盖 velocity 属性。<br/>&#8226; **Add**: 将新值添加到 velocity 属性值。<br/>&#8226; **Multiply**: 将速度属性值乘以新值。<br/>&#8226; **Blend**: 在速度属性值和新值之间进行插值。您可以指定介于 0 和 1 之间的混合因子。 |
| **Speed Mode**  | Enum     | 指定如何计算速度。选项包括：<br/>&#8226; **Constant**: 应用您在  **Speed** 属性中设置的恒定速度。<br/>&#8226; **Random**: 在 **Min Speed** 和 **Max** **Speed** 之间应用随机速度。 |

## 块属性

| **输入**           | **类型** | **描述**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| **Axis**            | [Line](Type-Line.md) | 此 Block 用于计算切线向量的线。 |
| **Speed**           | float    | 应用于方向向量以计算速度的速度乘数。<br/>仅当将 **Speed Mode** 设置为 **Constant** 时，才会显示此属性。 |
| **Min Speed**       | float    | 应用于方向向量以计算速度的最小速度乘数。<br/>仅当将 **Speed Mode** 设置为 **Random** 时，才会显示此属性。 |
| **Max Speed**       | float    | 为了计算速度而应用于方向向量的最大速度乘数。<br/>仅当将 **Speed Mode** 设置为 **Random** 时，才会显示此属性。 |
| **Blend Direction** | float    | 当前方向属性值与新计算的方向值之间的混合百分比。 |
| **Blend Velocity**  | float    | 当前方向属性值与新计算的方向值之间的混合百分比。<br/>仅当将 **Composition** 设置为 **Blend** 时，才会显示此属性。 |
