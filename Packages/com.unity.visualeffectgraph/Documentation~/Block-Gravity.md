# Gravity

菜单路径：**Force >** **Gravity**

**Gravity** 代码块将给定的力应用于粒子。为此，它会改变受影响粒子的速度。

## 代码块兼容性

此代码块兼容于以下上下文：

+   [Update](Context-Update.md)

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Force** | [Vector](Type-Vector.md) | 此代码块应用的力矢量。默认值是 (0, -9.81, 0)，模拟地球上的重力。 |

## 备注

仅当在 [Update Context](Context-Update.md) 中启用了 **Update Position** 设置时，此代码块才会影响粒子位置。
