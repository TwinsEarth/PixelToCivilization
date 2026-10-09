节点
==

描述
--


**节点**根据其可用的[端口](Port.md)（Ports）在 Shader Graph 上定义输入、输出或运算。**节点**可以具有任意数量的输入和/或输出端口。可以通过用[边](Edge.md)（Edges）将这些端口连接起来创建 Shader Graph。**节点**可能还有任意数量的**控件**，这些控件是**节点**上没有端口的控件。


可以通过单击**节点**右上角的 **Collapse** 按钮来折叠**节点**。这将隐藏所有未连接的端口。


如需了解**节点**组件，请参阅：

* [端口](Port.md)
* [边](Edge.md)


Shader Graph 中有许多可用的**节点**。有关所有可用**节点**的完整列表，请参阅[节点库](Node-Library.md)。


预览
--

许多节点包含预览功能，该预览显示图中该阶段的主要输出值。将鼠标悬停在节点上时会显示“折叠”控件，点击即可隐藏预览。您还可以通过 [Shader Graph 窗口](Shader-Graph-Window.md)中的上下文菜单来折叠或展开节点预览。要配置节点预览的外观，请参阅[预览模式控制](Preview-Mode-Control.md)。


上下文菜单
-----

右键单击**节点**将打开上下文菜单。此菜单包含许多可对**节点**执行的操作。请注意，选择多个节点时，这些操作将应用于整个选择范围。




| 项 | 描述 |
| --- | --- |
| Copy Shader | 将图形中此阶段生成的 HLSL 代码复制到剪贴板 |
| Disconnect All | 从**节点**上的所有端口删除所有边 |
| Cut | 将选定的**节点**剪切到剪贴板 |
| Copy | 将选定的**节点**复制到剪贴板 |
| Paste | 粘贴剪贴板中的**节点** |
| Delete | 删除选定的**节点** |
| Duplicate | 复制选定的**节点** |
| Convert To Sub\-graph | 使用选定**节点**创建新的[子图形资源](Sub-graph-Asset.md) |
| Convert To Inline Node | 将[属性节点](Property-Types.md)转换为适当[数据类型](Data-Types.md)的常规节点 |
| Convert To Property | 将**节点**转换为 [Blackboard](Blackboard.md) 上适当[属性类型](Property-Types.md)的新**属性** |
| Open Documentation | 在新的 Web 浏览器中打开[节点库](Node-Library.md)中的所选**节点**文档页面 |


颜色模式
----

**节点**受到 Shader Graph 窗口的颜色模式所影响。颜色显示在节点上（节点标题栏上的文本下方）。有关节点的可用颜色的更多信息，请参阅[颜色模式](Color-Modes.md)。
