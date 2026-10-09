# 版本 14 / Unity 2022.2 的新增功能
本页概述了 Visual Effect Graph 版本 14 中嵌入的新功能、改进和已解决的问题，嵌入在 Unity 2022.2 中。

## 新功能
以下是 Unity 添加到 Unity 2022.2 中嵌入的 Visual Effect Graph 版本 14 的功能列表。每个条目都包含功能摘要和指向任何相关文档的链接。

### 六面烟雾

![](Images/VFX-WhatsNew14-2-still.png)

VFX Graph 版本 14 包括一种照亮烟雾效果的新方法。现在，您可以使用从第三方软件导出的自定义光照贴图来照亮烟雾纹理。VFX Graph 从这些光照贴图中烘焙六个光照方向，并使用它们从各个方向照亮烟雾。

### 布尔端口

![](Images/VFX-WhatsNew14-6.png)

版本 14 包括布尔端口，可用于根据逻辑操作激活或停用 Block。此功能还包括数字类型和布尔值之间的一些隐式转换。

### 2D Shader Graph 支持

![](Images/VFX-WhatsNew14-3.png)

在此版本中，Visual Effect Graph 支持 Shader Graph 的 2D 子目标，您可以使用这些子目标将粒子渲染为 Sprite。要查找新的子目标，请转到 **Create > Shader Graph > URP**。此功能仅在 Universal Render Pipeline 中可用。

## 更新

以下是 Unity 在 Unity 2022.2 中嵌入的 Visual Effect Graph 版本 14 中所做的改进列表。每个条目都包含改进摘要，以及指向任何文档的链接（如果相关）。

### 时间线集成

![](Images/VFX-WhatsNew14-4.gif)

VFX Graph 14 改进了时间轴中视觉效果的工作流程。您可以使用新的 VFX 动画剪辑来拖动和控制 VFX Graph[事件](Events.md)。您还可以控制效果如何重新初始化和预热效果。更多信息，请参见[时间轴](Timeline.md)。

### 蒙皮网格采样

![](Images/VFX-WhatsNew14-5.gif)

在 VFX 中，Graph 14 根据您的反馈通过以下方式改进了蒙皮网格渲染器采样的各个方面：

+ 现在，您可以直接在 **Sample Skinned Mesh** 操作符中检索位置速度。
+ 可以在变换中定位粒子，由采样的切线和法线定义。
+ 根骨骼变换选项现在已集成到块或运算符中，这意味着您不需要使用属性绑定器。


## 修复问题
有关 Unity 2022.1 中嵌入的 Visual Effect Graph 版本 14 中已解决的问题的信息，请参阅[更改日志](../changelog/CHANGELOG.html)。
