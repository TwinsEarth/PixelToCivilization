# Trigger Event Rate

菜单路径：**GPU Event > Trigger Event Rate**

**Trigger Event Rate** 代码块使用指定的速率通过 GPU 事件触发粒子的创建。您可以设置随时间（每秒）或随距离（父粒子距离变化）生成粒子的速率。

![](Images/Block-TriggerEventRateExample.gif)


触发器代码块总是在 [Update Context](Context-Update.md) 结束时执行，无论代码块位于 Context 中的何处。

## 代码块兼容性

此代码块兼容于以下上下文：

+   [Update](Context-Update.md)

## 代码块设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Mode** | Enum | 此代码块用于应用 **Rate** 的方法。选项：  
• **Over Time**：随时间应用 **Rate**。  
• **Over Distance**：随距离应用 **Rate**。 |

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Rate** | Float | 基于 **Mode** 生成的 GPU 事件粒子的数量。  
如果将 **Mode** 设置为 **Over Time**，则为每秒生成的 GPU 事件粒子数。  
如果将 **Mode** 设置为 **Over Distance**，则为随着父粒子的移动而生成的 GPU 事件粒子数。 |

| **Output** | **类型** | **描述** |
| --- | --- | --- |
| **Evt** | [GPU 事件](Context-GPUEvent.md) | 要触发的 GPU 事件。 |
