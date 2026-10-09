# URP 中的自定义渲染 Pass 工作流

自定义渲染 Pass 允许修改通用渲染管线 (URP) 渲染场景或场景内对象的方式。自定义渲染 Pass 包含自定义的渲染代码，并可在特定的注入点添加到渲染管线中。

要添加自定义渲染 Pass，请完成以下任务：

- [编写代码](#create-code)：使用 Scriptable Render Pass API 创建自定义渲染 Pass。
- [注入渲染 Pass](#inject-pass)：使用 `RenderPipelineManager` API 进行注入，或[创建 Scriptable Renderer Feature](#create-srf) 并将其添加到 URP 渲染器中。

## <a name="create-code"></a>编写自定义渲染 Pass 代码

使用 `ScriptableRenderPass` 创建自定义渲染 Pass 代码。

更多信息请参考 [编写 Scriptable Render Pass](write-a-scriptable-render-pass.md)。

## <a name="inject-pass"></a>使用 RenderPipelineManager API 注入自定义渲染 Pass

Unity 在每帧渲染每个活动相机之前会触发 [beginCameraRendering](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/Rendering.RenderPipelineManager-beginCameraRendering.html) 事件。可以订阅该事件，并在 Unity 渲染相机之前执行自定义渲染 Pass。

更多信息请参考 [通过脚本注入渲染 Pass](../customize/inject-render-pass-via-script.md)。

## <a name="create-srf"></a>创建 Scriptable Renderer Feature

Scriptable Renderer Feature 控制 Scriptable Render Pass 何时以及如何应用于特定的渲染器或相机，并可以同时管理多个 Scriptable Render Pass。

创建 Scriptable Renderer Feature 需要完成以下步骤：

* 使用 API 创建 Scriptable Renderer Feature。
* 将 Scriptable Renderer Feature 添加到 Universal Renderer 资源，使其包含在渲染管线中。
* 在 Scriptable Renderer Feature 中入队自定义渲染 Pass。

更多信息请参考 [使用 Scriptable Renderer Feature 注入渲染 Pass](scriptable-renderer-features/inject-a-pass-using-a-scriptable-renderer-feature.md)。