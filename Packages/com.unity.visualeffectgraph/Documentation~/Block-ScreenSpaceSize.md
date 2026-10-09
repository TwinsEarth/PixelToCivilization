# Screen Space Size

菜单路径：**Output > Screen Space Size**

**Screen Space Size** 代码块计算每个粒子的 scaleXYZ 属性，以达到相对于像素大小或屏幕大小的尺寸。

## 代码块兼容性

此代码块兼容于以下上下文：

+   任何输出上下文

## 代码块设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Size Mode** | Enum | 指定如何调整粒子大小。这可以是绝对像素值，也可以是相对于分辨率的大小。选项：  <br/>• **Pixel Absolute**：使用绝对像素宽度的大小。  <br/>• **Pixel Relative To Resolution**：使用绝对像素宽度的大小（假设屏幕处于目标分辨率）。在目标分辨率下，这相当于 **Pixel Absolute**。  <br/>• **Ratio Relative To Width**：使用相对于渲染目标当前宽度的大小，值为 0.1 使 ScaleXY 代表屏幕宽度的 10%。  <br/>• **Ratio Relative To Height**：使用相对于渲染目标当前高度的大小，值为 0.1 使 ScaleXY 代表屏幕高度的 10%。  <br/>• **Ratio Relative To Height And Width**：使用相对于渲染目标当前高度和宽度的大小，值为 0.1 使 ScaleX 表示屏幕宽度的 10%，ScaleY 表示屏幕高度的 10%。因此，ScaleXY 与屏幕具有相同的比例。 |
| **Size Z Mode** | Enum | **（检查器）** 指定如果系统使用 ScaleZ 属性，如何计算粒子的 z 轴比例。选项：  <br/>• **Ignore**：不修改粒子的 ScaleZ。  <br/>• **Same As Size X**：使用粒子的 ScaleX 值作为其 ScaleZ。  <br/>• **Same As Size Y**：使用粒子的 ScaleY 值作为其 ScaleZ。  <br/>• **Min Of Size XY**：使用粒子的 ScaleX 和 ScaleY 之间的最小值作为其 ScaleZ。 <br/>• **Max Of Size XY**：使用粒子的 ScaleX 和 ScaleY 之间的最大值作为其 ScaleZ。  <br/>• **Average Of Size XY**：使用粒子的 ScaleX 和 ScaleY 的平均值作为其 ScaleZ。 |

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Pixel Size** | float | 粒子的大小（以像素为单位）。  <br/>此属性仅在将 **Size Mode** 设置为 **Pixel Absolute** 或 **Pixel Relative To Resolution** 时显示。 |
| **Relative Size** | float | 粒子相对于所选尺寸模式的比率。值 **1.0** 表示粒子调整大小以匹配指定的屏幕尺寸。  <br/>此属性仅在将 **Size Mode** 设置为 **Ratio Relative To Height**、**Ratio Relative To Width** 或 **Ratio Relative To Height And Width** 时显示。 |
| **Reference Resolution** | Vector2 | 设置粒子像素大小时所参照的屏幕分辨率。 <br/>此属性仅在将 **Size Mode** 设置为 **Pixel Relative To Resolution** 时显示。 |
