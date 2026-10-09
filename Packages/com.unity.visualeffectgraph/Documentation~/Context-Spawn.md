# Spawn

菜单路径 : **Context > Spawn**

Spawn 上下文控制系统向 [初始化](Context-Initialze.md) 上下文供给多少个实例。此上下文处理一组内置代码块以控制生成速率。您还可以实施 [VFXSpawnerCallbacks](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerCallbacks.html) 以创建自定义行为。

此上下文在生成循环内生成粒子。您可以指定它处理的循环数、每个循环的持续时间以及在每个循环之前或之后是否有延迟。



## 上下文设置

| **设置**             | **类型** | **描述**                                              |
| ----------------------- | -------- | ------------------------------------------------------------ |
| **Loop Duration**       | Enum     | **(检查器)** 指定此上下文生成粒子的时长。选项：<br/>&#8226; **Infinite**: 连续生成粒子。<br/>&#8226; **Constant**:在您可以指定的特定持续时间内生成粒子。在这段时间之后，如果 **Delay Mode** 设置为 **After Loop** 或 **Before And After Loop**，此上下文会暂停粒子生成。暂停后执行的操作取决于 **Loop Count** 和 **Delay Mode**。<br/>&#8226; **Random**: 在最小值和最大值之间的随机持续时间内生成粒子。在这段时间之后，如果 **Delay Mode** 设置为 **After Loop** 或 **Before And After Loop**，此上下文会暂停粒子生成。暂停后执行的操作取决于 **Loop Count** 和 **Delay Mode**。 |
| **Loop Count**          | Enum     | **(检查器)** 指定此上下文循环的次数。在每个循环中，此上下文都会触发粒子生成。选项:<br/>&#8226; **Infinite**: 无限循环。<br/>&#8226; **Constant**: 循环特定次数。<br/>&#8226; **Random**: 在最小值和最大值之间循环随机次数。 |
| **Delay Mode**          | Enum     | **(检查器)** 指定此上下文在循环中处理延迟的位置。选项：<br/>&#8226; **None**: 不处理延迟。<br/>&#8226; **Before Loop**: 在生成循环之前处理延迟。<br/>&#8226; **After Loop**: 在生成循环后处理延迟。<br/>&#8226; **Before And After Loop**: 在生成循环之前和之后处理延迟。 |
| **Delay Before Random** | Bool     | **(检查器)** 切换生成循环之前的延迟是最小值和最大值之间的随机值还是常数值。<br/>此设置仅在将 **Delay Mode** 设置为 **Before Loop** 或 **Before And After Loop** 时显示。 |
| **Delay After Random**  | Bool     | **(检查器)** 切换生成循环之后的延迟是最小值和最大值之间的随机值还是常数值。<br/>此设置仅在将 **Delay Mode** 设置为 **After Loop** 或 **Before And After Loop** 时显示。 |

## 上下文属性

| **Setting**       | **Type**      | **Description**                                              |
| ----------------- | ------------- | ------------------------------------------------------------ |
| **Loop Duration** | float/Vector2 | 生成循环的持续时间。<br/>此属性仅在将 **Loop Duration** 设置为 **Constant** 或 **Random** 时显示。 |
| **Loop Count**    | int/Vector2   | 定义在返回初始状态之前上下文处理的循环次数。<br/>此属性仅在将 **Loop Count** 设置为 **Constant** 或 **Random** 时显示。 |
| **Delay Before**  | float/Vector2 | 生成循环之前的延迟持续时间。<br/>此属性仅在启用 **Delay Before Random** 时显示。 |
| **Delay After**   | float/Vector2 | 生成循环之后的延迟持续时间。<br/>此属性仅在启用 **Delay After Random** 时显示。 |

## Flow

| **端口**       | **描述**                                              |
| -------------- | ------------------------------------------------------------ |
| **Start**      | 来自[Event](Context-Event.md), [GPU Event](Context-GPUEvent.md)或另一个 Spawn 上下文的连接。 |
| **Stop**       | 来自[Event](Context-Event.md), [GPU Event](Context-GPUEvent.md)或另一个 Spawn 上下文的连接 |
| **SpawnEvent** | 到 [初始化上下文](Context-Initialize.md) 或另一个 Spawn 上下文的连接。 |

## 备注

### 系统生命周期

有关循环和延迟系统的可视化，请参见下图。

![](Images/Context-SpawnVisualization.png)

循环阶段的生命周期分为 [状态](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerLoopState.html)，由 Spawn 上下文在内部处理。生命周期如下所示：

| **步骤**                     | **状态描述**                                        |
| ---------------------------- | ------------------------------------------------------------ |
| 1. 初始状态             | Spawn 上下文的初始状态是 [VFXSpawnerLoopState.Finished](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXSpawnerLoopState.Finished.html)。在此阶段，上下文正在等待 **Start** 流程输入的触发。触发发生在您调用 [VisualEffect.Play](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VisualEffect.Play.html)的时候。 |
| 2. OnPlay                    | 当 Start 流程输入触发时，如果存在 [自定义生成器回调], 上下文将触发 [VFXSpawnerCallbacks.OnPlay](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXSpawnerCallbacks.OnPlay.html)。TSpawn 上下文评估图形以设置 [循环次数](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerState-loopCount.html)。然后进行下一步操作。 |
| 3. 重置延迟持续时间    | Spawn 上下文评估图形并初始化：<br/>&#8226; [delayBeforeLoop](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerState-delayBeforeLoop.html) 时间。<br/>&#8226; [loopDuration](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerState-loopDuration.html) 时间。<br/>&#8226; [delayAfterLoop](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerState-delayAfterLoop.html) 时间。<br/>然后，它将 [totalTime](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerState-totalTime.html) 设置为 0 并进行下一步。 |
| 4. 在循环之前等待 | [loopState](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXSpawnerState-loopState.html) 过渡到[DelayingBeforeLoop](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXSpawnerLoopState.DelayingBeforeLoop.html). <br/>在此阶段，在[totalTime](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerState-totalTime.html) is below [delayBeforeLoop](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerState-delayBeforeLoop.html)期间，上下文一直等待。 <br/>当 totalTime 超过 delayBeforeLoop 时，上下文将 totalTime 设置为零并继续下一步。 |
| 5. 循环                   | [loopState](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXSpawnerState-loopState.html) 过渡到 [Looping](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXSpawnerLoopState.Looping.html) ，上下文将 [playing](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXSpawnerState-playing.html) 设置为 true。<br/>在此阶段，在 [totalTime](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerState-totalTime.html) 不足 [loopDuration](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerState-loopDuration.html)期间，Spawn 上下文将评估内部生成代码块并生成粒子。<br/>当 totalTime 超过 loopDuration 时，上下文将 totalTime 设置为零并继续下一步。 |
| 6.  在循环之后等待   | [loopState](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXSpawnerState-loopState.html) 过渡到 [DelayingAfterLoop](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXSpawnerLoopState.DelayingAfterLoop.html). <br/>在此阶段，在[totalTime](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerState-totalTime.html) 不足[delayAfterLoop](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerState-delayAfterLoop.html), 期间，上下文一直等待。 <br/>当 totalTime 超过 delayBeforeLoop 时，上下文将 totalTime 设置为零并继续下一步。 |
| 7. 验证最终状态       | 最后，上下文递增 [loopIndex](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXSpawnerState-loopIndex.html)。如果新的 loopIndex 小于 [loopCount](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerState-loopCount.html), 生命周期移回步骤 3。否则，它返回到初始步骤 1，并等待新的 Start 流程输入触发器。 |

### 链接 Spawn 上下文

打开和关闭 Spawn 上下文的标准方法是使用输入事件。当 Spawn 上下文的 **Start** 输入流程插槽没有输入时，Visual Effect Graph 会隐式连接“OnPlay”事件。同样，当 Spawn 上下文的 **Stop** 输入流程插槽没有输入时，Visual Effect Graph 会隐式连接“OnStop”事件。

打开和关闭 Spawn 上下文的另一种方法是将其链接到另一个 Spawn 上下文。为此，请将 Spawn 上下文的 **SpawnEvent** 输出流量插槽链接到另一个 Spawn 上下文的 **Start** 输入流程插槽。在这种情况下，事件属性状态会自动从第一个 Spawn 上下文转移到第二个。然后，您可以连接到第一个 Spawn 上下文的 **Start** 和 **Stop** 输入流程插槽来打开和关闭连接的上下文。
