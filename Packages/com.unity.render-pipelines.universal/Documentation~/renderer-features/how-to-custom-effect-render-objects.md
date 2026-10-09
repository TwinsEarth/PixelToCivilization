# 示例：如何使用 Render Objects Renderer Feature 创建自定义渲染效果

URP 在 **DrawOpaqueObjects** 和 **DrawTransparentObjects** Pass 中绘制对象。有时，您可能需要在帧渲染的不同阶段绘制对象，或以不同方式处理和写入渲染数据（如深度和模板缓冲）。[Render Objects Renderer Feature](renderer-feature-render-objects.md) 允许您通过特定的层、特定的时间点和特定的覆盖选项来自定义渲染。

本示例介绍如何使用 Render Objects Renderer Feature 创建自定义渲染效果。

## 示例概述

本示例实现以下效果：

* 场景中有一个角色。

    ![角色](../Images/how-to-render-objects/character.png)

* 当角色被其他 GameObject 遮挡时，Unity 使用不同的材质绘制角色轮廓。

    ![角色被遮挡](../Images/how-to-render-objects/character-goes-behind-object.gif)

## 前提条件

本示例需要以下条件：

* 一个已安装 URP 包的 Unity 项目。

* **Project Settings** > **Graphics** > **Scriptable Render Pipeline Settings** 指向 URP 资源。

## <a name="example-objects"></a>创建示例场景和 GameObject

按照以下步骤创建用于本示例的场景：

1. 创建一个 **Cube**，调整 **Scale** 使其看起来像一堵墙。

    ![墙壁](../Images/how-to-render-objects/rendobj-cube-wall.png)

2. 创建一个材质，并使用 `Universal Render Pipeline/Lit` Shader。设置基础颜色（例如红色），命名为 `Character`。

3. 创建一个基本角色并赋予 `Character` 材质。在本示例中，角色由三个胶囊体组成：中间较大的胶囊体表示身体，两个较小的胶囊体表示手臂。

    ![角色结构](../Images/how-to-render-objects/character-views-side-top-persp.png)

    为了便于操作，将三个胶囊体作为子 GameObject 归属到 **Character** GameObject 下。

    ![角色层级结构](../Images/how-to-render-objects/character-in-hierarchy.png)

4. 创建一个新的材质，并使用 `Universal Render Pipeline/Unlit` Shader。设置基础颜色（例如蓝色），命名为 `CharacterBehindObjects`。该材质用于当角色被遮挡时的渲染。

现在，场景已准备就绪，可以按照本示例的步骤进行实现。

## 示例实现

本节假设您已经按照 [创建示例场景和 GameObject](#example-objects) 章节搭建了场景。

本示例使用两个 Render Objects Renderer Feature：一个用于绘制被遮挡的角色部分，另一个用于绘制未被遮挡的角色部分。

### 创建 Renderer Feature 以绘制被遮挡的角色

按照以下步骤创建 Renderer Feature 以绘制被其他 GameObject 遮挡的角色部分。

1. 选择一个 URP Renderer。

    ![选择 URP Renderer](../Images/how-to-render-objects/rendobj-select-urp-renderer.png)

2. 在 **Inspector** 面板中，点击 **Add Renderer Feature**，选择 **Render Objects**。

    ![添加 Render Object Renderer Feature](../Images/how-to-render-objects/rendobj-add-rend-obj.png)

    在 **Name** 字段中输入 `DrawCharacterBehind` 作为新的 Renderer Feature 名称。

3. 本示例使用 **Layer** 来过滤需要渲染的 GameObject。创建一个新的 Layer 并命名为 `Character`。

    ![创建 Character Layer](../Images/how-to-render-objects/rendobj-new-layer-character.png)

4. 选择 **Character** GameObject，将其分配到 `Character` Layer。在 **Inspector** 面板的 **Layer** 下拉列表中选择 `Character`。

    ![将 Character 赋予 Character Layer](../Images/how-to-render-objects/rendobj-assign-character-gameobject-layer.png)

5. 在 `DrawCharacterBehind` Renderer Feature 的 **Filters** > **Layer Mask** 选项中选择 `Character`，确保此 Renderer Feature 仅渲染 `Character` 层的 GameObject。

6. 在 **Overrides** > **Material** 中选择 `CharacterBehindObjects` 材质，以在角色被遮挡时覆盖原材质。

    ![Layer Mask 和材质覆盖](../Images/how-to-render-objects/rendobj-change-layer-override-material.png)

7. 设置 **Depth** 选项，使角色仅在被遮挡时才渲染。勾选 **Depth** 选项，并将 **Depth Test** 设置为 **Greater**。

    ![设置 Depth Test 为 Greater](../Images/how-to-render-objects/rendobj-depth-greater.png)

此时，Unity 仅在角色被遮挡时使用 `CharacterBehindObjects` 材质进行渲染。但由于角色本身的不同部分可能相互遮挡，部分区域可能错误地显示 `CharacterBehindObjects` 材质。

![角色的自遮挡问题](../Images/how-to-render-objects/character-depth-test-greater.gif)

### 解决角色自遮挡问题

自遮挡问题的原因如下：

1. 在 URP 的 **Opaque** 渲染 Pass 中，Unity 使用 `Character` 材质绘制角色，并将深度值写入深度缓冲区。而 `DrawCharacterBehind` Renderer Feature 在 `AfterRenderingOpaques` 事件之后执行，因此会基于当前的深度缓冲进行测试。

2. 当 `DrawCharacterBehind` 执行时，Unity 根据 **Depth Test** 选项进行深度测试，导致角色自身某些部分被错误地替换为 `CharacterBehindObjects` 材质。

    ![角色自遮挡问题示意图](../Images/how-to-render-objects/rendobj-depth-greater-see-through.png)

为了解决这个问题：

1. 在 **URP Asset** 的 **Filtering** > **Opaque Layer Mask** 选项中，取消 `Character` 层的勾选。

    ![从 Opaque Layer Mask 中移除 Character 层](../Images/how-to-render-objects/rendobj-in-urp-asset-clear-character.png)

    这样，默认的 Opaque Pass 不会渲染角色，避免了错误的深度写入。

    ![角色不会被默认 Opaque Pass 渲染](../Images/how-to-render-objects/rendobj-character-only-behind.png)

2. 添加一个新的 Render Objects Renderer Feature，命名为 `Character`。

3. 在 `Character` Renderer Feature 中，**Filters** > **Layer Mask** 选择 `Character` 层。

    ![设置 Layer Mask 过滤 Character 层](../Images/how-to-render-objects/rendobj-render-objects-character.png)

    这样 Unity 在 `AfterRenderingOpaques` 事件中渲染角色时，不会被 `DrawCharacterBehind` 影响。

4. 在 `DrawCharacterBehind` Renderer Feature 的 **Overrides** > **Depth** 选项中，取消勾选 **Write Depth**。这样 `DrawCharacterBehind` 不会更改深度缓冲区，避免影响后续 `Character` Renderer Feature 的渲染。

    ![取消 Write Depth 选项](../Images/how-to-render-objects/rendobj-render-objects-no-write-depth.png)

此时，最终效果如下：

* 当角色在前方时，使用 `Character` 材质渲染。
* 当角色被遮挡时，使用 `CharacterBehindObjects` 材质渲染轮廓。

![最终效果](../Images/how-to-render-objects/character-goes-behind-object.gif)

完整的渲染顺序如下：

1. URP Renderer 在 `BeforeRenderingOpaques` 事件中跳过 `Character` 层对象的渲染。
2. `DrawCharacterBehind` Renderer Feature 在 `AfterRenderingOpaques` 事件中绘制被遮挡的角色部分。
3. `Character` Renderer Feature 在 `AfterRenderingOpaques` 事件中绘制未被遮挡的角色部分，修复自遮挡问题。