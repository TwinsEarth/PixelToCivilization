# <a name="render-objects-renderer-feature"></a>渲染对象渲染特性参考

URP 在 **DrawOpaqueObjects** 和 **DrawTransparentObjects** Pass 期间绘制对象。某些情况下，可能需要在帧渲染的不同阶段绘制对象，或以不同方式处理和写入渲染数据（如深度和模板缓冲）。Render Objects Renderer Feature 允许在特定层、特定时间点，并使用特定的覆盖方式进行自定义渲染。


## 如何使用 Render Objects Renderer Feature

更多信息请参考：[如何使用 Render Objects Renderer Feature](how-to-custom-effect-render-objects.md)。


## 属性

Render Objects Renderer Feature 包含以下属性：

| **属性** | **描述** |
|:-|:-|
| **Name** | 设置该 Renderer Feature 的名称。 |
| **Event** | 选择 Unity 在 URP 渲染队列中执行该 Renderer Feature 的事件。 |
| **Filters** | 设置该 Renderer Feature 需要渲染的对象。 |
| Queue | 选择该 Renderer Feature 渲染不透明对象 (Opaque) 或透明对象 (Transparent)。 |
| Layer Mask | 选择该 Renderer Feature 渲染的对象所在的层 (Layer)。 |
| **Pass Names** | 如果 Shader 中的 Pass 具有 `LightMode` Pass Tag，该 Renderer Feature 仅处理 `LightMode` Pass Tag 值匹配 Pass Names 属性中的 Pass。 |
| **Overrides** | 允许在渲染时覆盖某些属性的设置。 |
| Override Mode | 选择材质覆盖模式。 |
| Material | （Override Mode 设为 Material）渲染对象时，Unity 用该材质替换对象的原始材质，覆盖所有材质属性。 |
| Shader | （Override Mode 设为 Shader）渲染对象时，Unity 用该 Shader 替换对象的材质，但保持材质的所有原始属性。此方法不兼容 SRP Batcher，性能较低。 |
| Depth | 允许自定义该 Renderer Feature 如何影响或使用深度缓冲区，包括：<br/>Write Depth：定义该 Renderer Feature 在渲染对象时是否更新深度缓冲区。<br/>Depth Test：定义该 Renderer Feature 渲染对象像素的深度测试条件。 |
| Stencil | 选中该选项后，该 Renderer Feature 处理模板 (Stencil) 缓冲区的值。<br/>更多信息请参考 [ShaderLab: Stencil](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-Stencil.html)。 |
| Camera | 选中该选项后，可以覆盖以下相机属性：<br/>Field of View：渲染对象时，该 Renderer Feature 使用此视野角度值，而非相机的默认值。<br/>Position Offset：渲染对象时，该 Renderer Feature 使对象相对于原位置进行偏移。<br/>Restore：选中此选项后，Renderer Feature 执行完成后恢复相机的原始矩阵。 |
