# 初始化

菜单路径 : **Context > Initialize Particle**

**初始化**上下文处理一个 [Spawn 事件](Context-Spawn.md) 或 [GPU 事件](Context-GPUEvent)，并为 Particle 或 ParticleStrip 模拟初始化新的元素。

## 上下文设置

| **设置**                  | **类型** | **描述**                                              |
| ---------------------------- | -------- | ------------------------------------------------------------ |
| **Space**                    | Enum     | **(检查器)** 系统的 [模拟空间](https://docs.unity.cn/cn/Packages-cn/com.unity.visualeffectgraph@latest/index.html?subfolder=/manual/Systems.html%23system-simulation-spaces)。 |
| **Data Type**                | Enum     | **(检查器)** 系统中元素的数据类型。选项有：<br/>&#8226; **Particle**: 系统生成粒子。<br/>&#8226; **Particle Strip**: 系统生成粒子条。 |
| **Capacity**                 | UInt     | 模拟中的固定元素数量。此计数可缩放粒子系统的内存分配。 |
| **Particle Per Strip Count** | Uint     | 每个粒子条的固定粒子数。<br/>此设置仅在将 **Data Type** 设置为 **Particle Strip** 时显示。 |
| **Bounds Setting Mode**      | Enum     | **Bounds Setting Mode**: 指定如何设置系统的边界框。选项包括：<br/>&#8226; **Manual**: 使用 **Bounds** input 属性直接设置边界。<br/>&#8226; **Recorded**: 允许您从 Target 视觉效果 GameObject 面板录制系统。<br/>&#8226; **Automatic**: Unity 会自动计算边界。<br/><br/>有关这些模式和视觉效果的一般边界的更多信息，请参阅 [视觉效果边界](visual-effect-bounds.md)。 |

## 上下文输入属性

| **属性**       | **类型**               | **描述**                                              |
| ------------------ | ---------------------- | ------------------------------------------------------------ |
| **Bounds**         | [AABox](Type-AABox.md) | 为系统定义的边界框。此属性根据 [视觉效果资源](https://docs.unity.cn/cn/Packages-cn/com.unity.visualeffectgraph@latest/index.html?subfolder=/manual/VisualEffectGraphAsset.html) 中定义的 **Culling Flags** 属性进行计算。 |
| **Bounds Padding** | Vector3                | 要应用于视觉效果的边界框的其他填充。有关视觉效果边界的更多信息，请参阅 [视觉效果边界](visual-effect-bounds.md)。 |

## Flow

| **Port**   | **描述**                                              |
| ---------- | ------------------------------------------------------------ |
| **Input**  | 来自 [Spawn](Context-Spawn.md)、[GPU Event](Context-GPUEvent.md) 或 [Event](Context-Event.md) 上下文的连接。有关输入流兼容性的更多信息，请参阅 [输入流兼容性](#input-flow-compatibility)。 |
| **Output** | 连接到 [Update](Context-Update.md) (Single) 或 Output (Single/Multiple)  上下文的连接。 |

## 细节

### Overspawn

要创建新元素，您可以将 [Blocks](Blocks.md) 添加到 Context 的主体中。Visual Effect Graph 会将这些块添加到模拟中（如果有剩余内存来创建它们）。执行后，Unity 会丢弃所有无法以这种方式注入的元素。

#### Alive 属性

在 Initialize 上下文中将 [Alive 属性](https://docs.unity.cn/cn/Packages-cn/com.unity.visualeffectgraph@latest/index.html?subfolder=/manual/Reference-Attributes.html%23attribute-usage-and-implicit-behavior) 设置为 false 将创建一个死粒子。

虽然这样做可以让您在粒子生成时丢弃它们，但过度生成仍然适用。该粒子仅在下一次更新调用中才被视为死亡。这意味着您不能创建比剩余数量允许的更多的粒子（无论是活动粒子还是死亡粒子）。

### 调用顺序

Visual Effect Graph 仅在第一次 Update 之前对每个新元素执行一次 Initialize Context。在执行帧中，Visual Effect Graph 初始化新元素，执行元素的第一次更新，最后渲染元素。

### 源属性可用性

在 Initialize 上下文中，代码块和运算符可以通过 Get Attribute (Source) 运算符，或者通过 Inherit <Attribute> 代码块， [源属性](https://docs.unity.cn/cn/Packages-cn/com.unity.visualeffectgraph@latest/index.html?subfolder=/manual/Attributes.html%23source) 读取数据。


### 输入流兼容性

Initialize 上下文可以通过以下规则从一个或多个 SpawnEvent 输出上下文进行连接：

- Initialize 上下文可以从任意数量的 Spawn 和/或 Event 上下文连接。
- Initialize 上下文可以从单个 GPU Event 上下文连接。
- 您不能将 GPU 和 CPU Event/Spawn 上下文混合到输入端口。如果同时连接 GPU Event 和 Spawn 上下文，控制台会显示以下错误：`Exception while compiling expression graph: System.InvalidOperationException: Cannot mix GPU & CPU spawners in init`
