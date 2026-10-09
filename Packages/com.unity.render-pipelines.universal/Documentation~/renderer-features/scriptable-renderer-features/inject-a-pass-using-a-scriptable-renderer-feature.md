# 通过 Scriptable Renderer Feature 注入 Pass

本节介绍如何为 URP 渲染器创建一个 [Scriptable Renderer Feature](intro-to-scriptable-renderer-features.md)。Scriptable Renderer Feature 在每帧都会加入一个 `ScriptableRenderPass` 实例。

您需要先 [编写一个 Scriptable Render Pass](../write-a-scriptable-render-pass.md)。

本教程包含以下部分：

- [通过 Scriptable Renderer Feature 注入 Pass](#通过-scriptable-renderer-feature-注入-pass)
  - [创建 Scriptable Renderer Feature](#创建-scriptable-renderer-feature)
  - [将 Renderer Feature 添加到 Universal Renderer 资源](#将-renderer-feature-添加到-universal-renderer-资源)
  - [在自定义 Renderer Feature 中加入 Render Pass](#在自定义-renderer-feature-中加入-render-pass)
  - [自定义 Renderer Feature 代码](#自定义-renderer-feature-代码)

## <a name="scriptable-renderer-feature"></a>创建 Scriptable Renderer Feature

1. 创建一个新的 C# 脚本，命名为 `MyRendererFeature.cs`。

2. 在脚本中删除 Unity 自动生成的 `MyRendererFeature` 类的代码。

3. 添加以下 `using` 指令：

    ```C#
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
    ```

4. 创建 `MyRendererFeature` 类，并继承 **ScriptableRendererFeature**：

    ```C#
    public class MyRendererFeature : ScriptableRendererFeature    
    ```

5. 在 `MyRendererFeature` 类中实现以下方法：

    * `Create`：Unity 在以下情况下调用此方法：
        * 渲染器特性首次加载时。
        * 启用或禁用渲染器特性时。
        * 在 Inspector 面板中更改 Renderer Feature 的属性时。

    * `AddRenderPasses`：Unity 每帧为每个相机调用此方法。此方法用于向 Scriptable Renderer 注入 `ScriptableRenderPass` 实例。

至此，您已经创建了一个自定义 `MyRendererFeature` Renderer Feature，并定义了其主要方法。

完整代码如下：

```C#
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MyRendererFeature : ScriptableRendererFeature
{
    public override void Create()
    {

    }

    public override void AddRenderPasses(ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {

    }
}
```

## <a name="add-renderer-feature-to-asset"></a>将 Renderer Feature 添加到 Universal Renderer 资源

将创建的 Renderer Feature 添加到 Universal Renderer 资源。有关如何执行此操作的详细信息，请参阅 [如何向 Renderer 添加 Renderer Feature](../../urp-renderer-feature-how-to-add.md)。

## <a name="enqueue-the-render-pass-in-the-custom-renderer-feature"></a>在自定义 Renderer Feature 中加入 Render Pass

本节介绍如何在 `MyRendererFeature` 类的 `Create` 方法中实例化 Render Pass，并在 `AddRenderPasses` 方法中将其加入队列。

本节使用 [编写 Scriptable Render Pass](../write-a-scriptable-render-pass.md) 页面中的 `RedTintRenderPass` 示例。

1. 声明以下字段：

    ```C#
    [SerializeField] private Shader shader;
    private Material material;
    private RedTintRenderPass redTintRenderPass;
    ```

2. 在 `Create` 方法中实例化 `RedTintRenderPass` 类。

    在方法中使用 `renderPassEvent` 字段指定执行 Render Pass 的时机。

    ```C#
    public override void Create()
    {
        if (shader == null)
        {
            return;
        }
        material = CoreUtils.CreateEngineMaterial(shader);
        redTintRenderPass = new RedTintRenderPass(material);

        redTintRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }
    ```

3. 在 `AddRenderPasses` 方法中，使用 `EnqueuePass` 方法将 Render Pass 加入队列。

    ```C#
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(redTintRenderPass);
        }
    }
    ```

## <a name="code-renderer-feature"></a>自定义 Renderer Feature 代码

以下是完整的 `MyRendererFeature` 脚本代码：

```C#
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MyRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;
    private Material material;
    private RedTintRenderPass redTintRenderPass;

    public override void Create()
    {
        if (shader == null)
        {
            Debug.LogError("Ensure that you've set a shader in the Scriptable Renderer Feature.");
            return;
        }
        material = CoreUtils.CreateEngineMaterial(shader);
        redTintRenderPass = new RedTintRenderPass(material);
        
        redTintRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (redTintRenderPass != null &&
            renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(redTintRenderPass);
        }
    }
    
    public override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(material);
    }
}
```