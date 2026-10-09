# Kill (Sphere)

菜单路径：**Kill >** **Kill (Sphere)**

Kill (Sphere) 代码块根据粒子与给定 [Sphere](Type-Sphere.md) 的比较关系杀死粒子（将`alive`属性设置为 false）。

## 代码块设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Mode** | Enum | Unity 用于确定是否杀死粒子的方法。选项：  
• **Solid**：杀死球体内的粒子。  
• **Inverted**：杀死球体外的粒子。 |

## 代码块兼容性

此代码块兼容于以下上下文：

+    [Initialize](Context-Initialize.md)
+    [Update](Context-Update.md)
+    任何输出上下文

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Sphere** | [Sphere](Type-Sphere.md) | 要与粒子位置进行比较的球体。 |

## 备注

+   如果您在输出上下文中使用此代码块，粒子会暂时消失，但不会保持在被杀死的状态。
+   确保在更新中启用 **Reap Particles** 选项，否则被杀死的粒子不会消失。
