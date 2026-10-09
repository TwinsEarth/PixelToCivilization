# Universal Additional Light Data 组件

[Universal Additional Light Data](xref:UnityEngine.Rendering.Universal.UniversalAdditionalLightData) 组件是 Universal Render Pipeline (URP) 用于内部数据存储的组件。该组件允许 URP 扩展并覆盖 Unity 标准 Light 组件的功能。

在 URP 中，任何具有 Light 组件的 GameObject 必须同时具有 Universal Additional Light Data 组件。如果你的项目使用 URP，Unity 在你创建 Light GameObject 时会自动添加 Universal Additional Light Data 组件。你不能从 Light GameObject 中删除这个组件。
