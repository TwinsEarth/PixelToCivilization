# Visual Effect Graph 入门

此页面向您展示如何安装 Visual Effect Graph，并简要概述如何使用 Visual Effect Graph [创建](#创建视觉效果图形)、[编辑](#编辑-visual-effect-graph)和[预览](#预览图形的效果)效果。有关图形工作原理的概述，请参见[图形逻辑与理念](GraphLogicAndPhilosophy.md)。 Visual Effect Graph 是一个使用[可编程渲染管线](https://docs.unity3d.com/Manual/ScriptableRenderPipeline.html)来渲染视觉效果的 Unity 程序包。Visual Effect Graph 使用计算着色器来模拟效果。

## 要求

有关 Visual Effect Graph 系统要求的信息，请参阅[要求和兼容性](System-Requirements.md)。

## 安装 Visual Effect Graph

安装 Visual Effect Graph 程序包的步骤：

1.  在 Unity 编辑器中，进入 **Window** > **Package Manager**。在顶部导航栏中，确保选中 **All packages**。
2.  注意：在 Unity 2019.3 之前的版本上，您必须选中 "Show preview packages" "Advanced" 选项，Visual Effect Graph 才会出现在列表中。
3.  选择 **Visual Effect Graph** 程序包。
4.  在右下角，单击 **Install**。
   
![](Images/InstallVisualEffectGraph.png)


### 使用正确版本的 Visual Effect Graph

每个 Visual Effect Graph 程序包都能与相同版本的可编程渲染管线程序包一起使用。如果要升级 Visual Effect Graph 程序包，也必须升级正在使用的渲染管线程序包。

例如，Package Manager 中的 Visual Effect Graph 程序包版本 6.5.3-preview 可与高清晰度 RP 程序包 版本 6.5.3-preview 一起使用。

## 创建视觉效果图形

要使用 Visual Effect Graph，您必须首先创建一个 [Visual Effect Graph 资源](VisualEffectGraphAsset.md)。

要创建 Visual Effect Graph 资源：

1.  在 Unity 中，单击 **Assets** > **Create** > **Visual Effects** > **Visual Effect Graph**。

要制作 Visual Effect Graph 资源的副本：

1.  在 Project 窗口中，选择要制作副本的视觉效果资源。
2.  在顶部导航栏中，选择 **Edit** > **Duplicate**。现在已创建了副本。

## 在场景中使用视觉效果图形

要使用视觉效果图形，您必须在场景中添加一个[视觉效果](#创建视觉效果图形)。

为此，可执行以下操作之一：

+   将一个 Visual Effect Graph 资源从 Project 窗口拖放到 Hierarchy 窗口。  
    当您将资源放在现有游戏对象上时，会添加一个具有视觉效果组件的新的子游戏对象，并为其分配该图形。  
    当您将该资源放在空白处时，Unity 会创建一个新的视觉效果游戏对象并将图形分配给它。
+   将一个 Visual Effect Graph 资源从 Project 窗口拖放到 Scene 视图窗口。这使得该图形出现在摄像机前面。

在您将 Visual Effect Graph 资源添加到 Hierarchy 以后，Unity 会将该资源附加到[视觉效果组件](VisualEffectComponent.md)，该组件引用该资源。

## 编辑 Visual Effect Graph

在 [Visual Effect Graph 窗口](VisualEffectGraphWindow.md)中编辑 Visual Effect Graph 资源：

+   打开包含空白图形的 Visual Effect Graph 窗口（菜单：**Window\_** > **Visual Effects**）。这会提示您打开一个 Visual Effect Graph 资源。
+   选择一个现有的 Visual Effect Graph 资源，然后单击检查器中的 **Edit** 按钮。这将打开 Visual Effect Graph 窗口，其中包含此资源中所含的图形。
+   选择该视觉效果组件（菜单：在 Asset 模板旁边，单击 **Edit**）。这将打开 Visual Effect Graph 窗口，其中包含被引用资源中所含的图形。

## 预览图形的效果

要预览效果，您可以：

+   选择一项 Visual Effect Graph 资源并使用 Inspector Preview 窗口。
    
+   将您的效果作为视觉效果游戏对象直接放置在场景中。
    

这使您可以直接在场景中编辑参数，查看效果上的光照，并为效果的特定目标实例使用[目标游戏对象面板](VisualEffectGraphWindow.md#target-visual-effect-gameobject) 功能。

## 操作图形元素

当您在 Visual Effect Graph 窗口中打开一项资源时，您可以为该特定资源查看和编辑该图形。

Visual Effect Graph 包含[运算符](Operators.md)和[代码块](Blocks.md)。每个节点负责处理其输入属性。您可以将节点链接在一起以执行一系列计算。所有节点最终连接到一个代码块（或上下文）：代码块根据其输入属性定义对效果的操作。

当您将多个代码块链接在一起时，它们就形成了一个上下文。有关 Visual Effect Graph 中的节点、代码块和上下文的更多信息，请参见[图形逻辑](GraphLogicAndPhilosophy.md)。

您对图形所做的每一次更改都会对效果的行为产生直接影响，您可以实时预览这些更改。每次添加、删除或连接节点时，图形都会重新编译所有已更改的元素，并重新启动效果。但是，更改值（例如，编辑曲线）不会使 Unity 重新编译任何内容并实时影响模拟。 要添加节点，您可以执行以下任何一种操作：

+   在图形中单击右键，然后选择 **Create Node**。
+   按键盘上的空格键。
+   单击并从现有端口拖动一个边缘，然后在空白处释放鼠标。

当您执行上述任何操作时，都会显示 **Create Node** 菜单。在这里，您可以看到与图形中特定位置兼容的节点、代码块和上下文。
