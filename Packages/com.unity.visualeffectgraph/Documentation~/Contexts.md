# Context 上下文

上下文是 Visual Effect Graph **处理**（垂直）工作流程的主要元素，用于确定粒子的生成和模拟方式。在graph上组织 Context 的方式定义了处理工作流的操作顺序。有关处理工作流程的信息，请参阅[Visual Effect Graph 逻辑](GraphLogicAndPhilosophy.md)。每个 Context 都定义了一个计算阶段。例如，Context 可以：

* 计算效果生成的粒子数。
* 创建新粒子。
* 更新所有活动粒子。

上下文按顺序相互连接以定义粒子的生命周期。在图表创建新粒子后，**Initialize** Context 可以连接到 **Update Particle** Context 来模拟每个粒子。此外，**Initialize** Context 可以直接连接到 **Output Particle** Context 来渲染粒子，而无需模拟任何行为。


## 创建和连接上下文

Context 是一种[graph 元素](GraphLogicAndPhilosophy.md#graph-elements)，因此要创建一个graph元素，请参阅[新增 graph 元素](VisualEffectGraphWindow.md#adding-graph-elements)。

上下文以垂直的线性顺序相互连接。为了实现这一点，他们使用了flow slots。根据 Context 定义的粒子生命周期的哪一部分，它可能在其顶部和/或底部有flow slots。

## 配置上下文

要更改上下文的行为，请在 Node UI 或 Inspector 中调整其[设置](GraphLogicAndPhilosophy.md#settings)。

某些设置还会更改 Context 的外观。例如，在**Quad Output**上下文中，如果将 UV Mode 设置为 **FlipbookMotionBlend**，则 Unity 会将以下额外属性添加到上下文标头：**Flipbook Size**, **Motion Vector Map**, 和 **Motion Vector Scale**。

## Flow 兼容性

并非所有 Context 都可以相互连接。为了保持一致的工作流程，以下规则适用：

* 上下文仅连接到兼容的输入/输出数据类型。
* [Events](Events.md) 可以连接到一个或多个事件或**初始化**上下文。
* **初始化**上下文可以有一个或多个 **SpawnEvent** 源，也可以有一个或多个 **GPUSpawnEvent** 源，但这些数据类型是互斥的。
* 只有一个 **Initialize** Context 可以连接到一个 **Update** Context。
* 您可以将 **Output** Context 连接到 **Initialize** 或 **Update** Context。

有关 Context 兼容性的细分，请参阅下表。

| 上下文            | 输入数据类型                      | 输出数据类型 | 具体说明                                            |
| ---------------------- | --------------------------------------------- | ------------------- | ------------------------------------------------------------ |
| **Event**              | **无**                                      | **SpawnEvent** (1+) | **无**                                                     |
| **Spawn**              | **SpawnEvent** (1+)                           | **SpawnEvent** (1+) | 有两个输入流插槽，分别用于启动和停止 **Spawn Context**。 |
| **GPU Event**          | **无**                                      | **SpawnEvent**      | 用于**初始化**上下文的输出。                            |
| **Output Event** | **SpawnEvent (1+)** |  | 将 CPU SpawnEvent 输出回 Visual Effect 组件。 |
| **Initialize**         | **SpawnEvent** (1+) 或 **GPUSpawnEvent** (1+) | **Particle** (1)    | 输入类型为 **SpawnEvent** 或 **GPUSpawnEvent**。这些输入类型是互斥的。<br/>可以输出到 **Particle Update** 或 **Particle Output**。 |
| **Update**             | **Particle** (1)                              | **Particle** (1+)   | 可以输出到 **Particle Update** 或 **Particle Output**。  |
| **Particle Output**    | **Particle** (1)                              | **无**            | 可以具有来自 **Initialize** 或 **Update** Context 的输入。<br/>无输出。 |
| **Static Mesh Output** | **无**                                      | **无**            | 独立上下文。                                         |

# 上下文类型概述

本节涵盖了每种 Context 的所有常用设置。

## 事件

Event Contexts 仅显示其名称，即字符串。要触发事件上下文并从中激活工作流，请在[组件 API](ComponentApi.md) 中使用事件上下文的名称。有关如何执行此操作的信息，请参阅[发送事件](ComponentApi.md#sending-events)。

## Spawn

生成上下文是独立的系统，具有三种状态：Running （运行中）、Idle （空闲） 和 Waiting （等待）。

* **Looping** (运行中): 此状态意味着 Unity 在 Context 中计算 Block 并生成新粒子。
* **Finished** (空闲): 此状态表示生成机器处于关闭状态，并且不会在 Context 中计算 Blocks 或生成粒子。
* **DelayingBeforeLoop/DelayingAfterLoop** (等待): 此状态在您可以指定的延迟时间持续时间内暂停 Context。延迟后，Context 恢复，在 Context 中计算 Blocks，并生成粒子。

要自定义 **Spawn** 上下文，您可以向其添加兼容的 **Block**。有关 Spawn Context API 的信息，请参阅[脚本参考](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXSpawnerLoopState.html)。


### 启用和禁用

Spawn Contexts 公开两个 [flow slots](GraphLogicAndPhilosophy.md#processing-workflow-vertical-logic): **Start** 和 **Stop**:

-  **Start** 输入重置/启动 Spawn 上下文。如果您没有将任何内容连接到此流槽，它将隐式使用 **OnPlay** [事件](Events.md)。多次使用 **Start** 与使用 **Start** 效果相同。
- **Stop** 输入停止 Spawn 系统。如果您没有将任何内容连接到此流槽，它将隐式使用 **OnStop**  [事件](Events.md)。

### 循环和延迟

每个 Spawn Context 都包含一个状态，用于确定 Context 何时生成粒子。

* Spawn Context （生成上下文） 在特定持续时间的循环期间发射粒子。这意味着每个循环开始时，内部生成时间会重置。默认情况下，持续时间是**无限**的，但您可以更改此设置。<br/>要设置 loop 模式：
  1. 在图表中选择 Spawn Context （生成上下文）。
  2.在 Inspector 中，单击 **Loop Duration** 下拉列表。
  3. 从列表中，单击 **Infinite、Constant** 或 **Random**。
* Spawn Contexts 可以执行一个、多个或无限数量的循环。<br/>要设置循环数：
  1. 在图表中选择 Spawn Context （生成上下文）。
  2. 在 **Inspector** 中，单击 Loop 下拉列表。
  3. 从列表中，单击 **Infinite、Constant** 或 **Random**。
* Spawn Contexts 可以在每个循环之前和之后执行延迟。在延迟期间，生成时间正常经过，但 Spawn Context 不会生成任何粒子。<br/>要设置延迟持续时间：
  1. 在图表中选择 Spawn Context （生成上下文）。
  2. 在 Inspector 中，单击 **Delay Before Loop** 或 **Delay After Loop** 下拉列表。
  3. 从列表中，单击 **None、Constant** 或 **Random**。

如果将 **Loop Duration、Loop、Delay Before Loop** 或 **Delay After Loop** 设置为 **Constant** 或 **Random**，则 Spawn Context 会在其标头中显示额外的属性以控制每个行为。为了评估您设置的值，Unity 使用以下规则：

- 如果设置，Unity 会在触发 Context 的 **Start** flow 输入时计算 **Loop Coun**t。
- 如果设置，则 Unity 会在每次循环开始时评估 **Loop Duration**。
- 如果设置，则 Unity 会在每次延迟开始时评估 **Loop Before/After Delay**。

有关 Looping 和 delay 系统的可视化效果，请参见下图：

![Figure explaining the Loop/Delay System](Images/LoopDelaySystem.png)

## GPU 事件

GPU 事件上下文是实验性上下文，用于连接输入以从其他系统输出 GPU 事件。它们与常规的 Event Context 在两个方面有所不同：

* GPU 计算 GPU 事件，CPU 计算普通事件。
* 您无法使用 Blocks 自定义 GPU 事件上下文。

**注意**: 将 Spawn 事件连接到 Initialize 上下文时，请注意 GPU Spawn Events 和普通 Spawn Events 是互斥的。您只能同时将一种类型的 Spawn Event 连接到 **Initialize** Context。

## 初始化

Initialize Contexts 根据 **SpawnEvent** 数据生成新粒子，Unity 根据 Event、Spawn Context 或 GPU Event Context 计算这些数据。

例如：如果 Spawn Context 声明效果应创建 200 个新粒子，则 Initialize Context 会处理所有 200 个新粒子的 Block。

要自定义 **Initialize** Contexts，您可以向它们添加兼容的 **Block。**

Initialize Contexts 是新系统的入口点。因此，它们在其标头中显示以下信息和配置详细信息：



| 属性/设置   | 描述                                 |
| ---------------------- | -------------------------------------------- |
| **Bounds** (属性)  |	控制系统的 边界框。    |
| **Capacity** (设置) | 控制系统的分配计数。 |

## 更新

Update Contexts 根据**粒子**数据更新系统中的所有活动粒子，Unity 根据 Initialize 和 Update Contexts 计算粒子数据。Unity 执行 Update Contexts，从而更新每个粒子、每个帧。

粒子更新上下文还会自动处理粒子的一些计算，以简化常见的编辑任务。

要自定义 **Update** Contexts，您可以向其添加兼容的 **Blocks。**


| 设置             | 描述                                                  |
| ----------------------- | ------------------------------------------------------------ |
| **Update Position** | 指定 Unity 是否对粒子应用速度积分。启用后，Unity 会在每帧中将简单的 Euler 速度积分应用于每个粒子的位置。禁用后，Unity 不会应用任何速度积分。 |
| **Update Rotation** | 指定 Unity 是否对粒子应用角度积分。启用后，Unity 将简单的 Euler 积分应用于每个粒子每帧的旋转。禁用后，Unity 不会应用任何 angular 集成。 |
| **Age Particles**       | 如果上下文使用 Age 属性，则控制 Update Context 是否使粒子随时间老化。 |
| **Reap Particles**      | 如果上下文使用 Age 和 Lifetime 属性，则这将控制当粒子的年龄大于其生命周期时，Update Context 是否删除粒子。 |


## 输出

Output Contexts （输出上下文） 渲染系统中的粒子。它们使用不同的模式和设置渲染粒子，具体取决于同一系统中 **Initialize** 和 **Update** Contexts 中的粒子数据。然后，它将配置呈现为特定的基元形状。

要自定义 **Output** Contexts，您可以向它们添加兼容的 **Blocks**。
