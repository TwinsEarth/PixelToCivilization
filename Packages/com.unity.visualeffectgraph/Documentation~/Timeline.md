# 视觉效果和时间轴

![](Images/VFX_Timeline_Window.png)

使用 Unity 的[时间轴](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html?subfolder=/manual/index.html)控制 Visual Effect Graph 的属性和事件。为此，请将以下内容添加到 [Timeline 实例](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html?subfolder=/manual/wf_instance.html)中：

- [视觉效果控制轨迹](#视觉效果控制轨迹)。
- [视觉效果控制剪辑](#视觉效果控制剪辑)。

<a name="vfx-control-track"></a>

##  视觉效果控制轨迹

Visual Effect Control 轨迹是一种 Timeline 轨迹，用于确定 Timeline 何时可以发送 [Events](Events.md) 和 [Event Attribute Payloads](Events.md#event-attribute-payloads)。

![](Images/VFX_Timeline_ControlTrack.png)

要控制控制轨迹中的视觉效果，请创建一个 Visual Effect 控制轨迹并向其添加 Visual Effect：

1. 打开 Timeline 窗口（菜单：**Window > Sequencing > Timeline**）
2. 导航到 Hierarchy 窗口，然后选择 Visual Effect 游戏对象
3. 在 Timeline （时间轴） 窗口中，选择 **Create** 创建。
4. 保存 Timeline 资源。
5. 点击 + 按钮在 时间线 窗口。
6. 选择 **Visual Effect Control Track**。
7. 选择![img](images/vfx-timeline-toggle.png)图标并选择 IMGVisual Effect 资源。

您还可以使用 [VFXEventAttribute](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.SendEvent.html) 在脚本的 Event Attribute Payload 中配置事件的属性。

### 属性

当您选择 Visual Effect Control 轨迹时，Inspector （检查器） 窗口中会显示以下属性：

| **属性** | **描述**                                              |
| ------------ | ------------------------------------------------------------ |
| Reinit       | 控制 Unity 何时重置 Visual Effect 控制轨迹：<br/>&#8226; **On Binding Enable Or Disable:** 当您在时间轴中绑定或取消绑定 Visual Effect 时，重置控制轨迹。默认情况下，**Reinit** 使用此设置。 <br/>&#8226; **On Binding Enable:** R当您在时间轴中绑定 Visual Effect 时重置控制轨迹。<br/> &#8226; **On Binding Disable:** 当您在时间轴中取消绑定 Visual Effect 时，重置 Visual Effect Control 轨迹。<br/>&#8226; **None:** Unity 不会重置 Visual Effect Control 轨迹。 |

<a name="vfx-control-clip"></a>

## 视觉效果控制剪辑

控制剪辑是 Timeline 轨迹的一部分，用于控制 Visual Effect。你可以使用控制 Clip 来划动 Visual Effect，并添加 Clip 事件和单个事件。Visual Effect Control 剪辑存在于 Visual Effect Control 轨迹内。

![](Images/VFX_Timeline_ControlClip.png)

要添加 Visual Effect Control 剪辑：

1. 在 Clips 视图中，右键单击 Visual Effect Control 轨迹中的空白区域。
2. 选择 **Add Visual Effect Control Clip**。
3. 选择剪辑以在 Inspector 中显示其属性。

Visual Effect Control Track 中的每个剪辑都控制着 Visual Effect 在场景中的行为方式。您可以在 Inspector 窗口的 **Clip Events** 和 **Single Events** 部分中将 Visual Effect 事件发送到剪辑。

### 属性

当您选择 Visual Effect Control 剪辑时，Inspector 窗口中会显示以下属性：

| **属性**   |                    |            | **描述**                                              |
| -------------- | ------------------ | ---------- | ------------------------------------------------------------ |
| Scrubbing      |                    |            | 使用 **Scrubbing** 将效果与 Timeline 实例中的其他动画游戏对象同步。启用此选项可在拖动 Timeline Playhead（时间轴播放头）时向后和向前播放 Visual Effect。<br/> 启用 **Scrubbing** **时，Reinit** 默认为 **On Enter And Exit Clip**。这是因为划动只能控制从 Visual Effect Control 剪辑开头开始的 Visual Effect。<br/>启用此属性以使用 **Prewarm** 属性。<br><br/>禁用清理以：<br/>&#8226; 在将 **Reinit** 设置为 **On Enter** 或 **Exit** 时，公开 Enable PreWarm 属性。<br/>&#8226; 当播放头进入 Visual Effect Control 剪辑时发送 Enter 事件。 |
| Reinit         |                    |            | C控制时间轴何时初始化视觉效果：<br/>&#8226; **None:** 时间轴不再初始化视觉效果。<br/>&#8226; **On Exit Clip:** 时间轴在 Visual Effect Control 剪辑结束时重新启动 Visual Effect。<br/>&#8226; **On Enter Clip:** 时间轴在 Visual Effect Control 剪辑的开头重新初始化 Visual Effect。禁用 **Scrubbing** 后，此模式将公开 Enable PreWarm 属性。<br/>&#8226; **On Enter or Exit:** 时间轴在 Visual Effect Control 剪辑的开头和结尾重新初始化 Visual Effect。Reinit 默认使用此模式。禁用 **Scrubbing** 后，此模式将公开 **Enable PreWarm** 属性。 |
| Start Seed     |                    |            | 此属性表示 Unity 用于其随机数生成器的初始种子（另请参阅 [`VisualEffect.startSeed`](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect-startSeed.html)）。<br/><br/>当您启用 **Scrubbing** 或将 **Reinit** 模式设置为 **On Enter Clip** 时，Visual Effect Control 剪辑将控制此属性。 |
| Enable PreWarm |                    |            | 启用此属性可在 Visual Effect 出现之前开始处理它。<br/> 设置此属性时，它不会影响 Visual Effect 资源中的 PreWarm 设置。这意味着您可以拥有多个动画剪辑，这些动画剪辑使用具有不同预热设置的相同 Visual Effect 资源。<br/><br/>仅当启用 **Scrubbing** 或将 **Reinit** 模式设置为 **On Enter Clip** 或 **On Enter or Exit** 时，此属性才可用。 |
|                | PreWarm Total Time |            | S在视觉效果开始之前，在给定的时间内模拟视觉效果。此属性的值以秒为单位。设置此值以重新计算 **PreWarm Delta Time**。 |
|                | PreWarm Step Count |            | 设置 Unity 用于计算 PreHeat 的模拟步骤数。<br/>步骤数越多，PreWarm 越精确，但可能会影响性能。 <br/>更改此值时，Unity 会计算 **PreWarm Delta Time** 的新值。 |
|                | PreWarm Delta Time |            | 设置 Unity 用于 PreHeat 的增量时间（以秒为单位）。<br/>更改此值时，Unity 会计算 **PreWarm Total Time** 和 **PreWarm Step Count** 的新值。如果需要为模拟使用精确的增量时间，请调整此值，而不是单独调整 **PreWarm Total Time** 和 **PreWarm Step Count**。  |
|                | PreWarm Event Name |            | Unity 在效果开始时发送的[事件](Events.md)的名称。您可以自定义此名称，也可以将其留空以使其默认不会生成每个系统。<br/><br/>默认值为 **OnPlay**。 |
| Clip Events    |                    |            | 选择 **+** 以添加视觉效果[事件](Events.md) 在 VisualEffect 控件剪辑中。Clip Event 有 start 和 end。<br/> 当您禁用划动并且播放头进入 Visual Effect Control 剪辑时，它会发送 **Enter** 事件。 |
|                | Enter              |            | 当 Timeline Playhead 位于 Clip Event 的开头时，Unity 会启动 Visual Effect。 |
|                |                    | Time       | Unity 用于确定何时在剪辑中播放 Visual Effect 的值。单位根据您选择的 **Time Space** 模式而变化。 |
|                |                    | Time Space | &#8226; **After Clip Start**: 播放 Visual Effect Control 剪辑开始后加上您在 **Time** 字段中输入的秒数的事件。<br/>&#8226; **Before Clip End**:播放 Visual Effect Control 剪辑结束前的事件，减去您在 **Time** 字段中输入的秒数。<br/>&#8226; **Percentage**: 以 Visual Effect Control 剪辑总时间的百分比播放事件。例如，**Time** 值为 0 表示剪辑的开始，值为 100 表示结束。<br/>&#8226; **Absolute**:  在您在 **Time** 属性中指定的秒播放事件。 |
|                |                    | Name       | Clip Event 的名称。Enter Clip Event 的默认名称为 OnPlay。此名称显示在 Visual Effect Control 轨迹中。 |
|                |                    | Attributes | 选择 **+** 以添加 Timeline 随相应的 Visual Effect Event 发送的属性。例如[位置](Operator-GetAttributePosition.md), [颜色](Operator-GetAttributeColor.md)或[自定义属性](Operator-GetCustomAttribute.md)。 |
|                | Exit               |            | 当 Timeline Playhead 到达剪辑末尾时，Unity 会停止 Visual Effect。 |
|                |                    | Time       | Unity 用于确定何时在 Clip 中播放 Visual Effect 的值。单位根据您选择的 **Time Space** 模式而变化。 |
|                |                    | Time Space | &#8226; **After Clip Start**: 播放 Visual Effect Control 剪辑开始之后的事件。<br/>&#8226; **Before Clip End**: 在 Visual Effect Control 剪辑结束之前播放事件。<br/>&#8226; **Percentage**: 以 Visual Effect Control 剪辑总时间的百分比播放事件。例如，值 0 表示剪辑的开始，值 100 表示结束。<br/>&#8226; **Absolute**: 在轨迹中的特定秒播放事件。 |
|                |                    | Name       | Clip Event 的名称。Exit Clip Event 的默认名称是 OnStop。此名称显示在 Visual Effect Control 轨迹中。 |
|                |                    | Attributes | 选择 **+** 以添加 Timeline 随相应的 Visual Effect Event 发送的属性。例如[位置](Operator-GetAttributePosition.md), [颜色](Operator-GetAttributeColor.md)或[自定义属性](Operator-GetCustomAttribute.md)。Unity 使用这些属性创建[VFXEventAttribute](https://docs.unity3d.com/ScriptReference/VFX.VFXEventAttribute.html)它与单个 Event 一起发送。 |
| Single Events  |                    |            | 选择 **+** 可在 Visual Effect Control 剪辑中添加单个 Visual Effect Event。单个事件发生一次。这对于爆发生成系统非常有用。禁用 **Scrubbing** 时，Single Event 仅在时间轴向前播放时播放。 |
|                | Editor Color       |            | 指示 Timeline 窗口中的 Single Event 的颜色。 |
|                | Time               |            | Unity 用于确定何时在 Visual Effect Control 剪辑中发送 Single Event 的值。单位根据您选择的 **Time Space** 模式而变化。 |
|                | Time Space         |            | &#8226; **After Clip Start**: 在 Visual Effect Control 剪辑开始后播放单个事件。<br/>&#8226; **Before Clip End**: 在 Visual Effect Control 剪辑结束之前播放单个事件。<br/>&#8226; **Percentage**: 以 Visual Effect Control 剪辑总时间的百分比播放单个事件。例如，值 0 表示剪辑的开始，值 100 表示结束。<br/>&#8226; **Absolute**:  在轨迹中的特定秒播放单个 Event。 |
|                | Name               |            | Single Event 的名称。此名称显示在 Visual Effect Control 轨迹中。 |
|                | Attributes         |            | 选择 **+** 以添加 Timeline 随相应的 Visual Effect Event 发送的属性。例如[位置](Operator-GetAttributePosition.md), [颜色](Operator-GetAttributeColor.md)或[自定义属性](Operator-GetCustomAttribute.md)。<br/>Unity 使用这些属性创建[VFXEventAttribute](https://docs.unity3d.com/ScriptReference/VFX.VFXEventAttribute.html)它与单个 Event 一起发送。 |

## 动画属性

要在 Timeline and Animation 窗口中对 Visual Effect Graph 的动画属性进行动画处理，请将 Animation Track 分配给 Visual Effect 游戏对象。要在 Timeline （时间轴） 窗口中对 Visual Effect 属性进行动画处理：

1. 创建 [视觉效果控制轨迹](#视觉效果控制轨迹).
2. 在 Timeline （时间轴） 窗口中，打开 **+** 下拉菜单并选择 **Animation Track**。
3. 选择 Animation Track 的 ![img](images/vfx-timeline-toggle.png)图标，然后选择具有 [Animator 组件](https://docs.unity3d.com/Manual/class-Animator.html)的 Visual Effect 资源。

这会将 Visual Effect GameObject 绑定到动画轨迹，并在 Inspector 窗口中添加 [Playable Director](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html?subfolder=/manual/play_director.html) 组件。

# 局限性

- Unity 限制了启用 **Scrubbing** 时可以跳过的时间。您可以在 **Project Settings > VFX > Max Scrube Time** 中更改此限制。如果超过此限制，Unity 会显示警告。
- 当您在运行时在 Timeline 中向后播放 Visual Effect 时，它会导致 GPU 使用达到高峰。这是因为此实现使用 [Simulate](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.Simulate.html)。
- 为控制同一 Visual Effect 的多个 Timeline Track 启用划动时，Unity 会在 Scene 视图中显示警告。

# 自动迁移

升级到 Unity 版本 2022.2.0a9 或更高版本时，Unity 会自动迁移时间轴中的 Visual Effect 集成。为此，它会升级：

- **Visual Activation track** 到 **Visual Effect Control track** 
- **Visual Activation clip** 到 **Visual Effect Control clip**

为了保持相同的行为并避免任何中断性变更，Unity 会自动：

- 使用 **Visual Effect Control clip** 设置镜像的 **Visual Effect Control track**。
- 将镜像的 **Visual Effect Control track** 的 **Reinit** 属性设置为 **None**。
- 在镜像的 **Visual Effect Control clip** 上禁用**Scrubbing**。
- 将 **Name** 和 **Attributes** 属性值复制到 **Clip 事件**和**单个事件**中。