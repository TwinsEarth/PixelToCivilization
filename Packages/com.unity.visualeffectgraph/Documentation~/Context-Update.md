# 更新粒子

菜单路径 : **Context > Update Particle**

Update [上下文](https://docs.unity.cn/cn/Packages-cn/com.unity.visualeffectgraph@latest/index.html?subfolder=/manual/Contexts.html) 处理给定系统的 [已初始化](Context-Initialize.md) 的粒子或粒子条。


Visual Effect Graph 根据场景中效果的剔除状态和 Visual Effect Graph 资源中指定的剔除标志，在每帧中执行此上下文。每个 Update 上下文执行它包含的代码块，并且可以根据特定条件处理附加的隐式行为。有关隐式行为的信息，请参阅 [详情部分](#详情).

## 上下文设置

| **Setting**         | **Type** | **Description**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| **Space**           | Enum     | (**检查器**)此系统的模拟空间。选项：<br/>&#8226; **Local**: 系统在本地空间进行模拟。<br/>&#8226; **World**: 系统在世界空间进行模拟。 |
| **Update Position** | Bool     | (**检查器**) 指定是否根据粒子速度更新粒子位置。|
| **Update Rotation** | Bool     | (**检查器**) 指定是否对粒子旋转应用隐式角速度。 |
| **Age Particles**   | Bool     | (**检查器**) 指定是否自动老化粒子。 |
| **Reap Particles**  | Bool     | (**检查器**) 指定是否自动收获粒子。启用后，如果粒子的年龄超过其生命周期，则此上下文会将其从模拟中移除。 |

## Flow

| **端口**   | **描述**                                              |
| ---------- | ------------------------------------------------------------ |
| **输入**  | [初始化](Context-Initialize.md) 上下文的连接。 |
| **输出** | 到 Update (Single) 或 Output (Single/Multiple) 上下文的连接。 |

## 详情

### 隐式行为

根据系统中存在的属性，此上下文隐式执行额外的行为：

- **Velocity Integration**: 如果系统中有 Velocity 属性，则此上下文使用以下方程执行欧拉速度积分：`position += velocity * deltaTime`。 这会根据粒子的速度移动粒子。在执行速度积分之前，此上下文将位置属性备份到 oldPosition 属性中。

- **Angular Velocity Integration**:如果系统中有 AngularVelocity 属性，则此上下文使用以下方程执行欧拉角速度积分： `angle.xyz += angularVelocity.xyz * deltaTime`。这会根据粒子的角速度旋转粒子。

- **Aging**: 如果系统中有 Age 属性，则此上下文按照以下方程执行粒子的自动老化：`age += deltaTime`

- **Reaping**: 如果系统中存在 bot Age 和 Lifetime 属性，此上下文将使用以下方程，在粒子的生命周期超过其年龄时杀死粒子（将其 alive 属性设置为 false）：`alive = (age <= lifetime)`

默认情况下，所有隐式行为都是启用的，并且可以在上下文检查器中禁用

所有隐式行为都在执行所有 Update 上下文代码块之后发生。

### 更新时间

Visual Effect Graph 根据在 Visual Effect Graph 资源上设置的[更新模式](https://docs.unity.cn/cn/Packages-cn/com.unity.visualeffectgraph@latest/index.html?subfolder=/manual/VisualEffectGraphAsset.html%23creating-visual-effect-graph-assets)，在每帧中执行此上下文：

- 在 **Delta Time** 模式下，更新使用帧的增量时间并且每帧发生一次。在此模式下，增量时间是可变的，帧速率的变化会显着影响模拟。

- 在 **Fixed Delta Time** 模式下，如果帧尚不需要更新，则更新使用固定增量时间值或零增量时间值。在此模式下，模拟以固定速率发生，这对模拟质量的影响较小。但是，在此模式下，deltaTime 有时可能等于 0，因此在 0 deltaTime 更新后，oldPosition 可能等于帧中的某个位置。
