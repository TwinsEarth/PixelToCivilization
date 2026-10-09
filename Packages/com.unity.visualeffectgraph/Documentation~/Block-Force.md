# Force

菜单路径：**Force > Force**

**Force** 代码块将给定的力应用于粒子。为此，它会改变受影响粒子的速度。

## 代码块兼容性

此代码块兼容于以下上下文：

+   [Update](Context-Update.md)
## 代码块设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Mode** | Enum | 此代码块用于向粒子施加力的方法。选项：  <br/>• **Absolute**：直接对粒子施加力。  <br/>• **Relative**：以某种方式施加力，使粒子的速度趋向于 **Velocity** 值。此操作的速度取决于粒子的速度和目标 **Velocity** 之差。**Drag** 值越高，粒子质量越低，此转换的速度越快。此选项适用于模拟粒子顺随但不会超过目标速度的流动（如风）。 |

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Force** | [Vector](Type-Vector.md) | 此代码块施加于粒子的力矢量。  <br/>此属性仅在将 **Mode** 设置为 **Absolute** 时显示。 |
| **Velocity** | [Vector](Type-Vector.md) | 受影响的粒子的趋向相对速度。  <br/>此属性仅在将 **Mode** 设置为 **Relative** 时显示。 |
| **Drag** | float | 阻力系数。  <br/>仅当 **Mode** 设置为 **Relative** 时，才显示此属性。 |

## 备注

仅当在 [Update Context](Context-Update.md) 中启用了 **Update Position** 设置时，此代码块才会影响粒子位置。
