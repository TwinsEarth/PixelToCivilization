# Collide with AABox

菜单路径：**Collision > Collide with AABox**

**Collide with AABox** 代码块定义一个与粒子碰撞的轴对齐盒体。

![](Images/Block-CollideWithAABoxMain.png)

## 代码块兼容性

此代码块兼容于以下上下文：

+   [Update](Context-Update.md)

## 代码块设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Mode** | Enum | 碰撞形状模式。选项： <br/>• **Solid**：粒子不能进入碰撞体。  <br/>• **Inverted**：粒子不能离开碰撞体。碰撞体成为粒子无法退出的体积。 |
| **Radius Mode** | Enum | 决定每个粒子碰撞半径的模式。选项：  <br/>• **None**：粒子的半径为零。  <br/>• **From Size**：粒子从它们各自的大小继承半径。  <br/>• **Custom**：允许您将粒子的半径设置为特定值。 |
| **Rough Surface** | Bool | 切换碰撞体是否模拟粗糙表面。启用后，Unity 会向粒子反弹的方向添加随机性，以模拟与粗糙表面的碰撞。 |

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Box** | [AABox](Type-AABox.md) | 指定碰撞体积的中心位置和大小的轴对齐盒体。 |
| **Bounce** | Float | 碰撞后应用于粒子的反弹量。值为 0 表示粒子不会反弹。值为 1 表示粒子以与它们撞击的相同速度弹开。 |
| **Friction** | Float | 粒子在碰撞过程中损失的速度。最小值为 0。 |
| **Lifetime Loss** | Float | 粒子在碰撞后失去的生命比例。 |
| **Roughness** | Float | 粒子与表面碰撞后随机调整方向的量。  <br/>此属性仅在启用 **Rough Surface** 时显示。 |
| **Radius** | Float | 此代码块用于碰撞检测的粒子半径。  <br/>此属性仅在将 **Radius Mode** 设置为 **Custom** 时显示。 |
