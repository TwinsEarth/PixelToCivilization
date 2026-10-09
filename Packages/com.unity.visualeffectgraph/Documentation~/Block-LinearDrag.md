# Linear Drag

菜单路径：**Force >** **Linear Drag**

**Linear Drag** 代码块对粒子施加力，使它们减慢速度而不影响它们的方向。

## 代码块设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **UseParticleSize** | bool | 控制是否越大的粒子受到的阻力越大的开关。启用后，粒子受到的阻力大小取决于粒子的大小。禁用时，所有粒子都会受到相同的阻力。 |

## 代码块兼容性

此代码块兼容于以下上下文：

+   [Update](Context-Update.md)

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Drag Coefficient** | float | 阻力系数。 |

## 备注

要使此代码块影响粒子位置，请启用 [Update Context](Context-Update.md) 粒子中的 **Update Position**。
