Shader Graph 窗口
===============


描述
--


**Shader Graph 窗口**包含使用 **Shader Graph** 系统创建着色器的工作空间。要打开 **Shader Graph 窗口**，必须先创建一个 [Shader Graph 资源](index.md)。有关更多信息，请参阅[开始使用](Getting-Started.md)部分。


**Shader Graph** 窗口包含各种单独的元素，例如 [Blackboard](Blackboard.md)、[Graph Inspector](Internal-Inspector.md) 和 [Main Preview](Main-Preview.md)。这些元素可以在工作空间内移动。缩放 **Shader Graph 窗口**时，这些元素将自动锚定到最近的角落。


标题栏
---

**Shader Graph 窗口**顶部的标题栏包含可对**图形**执行的操作。


| 项 | 描述 |
| --- | --- |
| Save Asset | 保存图形以更新 Shader Graph 资源。 |
| Save As | 打开一个文件对话框，允许用户以新名称保存 Shader Graph 资源。 |
| Show In Project | 此时将在 [Project 窗口](https://docs.unity.cn/cn/tuanjiemanual/Manual/ProjectView.html)中突出显示 Shader Graph 资源。 |
| Check Out | 如果启用了版本控制，这将从源代码控制提供程序中签出 Shader Graph 资源 。 |
| Color Mode | 提供下拉菜单以选择图形的[颜色模式](Color-Modes.md)。 |
| Blackboard | 切换 [Blackboard](Blackboard.md) 的可见性。 |
| Graph Inspector | 切换 [Graph Inspector](Internal-Inspector.md) 的可见性。 |
| Main Preview | 切换 [Main Preview](Main-Preview.md) 的可见性。 |

****
工作空间
----

工作空间是创建[节点](Node.md)网络的地方。

可按住鼠标左键并拖动以使用选取框选择多个节点。还有各种快捷键可用于改善工作流程。

| 热键 | Windows | OSX | 描述 |
| --- | --- | --- | --- |
| 剪切 | Ctrl \+ X | Command \+ X | 将选定的节点剪切到剪贴板 |
| 复制 | Ctrl \+ C | Command \+ C | 将选定的节点复制到剪贴板 |
| 粘贴 | Ctrl \+ V | Command \+ V | 粘贴剪贴板中的节点 |
| 焦点 | F | F | 将工作空间聚焦在所有或选定的节点上 |
| 创建节点 | 空格键 | 空格键 | 打开 [Create Node Menu](Create-Node-Menu.md) |

要在工作空间中导航，可按住 Alt 和鼠标左键，使用滚轮进行平移和缩放。

当您将工作空间拉远至一定距离后，节点的 **LOD（细节层次）** 预览将自动关闭。团结引擎优化了整体预览的操作体验，使视图更加简洁清晰，避免了不必要的细节干扰。

![](./Images/LODOptimization.gif)

上下文菜单
-----

在工作空间内单击右键将打开上下文菜单。请注意，右键单击工作空间中的某一项（例如节点）将打开该项的上下文菜单，而不是工作空间。


| 项 | 描述 |
| --- | --- |
| Create Node | 打开 [Create Node Menu](Create-Node-Menu.md) |
| Create Sticky Note | 在图形上创建一个新的[即时贴](Sticky-Notes.md)。 |
| Collapse All Previews | 折叠所有节点上的预览 |
| Cut | 将选定的节点剪切到剪贴板 |
| Copy | 将选定的节点复制到剪贴板 |
| Paste | 粘贴剪贴板中的节点 |
| Delete | 删除选定的节点|
| Duplicate | 复制选定的节点 |
| Select / Unused Nodes | 从主栈（Master Stack）选择图形中所有对最终着色器输出没有贡献的节点。 |
| View / Collapse Ports | 折叠所有选定节点上未使用的端口 |
| View / Expand Ports | 展开所有选定节点上未使用的端口 |
| View / Collapse Previews | 折叠所有选定节点上的预览 |
| View / Expand Previews | 展开所有选定节点上的预览 |
| Precision / Inherit | 将所有选定节点的精度设置为 Inherit。 |
| Precision / Float | 将所有选定节点的精度设置为 Float。 |
| Precision / Half | 将所有选定节点的精度设置为 Half。 |



