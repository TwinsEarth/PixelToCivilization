# Trigger Event Always

菜单路径：**GPU Event > Trigger Event Always**

**Trigger Event Always** 代码块通过 [GPU 事件](Context-GPUEvent.md)，触发每帧持续创建指定数量的粒子。触发器代码块总是在更新结束时执行，无论代码块位于 [blackboard](Blackboard.md) 上的何处。

您还可以在各种条件下使用 Trigger 代码块来创建更复杂的生成行为。例如：

![](Images/Block-TriggerEventAlwaysExample.png)

## 代码块兼容性

此代码块兼容于以下上下文：

+   [Update](Context-Update.md)

## 代码块属性

| **Input** | **类型** | **描述** |
| --- | --- | --- |
| **Count** | Uint | 每帧生成的 GPU 事件粒子数。 |

| **Output** | **类型** | **描述** |
| --- | --- | --- |
| **Evt** | [GPU 事件](Context-GPUEvent.md) | 要触发的 GPU 事件。 |
