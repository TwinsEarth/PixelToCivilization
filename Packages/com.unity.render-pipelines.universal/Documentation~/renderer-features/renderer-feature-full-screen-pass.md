# 全屏 Pass Renderer Feature 参考

有关如何使用全屏 Pass Renderer Feature 创建全屏效果的信息，请参考 [如何创建自定义后处理效果](../post-processing/post-processing-custom-effect-low-code.md)。

## 属性

全屏 Pass Renderer Feature 包含以下属性：

| **属性** | **描述** |
| -------- | ----------- |
| **Name** | 全屏 Pass Renderer Feature 的名称。 |
| **Pass Material** | 渲染该效果的材质。 |
| **Injection Point** | 选择效果渲染的时机：<ul><li>**Before Rendering Transparents**：在天空盒 Pass 之后、透明物体 Pass 之前添加效果。</li><li>**Before Rendering Post Processing**：在透明物体 Pass 之后、后处理 Pass 之前添加效果。</li><li>**After Rendering Post Processing** (默认)：在后处理 Pass 之后、AfterRendering Pass 之前添加效果。</li></ul> |
| **Requirements** | 选择 Renderer Feature 需要的额外 Pass，可选择以下内容：<ul><li>**None**：不添加额外的 Pass。</li><li>**Everything**：添加所有可用的额外 Pass（Depth、Normal、Color 和 Motion）。</li><li>**Depth**：添加深度预 Pass，以启用深度值的使用。</li><li>**Normal**：启用法线向量数据的使用。</li><li>**Color** (默认)：将屏幕的颜色数据复制到 Shader 内的 `_BlitTexture` 纹理。</li><li>**Motion**：启用运动矢量的使用。</li></ul> |
| **Pass Index** | 选择 Pass Material 所使用的 Shader 中的特定 Pass。<br/><br/>该选项默认隐藏，要访问此选项，请在 **Inspector** 面板的 Renderer Feature 区域点击 &#8942;，然后选择 **Show Additional Properties**。 |
