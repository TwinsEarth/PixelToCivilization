# Scriptable Renderer Feature 参考指南

在使用 Scriptable Renderer Feature 和 Scriptable Render Pass 时，需要实现一些预定义的方法，使 URP 能够在渲染管线的特定阶段调用它们。

以下内容总结了编写 Scriptable Renderer Feature 和 Scriptable Render Pass 的常见方法：

- [Scriptable Renderer Feature 参考指南](#scriptable-renderer-feature-参考指南)
  - [Scriptable Renderer Feature 方法](#scriptable-renderer-feature-方法)
  - [Scriptable Render Pass 方法](#scriptable-render-pass-方法)
  - [其他资源](#其他资源)

## <a name="scriptable-renderer-feature-methods"></a>Scriptable Renderer Feature 方法

在 Scriptable Renderer Feature 中，可以使用以下方法来处理其核心功能。有关脚本编写的更多详细信息，请参阅 [ScriptableRendererFeature](xref:UnityEngine.Rendering.Universal.ScriptableRendererFeature)。

| **方法** | **描述** |
| -------- | -------- |
| `AddRenderPasses` | 通过 `EnqueuePass` 方法向渲染序列中添加一个或多个 Render Pass。<br/><br/>默认情况下，该方法会将 Render Pass 应用于所有相机。如需修改此行为，可以在检测到特定相机或相机类型时提前返回。<br/><br/>**注意**：URP 在每个相机设置渲染器时都会调用此方法一次，因此不应在该方法内创建或实例化任何资源。 |
| `Create` | 用于初始化 Scriptable Renderer Feature 需要的资源，例如材质（Material）和 Render Pass 实例。 |
| `Dispose` | 用于清理 Scriptable Renderer Feature 分配的资源，例如材质。 |
| `SetupRenderPasses` | 负责对 Scriptable Render Pass 进行初始化，例如设置初始属性值或调用自定义设置方法。<br/><br/>如果 Scriptable Renderer Feature 需要访问相机目标（camera targets）来配置 Scriptable Render Pass，应在此方法中完成，而不是在 `AddRenderPasses` 方法中。 |

## <a name="scriptable-render-pass-methods"></a>Scriptable Render Pass 方法

在 Scriptable Render Pass 中，可以使用以下方法来处理其核心功能。有关脚本编写的更多详细信息，请参阅 [ScriptableRenderPass](xref:UnityEngine.Rendering.Universal.ScriptableRenderPass)。

| **方法** | **描述** |
| -------- | -------- |
| `Execute` | 实现 Scriptable Renderer Feature 的渲染逻辑。<br/><br/>**注意**：不得在 URP 提供的命令缓冲区（Command Buffer）上调用 `ScriptableRenderContext.Submit`，因为渲染管线会在特定时间点处理提交操作。 |
| `OnCameraCleanup` | 释放在 Render Pass 期间分配的资源。 |
| `OnCameraSetup` | 配置渲染目标及其清除状态，并创建临时渲染目标纹理。<br/><br/>**注意**：如果该方法为空，则 Render Pass 会渲染到当前相机的渲染目标。 |

## 其他资源

* [Scriptable Renderer Feature 介绍](./intro-to-scriptable-renderer-features.md)
* [Scriptable Render Pass 介绍](./intro-to-scriptable-render-passes.md)
* [如何创建自定义 Renderer Feature](../create-custom-renderer-feature.md)
