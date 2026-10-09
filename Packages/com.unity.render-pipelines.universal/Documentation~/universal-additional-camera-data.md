# Universal Additional Camera Data 组件

Universal Additional Camera Data 组件是 Universal Render Pipeline (URP) 用于内部数据存储的组件。该组件允许 URP 扩展并覆盖 Unity 标准 Camera 组件的功能和外观。

在 URP 中，任何具有 Camera 组件的 GameObject 必须同时具有 Universal Additional Camera Data 组件。如果你的项目使用 URP，Unity 在你创建 Camera GameObject 时会自动添加 Universal Additional Camera Data 组件。你不能从 Camera GameObject 中删除这个组件。

如果你不使用脚本控制和自定义 URP，那么你不需要对 Universal Additional Camera Data 组件做任何操作。

如果你使用脚本控制和自定义 URP，你可以通过如下方式在脚本中访问 Camera 的 Universal Additional Camera Data 组件：

```c#
UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
```

> [!NOTE]
> 要使用 `GetUniversalAdditionalCameraData()` 方法，你必须使用 `UnityEngine.Rendering.Universal` 命名空间。为此，请在脚本顶部添加以下语句：`using UnityEngine.Rendering.Universal;`。

有关更多信息，请参阅 [UniversalAdditionalCameraData API](xref:UnityEngine.Rendering.Universal.UniversalAdditionalCameraData)。

如果你需要在脚本中频繁访问 Universal Additional Camera Data 组件，建议缓存对该组件的引用，以避免不必要的 CPU 工作。

> [!NOTE]
> 当 Camera 使用预设时，仅支持部分属性，未支持的属性会被隐藏。
