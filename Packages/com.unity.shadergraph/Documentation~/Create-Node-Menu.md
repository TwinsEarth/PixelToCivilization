Create Node Menu
================

描述
--

**Create Node Menu** 用于在 Shader Graph 中创建[节点](Node.md)。要打开 **Create Node Menu**，右键单击 [Shader Graph 窗口](Shader-Graph-Window.md)中的工作空间，然后选择 **Create Node** 或按空格键。


**Create Node Menu** 的顶部是搜索栏。可以通过在搜索字段中键入节点名称的任何部分来搜索节点。搜索框提供了自动完成选项，您可以按 Tab 接受预测文本。匹配的文本以黄色突出显示。


Shader Graph 中可用的所有节点都列在按节点函数分类的 **Create Node Menu** 中。用户创建的[子图形](Sub-graph.md)也会在 **Sub Graph Assets** 下的 **Create Node Menu** 中列出，或在 Sub Graph Asset 中定义的自定义类别中列出。


要将一个节点添加到工作空间，请在 **Create Node Menu** 中双击该节点。


### Create Node Menu 上下文菜单


**Create Node Menu** 上下文菜单会过滤可用的节点，因此仅显示使用选定边缘的[数据类型](Data-Types.md)的节点。其中将列出这些节点上每个与该数据类型匹配的可用[端口](Port.md)。


要打开 **Create Node Menu** 上下文菜单，可从端口点击并拖动一个[边缘](Edge.md)（Edge），然后将其放置到工作空间的空白区域中。


### 主栈 Create Node Menu

要在主栈（Master Stack）中添加一个新的 Block 节点，可以右键单击并选择 **Create Node**，或者在选中栈的情况下按空格键。

**Create Node Menu** 将根据项目中的渲染管线显示主栈的所有可用的块。任何块都可以通过**Create Node Menu** 添加到主栈。如果添加的块与当前 Graph Settings 不兼容，则该块将被禁用，直到该设置的配置能支持该块。