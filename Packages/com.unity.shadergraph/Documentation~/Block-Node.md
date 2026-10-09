Block 节点
==========

描述
---------------------------

Block 是 Master Stack 中的一种特定类型的节点。Block 表示 Shader Graph 在最终着色器输出中使用的单一表面（或顶点）描述数据的一个部分。[内置 Block 节点](Built-In-Blocks.md)始终可用，但特定于某个渲染管线的节点仅在该管线中可用。例如，Universal Block 节点仅在通用渲染管线（URP）中可用，而 High Definition Block 节点仅在高定义渲染管线（HDRP）中可用。

某些 Block 仅与特定的[图形设置](Graph-Settings-Tab.md)（Graph Settings）兼容，并且可能会根据您选择的图形设置变为激活或非激活状态。您不能剪切、复制或粘贴 Block。

添加和移除 Block 节点
---------------------------------------------------------

要在 Master Stack 中的 Context 中添加新的 Block 节点，将光标放置在 Context 中的空白区域上，然后按空格键或右键点击并选择 **创建节点**。

这将弹出 Create Node 菜单，仅显示适用于当前 Context 的 Block 节点。例如，Vertex Block 节点不会出现在 Fragment Context 的创建节点菜单中。

从菜单中选择一个 Block 节点，将其添加到 Context 中。要从 Context 中删除一个 Block，选择 Context 中的 Block 节点，然后按删除键或右键点击并选择 **删除**。

### 自动添加或移除 Block

您还可以在 Shader Graph 偏好设置中启用或禁用一个选项，自动添加和移除 Context 中的 Block 节点。

如果启用 **Automatically Add or Remove Blocks**，Shader Graph 会自动根据该特定资源的目标或材质类型添加所需的 Block 节点。它还会自动移除任何没有连接且默认值的无效 Block 节点。

如果禁用 **Automatically Add or Remove Blocks**，Shader Graph 不会自动添加或移除 Block 节点。您必须手动添加和移除所有 Block 节点。

**激活和非激活的 Block**
---------------------------------------------------------

激活的 Block 节点是对最终着色器有贡献的 Block。非激活的 Block 节点是存在于 Shader Graph 中，但对最终着色器没有贡献的 Block。

![image](./Images/Active-Inactive-Blocks.png)

当您更改图形设置时，某些 Block 可能会变为激活或非激活。非激活的 Block 节点和仅连接到非激活 Block 节点的节点流将显示为灰色。