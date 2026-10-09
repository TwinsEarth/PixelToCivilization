# Visual Effect Graph 资源

Visual Effect Graph 资源是 Visual Effect Graph 用于播放视觉效果的数据容器。它包括：

+ 图形元素
+ 公开的属性
+ 已编译的着色器
+ 运算符字节码
  
## 创建 Visual Effect Graph 资产

要创建 Visual Effect Graph 资源，请选择 **Assets** > **Create** > **Visual Effects** > **Visual Effect Graph** 创建资源。Unity 会在 Project 窗口的当前打开文件夹中创建新的 Visual Effect。

## 使用 Visual Effect Graph 资源

要编辑 Visual Effect Graph 资源，请先在 [Visual Effect Graph 窗口](VisualEffectGraphWindow.md)打开它。为此，您可以：

+ 双击 Project 窗口中的 Visual Effect Graph 资源。
+ 在 Project 窗口中选择 Visual Effect Graph 资源以在 Inspector 中查看它，然后在资源的标题中单击 **Open** 按钮。
+ 在[Visual Effect 组件](VisualEffectComponent.md#the-visual-effect-inspector)的 Inspector 中，单击 Asset Template 属性旁边的 Edit 按钮。这将打开分配的 Visual Effect Graph 资源。

打开 Visual Effect Graph 后，您现在可以编辑 Visual Effect。

#### Visual Effect 资源检查器
当您选择 Visual Effect Graph 资源时，Inspector 会显示资源范围的配置 Options。

| 属性名称          | 描述/值                                         |
| ----------------------- | ------------------------------------------------------------ |
| **Fixed Delta Time**    | 指示是否以 Fixed Time Step （固定时间步长） 属性在 [Visual Effect 项目设置.](VisualEffectProjectSettings.md)。  |
| **Culling Flags**       | 设置 Unity 是否根据其剔除状态更新视觉效果。剔除状态是指 Camera 是否可以看到视觉效果的边界框。选项包括：<br />&#8226; **Recompute bounds and simulate when visible**: Unity 模拟效果，并在效果可见时重新计算效果的边界框。如果您的视觉效果使用动态边界框（使用 Operators 计算的边界框），请不要使用此选项，而应使用包含 **Always Recompute Bounds** 的边界框。<br /> &#8226; **Always Recompute Bounds, simulate only when Visible**: 无论任何摄像机是否可以看到效果的边界框，Unity 始终会重新计算边界框。Unity 仅在 Camera 可以看到更新的边界时模拟效果。<br />&#8226; **Always Recompute Bounds and Simulate**: 无论任何摄像机是否可以看到效果的边界框，Unity 始终会重新计算边界框并模拟效果。<br /><br />**注意**: 无论采用何种模式，Unity 始终使用边界框来执行效果的剔除。 |
| **Exact Fixed Time**    | 指示如果帧时间长于 **Fixed Time Step**，是否每帧多次更新视觉效果。例如，如果一帧需要 10 毫秒，而 **Fixed Time Step** 为 5 毫秒，则效果会以 5 毫秒的 deltaTime 更新两次，而不是以 10 毫秒的 deltaTime 更新一次。这是一项资源密集型操作，因此请仅在高端场景中使用。<br/>仅当启用 **Fixed Delta Time** 时，此属性才相关。 |
| **Ignore Time Scale**   | 指示是否忽略[时间表](https://docs.unity3d.com/ScriptReference/Time-timeScale.html)。当 Unity 模拟效果时。如果您希望视觉效果在时间刻度不等于 `1` 时以正常速度播放，请启用此属性。  |
| **PreWarm Total Time**  | 设置 Unity 模拟效果发生的持续时间（以秒为单位）。这会预先模拟效果，因此，当效果开始时，它看起来已经 "构建" 了。更改此值时，Unity 会计算 **PreWarm Delta Time**.`Reset()` 的新值。|
| **PreWarm Step Count**  | 设置 Unity 用于计算 PreHeat 的模拟步骤数。步数越多，效果的精度越高，资源强度越高，这会降低性能。更改此值时，Unity 会计算r **PreWarm Delta Time**的新值。 |
| **PreWarm Delta Time**  | 设置 Unity 用于 PreHeat 的增量时间（以秒为单位）。更改此值时，Unity 会计算 **PreWarm Total Time** 和 **PreWarm Step Count**的新值。如果需要为模拟使用精确的增量时间，请调整此值，而不是单独调整 **PreWarm Total Time** 和 **PreWarm Step Count**。 |
| **Initial Event Name**  | 设置 [事件](Events.md)Unity 在启用效果时发送。默认值为 **OnPlay**，但您可以将其更改为其他名称，甚至是空白字段，以便默认情况下不会生成每个系统。 |
| **Output Render Order** | 定义一个列表，该列表按渲染顺序显示每个 Output Context。您可以重新排序此列表以更改 Unity 渲染输出上下文的顺序。Unity 首先在列表顶部绘制项目，然后逐渐绘制位于列表下方的项，位于上方的项之前。 |
| **Shaders**             | 定义 Unity 为 Visual Effect Graph 编译的每个着色器的列表。这些是只读的，主要用于调试目的。在 **Shader Externalization** 中使用 [Visual Effect 首选项](VisualEffectPreferences.md) 临时外部化 Shaders 以进行调试。 |
