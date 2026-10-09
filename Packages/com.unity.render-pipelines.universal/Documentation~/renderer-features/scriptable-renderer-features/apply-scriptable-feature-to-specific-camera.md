# 将 Scriptable Renderer Feature 应用于特定类型的相机

本指南介绍如何将 Scriptable Renderer Feature 应用于特定类型的相机。

通过这种方法，您可以控制哪些相机会受到 Scriptable Renderer Feature 的影响。这在项目中使用额外的相机（如反射渲染）时尤为重要，因为这些相机可能会因 Scriptable Renderer Feature 而产生意外效果。

您可以在 Scriptable Renderer Feature 脚本中添加逻辑，以在应用效果前检查相机类型。

本指南包含以下部分：

- [将 Scriptable Renderer Feature 应用于特定类型的相机](#将-scriptable-renderer-feature-应用于特定类型的相机)
  - [前提条件 ](#前提条件-)
  - [将 Scriptable Renderer Feature 应用于游戏相机](#将-scriptable-renderer-feature-应用于游戏相机)
  - [其他资源](#其他资源)

## 前提条件 <a name="prerequisites"></a>

本指南假设您已有完整的 Scriptable Renderer Feature。如果没有，请参考 [如何创建自定义 Renderer Feature](../create-custom-renderer-feature.md)。

## <a name="scriptable-renderer-feature-game-camera"></a>将 Scriptable Renderer Feature 应用于游戏相机

此脚本将 Scriptable Renderer Feature 仅应用于特定类型的相机。本示例仅应用于游戏相机（Game Camera）。

1. 打开您要修改的 Scriptable Renderer Feature 的 C# 脚本。
2. 在 `AddRenderPasses` 方法中，添加以下 `if` 语句：

    ```c#
    if (renderingData.cameraData.cameraType == CameraType.Game)
    ```

3. 使用 `EnqueuePass` 方法，将必要的渲染通道添加到渲染器，如下所示：

    ```c#
    if (renderingData.cameraData.cameraType == CameraType.Game)
    {
        renderer.EnqueuePass(yourRenderPass);
    }
    ```

现在，此 Scriptable Renderer Feature 仅适用于 `Game` 类型的相机。

> **注意**: URP 在每帧至少调用 `AddRenderPasses` 方法一次，且对每个相机都会调用。因此，建议尽量减少该方法的复杂度，以避免性能问题。

## 其他资源

* [Scriptable Renderer Feature 介绍](./intro-to-scriptable-renderer-features.md)
* [Scriptable Render Pass 介绍](../intro-to-scriptable-render-passes.md)
* [如何创建自定义 Renderer Feature](../create-custom-renderer-feature.md)
