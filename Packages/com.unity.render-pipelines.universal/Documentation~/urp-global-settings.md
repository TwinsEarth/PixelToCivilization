# URP 全局设置

如果项目安装了URP包，Unity将在“项目设置”窗口的“图形”标签中显示URP全局设置部分。

URP全局设置部分允许您定义项目范围的URP设置。

![URP Settings Window](Images/Inspectors/global-settings.png)

该部分包含以下设置。

## 渲染层（3D）

使用此部分定义渲染层的名称。渲染层仅与3D渲染器一起工作。

## 着色器剥离

此部分中的复选框定义了Unity在构建Player时剥离哪些着色器变体。

| **属性**                    | **描述**                                                    |
| ----------------------------| ------------------------------------------------------------ |
| **Shader Variant Log Level** | 选择Unity在构建Unity项目时保存关于着色器变体的哪些信息。<br/>选项：<br/>• Disabled: Unity不保存任何着色器变体信息。<br/>• Only SRP Shaders: Unity仅保存URP着色器的着色器变体信息。<br/>• All Shaders: Unity保存每种类型的着色器变体信息。 |
| **Strip Debug Variants**     | 启用时，Unity在构建Player时剥离所有调试视图着色器变体。这可以减少构建时间，但会阻止在Player构建中使用渲染调试器。 |
| **Strip Unused Post Processing Variants** | 启用时，Unity假定Player在运行时不会创建新的[Volume Profiles](VolumeProfile.md)。基于这个假设，Unity只保留现有[Volume Profiles](VolumeProfile.md)使用的着色器变体，并剥离所有其他变体。即使项目中的场景未使用这些Profiles，Unity也会保留在Volume Profiles中使用的着色器变体。 |
| **Strip Unused Variants**   | 启用时，Unity以更高效的方式进行着色器剥离。如果项目使用以下URP功能，启用此选项可将Player中的着色器变体数量减少一半：<ul><li>渲染层</li><li>原生渲染通道</li><li>反射探针混合</li><li>反射探针盒子投影</li><li>SSAO渲染器功能</li><li>Decal渲染器功能</li><li>某些后处理效果</li></ul>只有在Player中遇到问题时，才禁用此选项。 |
| **Strip Screen Coord Override Variants** | 启用时，Unity在Player构建中剥离屏幕坐标覆盖着色器变体。 |
