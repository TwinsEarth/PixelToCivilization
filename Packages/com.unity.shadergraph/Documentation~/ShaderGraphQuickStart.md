# Shader Graph快速入门
<本文原本是放在用户手册里的，目前已弃，后续也需要修改团结用户手册里的内容，跳转到包的文档> 


## 下载与安装

**【这部分来源于package documentation，请根据目前的实际情况修改】**

将 HDRP 或 URP 安装到项目中时，团结引擎会自动安装 Shader Graph 包。您可以通过 **Package Manager -> Packages: Unity Registry -> Shader Graph** 手动安装，以便与内置渲染管线一起使用。有关如何安装包的更多信息，请参阅 Unity User Manual 中的[添加和删除包](https://docs.unity3d.com/Manual/upm-ui-actions)。

有关如何设置可编程渲染管线的更多信息，请参阅 [HDRP 入门](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/getting-started-in-hdrp)或 [URP 入门](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest/manual/InstallingAndConfiguringURP)。

![](../uploads/doc_img/ShaderGraph/install.png)

## 创建新图表

在 **Project Browser（项目浏览器）**中创建新的[着色器图形资产](https://docs.unity.cn/cn/Packages-cn/com.unity.shadergraph@latest/manual/Shader-Graph-Asset)。**Create -> Shader Graph** 将显示各种创建选项。

**Blank Shader Graph** 将创建一个没有选定活动目标或块节点的 Shader Graph 。您需要通过 [Graph Settings Menu](https://docs.unity.cn/cn/Packages-cn/com.unity.shadergraph@latest/manual/Graph-Settings-Tab) 选择一个目标才能继续。

某些集成（如 渲染管道）也可以为 Shader Graphs 提供预配置选项。在本示例中，打开并创建了 **URP -> Lit Shader Graph** 。具体窗口界面介绍请查阅 [Shadow Graph Window](https://docs.unity.cn/cn/Packages-cn/com.unity.shadergraph@latest/manual/Shader-Graph-Window)。

## 创建新节点

有两种方法可以打开 **Create Node （创建节点**） 菜单创建新节点：

1. 右键单击，然后从上下文菜单中选择 **Create Node** 。
2. 按空格键。

在菜单中，您可以在搜索栏中键入内容以查找特定节点，或浏览库中的所有节点。在此示例中，我们将创建一个 Color 节点。首先，在 **Create Node** 菜单的搜索栏中键入 “color”。然后，单击 **Color** ，或高亮显示 **Color** 并按 Enter 键创建节点。

![](../uploads/doc_img/ShaderGraph/createNode.png)

## 连接节点

要构建图形，您需要将节点连接在一起。为此，请单击节点的 **Output Slot**，然后将该连接拖动到另一个节点的 **Input Slot** 中。

首先，将 Color 节点连接到 Fragment Stack 的 **Base Color** 块。

![](../uploads/doc_img/ShaderGraph/baseColor.png)

## 更改节点输出

请注意，该连接更新了主预览，**主预览**中的 3D 对象现在是黑色的，这是在 Color 节点中指定的颜色。您可以单击该节点中的颜色条，然后使用颜色选取器更改颜色。对节点所做的任何更改都会实时更新 **Main Preview** 中的对象。

例如，如果选择红色，则 3D 对象会立即反映此更改。

![](../uploads/doc_img/ShaderGraph/changeRed.png)

## 保存图表

目前，Shader Graph 不会自动保存。需要您手动保存：

- 单击窗口左上角的 **Save Asset** 按钮，或快捷键 Ctrl + S / command + S。
- 关闭图表。如果 Unity 检测到任何未保存的更改，则会显示一个弹出窗口，询问是否要保存这些更改。

## 创建材质

创建新材质并为其分配 Shader Graph 着色器的过程与常规着色器的过程相同。

在主菜单或 Project View 上下文菜单中，选择 **Assets -> Create -> Material**。选择您刚刚创建的材质，在其 Inspector 窗口中，选择 **Shader** 下拉菜单，单击 **Shader Graphs**，然后选择要应用于材质的 Shader Graph 着色器。

您也可以右键单击 Shader Graph 着色器，然后选择 **Create -> Material**。此方法会自动将该 Shader Graph 着色器分配给新创建的材质。

![](../uploads/doc_img/ShaderGraph/newMaterial.png)

材质也会自动生成为 Shader Graph 的子资产。您可以将其直接指定给场景中的对象。在 Shader Graph 上修改 Blackboard 中的属性将实时更新此材质，从而允许在场景中快速可视化。

## 将材质放入场景中

现在，您已将着色器分配给材质，可以将其应用于场景中的对象。将 Material 拖放到 Scene 中的对象上。或者，在对象的 Inspector 窗口中，找到 **Mesh Renderer -> Materials**，然后将材质应用于**元素**。

![](../uploads/doc_img/ShaderGraph/applyToElement.png)

## 使用属性编辑图形

您还可以使用属性来更改着色器的外观。Properties （属性） 是从材质的 Inspector 中可见的选项，它允许其他人更改着色器中的设置，而无需打开 Shader Graph 。

要创建新属性，请使用 Blackboard 右上角的**添加 （+）** 按钮，然后选择要创建的属性类型。在此示例中，我们将选择 **Color** 。

![](../uploads/doc_img/ShaderGraph/newProperties.png)

当选择该属性时，这会在 Blackboard 中添加一个新属性，并在 Graph Inspector 的 **Node Settings （节点设置**） 选项卡中使用以下选项。

![](../uploads/doc_img/ShaderGraph/setProperties.png)

| Option        | Description                                                  |
| ------------- | ------------------------------------------------------------ |
| **Name**      | 该节点及出现在Inspector中的名称                              |
| **Reference** | 出现在 C# 脚本中的属性名称。要更改 Reference name ，请输入新字符串。 |
| **Default**   | 属性的默认值。                                               |
| **Mode**      | 属性的模式。每个属性都有不同的模式。对于 Color ，您可以选择 Default 或 HDR 。 |
| **Percision** | 默认的精度的财产。                                           |
| **Exposed**   | 启用此复选框可使属性在 Material 的 Inspector 中可见。        |

创建属性后，有两种方法可以在图表中引用属性：

1. 将属性从 Blackboard 拖动到图表上。
2. 右键单击并选择 **Create Node** 。该属性列在 **Properties** 类别中。

![](../uploads/doc_img/ShaderGraph/useProperties.png)

尝试将属性连接到 **Base Color** 块。由于默认值是黑色，预览对象立即变为黑色。

![](../uploads/doc_img/ShaderGraph/linkProperties.png)

保存您的图表，然后返回到 Material 的 Inspector。该属性现在显示在 Inspector 中。您在 Inspector 中对属性所做的任何更改都会影响使用此材质的所有对象。

![](../uploads/doc_img/ShaderGraph/useColor.png)