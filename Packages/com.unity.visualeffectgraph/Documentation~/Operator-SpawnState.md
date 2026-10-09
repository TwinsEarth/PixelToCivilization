# Spawn State

菜单路径：**Operator > Spawn > Spawn Context State**

**Spawn State** 运算符包含有关 [Spawn](Context-Spawn.md) 系统[状态](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerState.html)的信息。它包含以下信息：当前帧中生成的粒子数、生成循环的持续时间和当前增量时间。

您只能将此运算符的输出连接到 [Spawn 上下文](Context-Spawn.md) 中的代码块。如果将输出连接到其他上下文类型的代码块，Unity 将引发异常。

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **NewLoop** | Boolean | 指示新的生成循环是否已开始此帧。如果新循环已经开始，则此运算符输出`true`。否则返回`false`。 |
| **LoopState** | Uint | 生成循环的当前状态。每个值代表一个不同的状态。这些值包括：<br/>&#8226;**0**：不循环。 <br/>&#8226;**1**：在循环之前延迟。 <br/>&#8226;**2**：正在循环。<br/>&#8226;**3**：循环后延迟。  <br/>有关生成循环状态的更多信息，请参见 [VFXSpawnerLoopState](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXSpawnerLoopState.html)。 |
| **LoopIndex** | int | 循环的当前索引。每次新的生成循环开始时，Unity 都会递增这个数字。 |
| **SpawnCount** | float | 系统在当前帧中生成的粒子数。 |
| **SpawnDeltaTime** | float | 当前帧的增量时间。您可以使用自定义生成器修改此值。 |
| **SpawnTotalTime** | float | 自应用程序启动以来经过的总时间。 |
| **LoopDuration** | float | 连接系统的 [Spawn 上下文](Context-Spawn.md)中指定的循环持续时间。 |
| **LoopCount** | int | 连接系统的 [Spawn 上下文](Context-Spawn.md)中指定的循环次数。 |
| **DelayBeforeLoop** | float | VFXSpawner 在开始新循环之前等待的时间。连接系统的 [Spawn 上下文](Context-Spawn.md)中指定的值。 |
| **DelayAfterLoop** | float | VFXSpawner 完成循环后等待的时间。连接系统的 [Spawn 上下文](Context-Spawn.md)中指定的值。 |
