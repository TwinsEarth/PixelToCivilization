# Camera 组件参考

在 **通用渲染管线（URP）** 中，Unity 会根据相机类型在 **Inspector** 中显示不同的 **Camera** 组件属性。要更改相机类型，请选择相应的 [Render Type](camera-types-and-render-type.md)。

**基础（Base）相机** 公开以下属性：

- [Camera 组件参考](#camera-组件参考)
  - [投影（Projection）](#投影projection)
    - [物理相机（Physical Camera）](#物理相机physical-camera)
  - [渲染（Rendering）](#渲染rendering)
  - [相机堆栈（Stack）](#相机堆栈stack)
  - [环境（Environment）](#环境environment)
  - [输出（Output）](#输出output)

**叠加（Overlay）相机** 公开以下属性：

- [Camera 组件参考](#camera-组件参考)
  - [投影（Projection）](#投影projection)
    - [物理相机（Physical Camera）](#物理相机physical-camera)
  - [渲染（Rendering）](#渲染rendering)
  - [相机堆栈（Stack）](#相机堆栈stack)
  - [环境（Environment）](#环境environment)
  - [输出（Output）](#输出output)

<a name="Projection"></a>

## 投影（Projection）

| **属性** | **描述** |
| ------------------------ | :-------------------------------------------------------------------- |
| **Projection** | 控制相机的透视模拟方式。 |
| &#160;&#160;&#160;&#160;**Perspective** | 以透视方式渲染对象，远处物体显得较小。 |
| &#160;&#160;&#160;&#160;**Orthographic** | 以正交方式渲染对象，不受透视影响，所有对象尺寸恒定。 |
| **Field of View Axis** | 设置 Unity 衡量相机视野角度的轴向。<br/><br/>可选项：<ul><li>**Vertical**（垂直）</li><li>**Horizontal**（水平）</li></ul>仅在 **Projection** 设为 **Perspective** 时可见。 |
| **Field of View** | 设置相机的视角宽度（以度为单位），沿所选轴计算。<br/><br/>仅在 **Projection** 设为 **Perspective** 时可见。 |
| **Size** | 设置相机视口的大小。<br/><br/>仅在 **Projection** 设为 **Orthographic** 时可见。 |
| **Clipping Planes** | 设置相机开始和停止渲染的距离范围。 |
| &#160;&#160;&#160;&#160;**Near** | 渲染开始的最近距离（相对于相机）。 |
| &#160;&#160;&#160;&#160;**Far** | 渲染终止的最远距离（相对于相机）。 |
| **Physical Camera** | 在 **Inspector** 中显示附加的物理相机属性，以模拟真实相机的光学特性。物理相机使用 **焦距（Focal Length）**、**传感器尺寸（Sensor Size）** 和 **偏移（Shift）** 计算视场角。<br/><br/>仅在 **Projection** 设为 **Perspective** 时可用。 |

<a name="PhysicalCamera"></a>

### 物理相机（Physical Camera）

启用 **Physical Camera** 后，相机将增加额外的物理属性，以模拟真实相机。更多信息，请参考 [物理相机参考](cameras/physical-camera-reference.md)。

<a name="Rendering"></a>

## 渲染（Rendering）

| **属性** | **描述** |
| -------------------------- | ------------------------------------------------------------ |
| **Renderer** | 选择此相机使用的渲染器（Renderer）。 |
| **Post Processing** | 启用后处理效果。 |
| **Anti-Aliasing** | 选择该相机使用的后处理抗锯齿方法。相机仍然可以使用 **MSAA**（硬件抗锯齿），但如果使用 **TAA**，则无法同时使用 **MSAA**。<br/><br/>可用的抗锯齿选项：<ul><li>**None**：该相机仅处理 **MSAA**，但不进行任何后处理抗锯齿。</li><li>**Fast Approximate Anti-aliasing (FXAA)**：全屏处理，每像素平滑边缘。</li><li>**Subpixel Morphological Anti-aliasing (SMAA)**：检测图像边缘模式，并根据模式混合边界像素。</li><li>**Temporal Anti-aliasing (TAA)**：使用历史帧缓冲区平滑多个帧中的边缘。</li></ul>更多信息，请参考 [URP 中的抗锯齿](anti-aliasing.md)。<br/><br/>仅在 **Render Type** 设为 **Base** 时可见。 |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Quality (SMAA)** | 选择 **SMAA** 的质量。**Low** 和 **High** 之间的资源消耗差异较小。<br/><br/>可选项：<ul><li>**Low**</li><li>**Medium**</li><li>**High**</li></ul>仅在 **Anti-aliasing** 选择 **SMAA** 时可见。 |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Quality (TAA)** | 选择 **TAA** 的质量。<br/><br/>可选项：<ul><li>**Very Low**</li><li>**Low**</li><li>**Medium**</li><li>**High**</li><li>**Very High**</li></ul>仅在 **Anti-aliasing** 选择 **TAA** 时可见。 |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Contrast Adaptive Sharpening** | 启用高质量后处理锐化，以减少 **TAA** 产生的模糊。<br/><br/>如果在 **URP 资源** 中启用了 [AMD FidelityFX Super Resolution（FSR）或 Scalable Temporal Post-Processing（STP）](universalrp-asset.md#quality)，此选项将被覆盖，因为这些功能已包含锐化处理。<br/><br/>仅在 **Anti-aliasing** 选择 **TAA** 时可见。 |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Base Blend Factor** | 设置历史缓冲区与当前帧的混合程度。较高的值可以增强抗锯齿效果，但也可能导致更明显的**重影（Ghosting）**。<br/><br/>仅在 **Anti-aliasing** 选择 **TAA** 并启用 **Show Additional Properties** 时可见。 |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Jitter Scale** | 设置 **TAA** 应用的抖动比例。较低的值可减少可见的闪烁和抖动，但也会降低抗锯齿效果。<br/><br/>仅在 **Anti-aliasing** 选择 **TAA** 并启用 **Show Additional Properties** 时可见。 |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Mip Bias** | 设置渲染时纹理 **mipmap** 选择的偏移量。<br/><br/>正偏移值会使纹理更模糊，而负偏移值会锐化纹理。但降低 **Mip Bias** 可能会影响性能。<br/><br/>**注意**：需要使用带有 **mipmap** 的纹理。<br/><br/>仅在 **Anti-aliasing** 选择 **TAA** 并启用 **Show Additional Properties** 时可见。 |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Variance Clamp Scale** | 设置 Unity 用于查找相邻像素颜色时所使用的颜色范围。当颜色历史无效或不可用时，该值会限制像素颜色的变化程度。<br/><br/>较低的值可以减少重影，但可能会导致更明显的闪烁。较高的值可以减少闪烁，但可能会导致模糊和重影。<br/><br/>仅在 **Anti-aliasing** 选择 **TAA** 并启用 **Show Additional Properties** 时可见。 |
| **Stop NaNs** | 将 **NaN（Not a Number）** 值替换为黑色像素，以防止某些特效出现错误。然而，该过程会消耗较多资源，并对性能产生负面影响。仅当遇到 **NaN** 相关问题无法修复时启用。<br/><br/>该功能在 **后处理 Pass** 开始时执行。必须启用 **Post Processing** 才能使用 **Stop NaNs**。<br/><br/>仅在 **Render Type** 设为 **Base** 时可用。 |
| **Dithering** | 启用 8-bit 抖动，以减少大范围渐变和低光区域的条带效应（Banding）。<br/><br/>仅在 **Render Type** 设为 **Base** 时可见。 |
| **Clear Depth** | 启用该选项可在渲染时清除上一个相机的深度信息。<br/><br/>仅在 **Render Type** 设为 **Overlay** 时可见。 |
| **Render Shadows** | 启用阴影渲染。 |
| **Priority** | **优先级（Priority）** 影响相机的渲染顺序，数值较高的相机会覆盖数值较低的相机。范围：**-100 ~ 100**。<br/><br/>仅在 **Render Type** 设为 **Base** 时可见。 |
| **Opaque Texture** | 控制相机是否创建 **CameraOpaqueTexture**，即渲染视图的副本。<br/><br/>可选项：<ul><li>**Off**：不创建 **CameraOpaqueTexture**。</li><li>**On**：创建 **CameraOpaqueTexture**。</li><li>**Use Pipeline Settings**：由 **Render Pipeline 资源** 确定该设置。</li></ul>仅在 **Render Type** 设为 **Base** 时可见。 |
| **Depth Texture** | 控制相机是否创建 `_CameraDepthTexture`，即渲染深度值的副本。<br/><br/>可选项：<ul><li>**Off**：不创建 **CameraDepthTexture**。</li><li>**On**：创建 **CameraDepthTexture**。</li><li>**Use Pipeline Settings**：由 **Render Pipeline 资源** 确定该设置。</li></ul>**注意**：`_CameraDepthTexture` 在 `AfterRenderingSkybox` 和 `BeforeRenderingTransparents` 事件之间设置。如果使用深度预处理（Depth Prepass），则会在 `BeforeRenderingOpaques` 事件设置。有关渲染循环中的事件顺序，请参考 [注入点（Injection Points）](customize/custom-pass-injection-points.md)。 |
| **Culling Mask** | 选择该相机渲染的 **Layer**（层）。 |
| **Occlusion Culling** | 启用遮挡剔除（Occlusion Culling）。 |


<a name="Stack"></a>
## 相机堆栈（Stack）

> **注意**  
> 该部分仅在 **Render Type** 设为 **Base** 时可用。

相机堆栈允许将多个相机的渲染结果组合在一起。相机堆栈由一个 **Base 相机** 和多个 **Overlay 相机** 组成。

你可以使用 **Stack** 属性向堆栈中添加 **Overlay 相机**，它们会按照堆栈中定义的顺序进行渲染。  
关于如何配置和使用相机堆栈的详细信息，请参考 [设置相机堆栈](camera-stacking.md)。

<a name="Environment"></a>

## 环境（Environment）

| **属性** | **描述** |
| -------------------------- | ------------------------------------------------------------ |
| **Background Type** | 控制相机渲染循环开始时如何初始化颜色缓冲区。<br/>更多信息，请参考 [清除相关文档](cameras-advanced.md#clearing)。<br/><br/>仅在 **Render Type** 设为 **Base** 时可见。 |
| &#160;&#160;&#160;&#160;**Skybox** | 通过清除到 **Skybox** 来初始化颜色缓冲区。如果未找到 **Skybox**，则默认为背景色。 |
| &#160;&#160;&#160;&#160;**Solid Color** | 通过清除到指定的颜色来初始化颜色缓冲区。<br/>如果选择该属性，会显示以下额外选项：<br/>**Background**：相机在渲染前将颜色缓冲区清除为该颜色。 |
| &#160;&#160;&#160;&#160;**Uninitialized** | 不初始化颜色缓冲区。这意味着该 **RenderTarget** 的加载操作将使用 `DontCare`，而不是 `Load` 或 `Clear`。`DontCare` 指定不需要保留该 **RenderTarget** 先前的内容。<br/><br/>仅在优化性能时使用该选项，前提是 **相机** 或 **相机堆栈** 会绘制颜色缓冲区中的所有像素，否则未绘制的像素行为是不确定的。<br/><br/>**注意**：该选项在 **编辑器** 和 **运行时** 可能会导致不同的结果，因为 **编辑器** 不在基于 **TBDR**（Tile-Based Deferred Rendering）的 GPU（如移动设备）上运行。如果在 **TBDR** GPU 上使用该选项，可能会导致未初始化的 **Tile 内存**，从而导致不确定的内容。 |
| **Volumes** | 该部分设置 **Volume** 如何影响当前相机。 |
| &#160;&#160;&#160;&#160;**Update Mode** | 选择 Unity 更新 **Volumes** 的方式。<br/><br/>可选项：<ul><li>**Every Frame**：每帧更新 **Volumes**。</li><li>**Via Scripting**：仅在脚本触发时更新 **Volumes**。</li><li>**Use Pipeline Settings**：使用 **渲染管线** 的默认设置。</li></ul> |
| &#160;&#160;&#160;&#160;**Volume Mask** | 选择该相机影响的 **Layer Mask**，用于确定哪些 **Volumes** 影响该相机。 |
| &#160;&#160;&#160;&#160;**Volume Trigger** | 指定一个 **Transform**，用于确定 **Volume** 系统如何处理该相机的位置。<br/><br/>例如，如果游戏采用 **第三人称视角**，则可以将该属性设置为角色的 **Transform**，这样相机会使用角色进入的 **Volume** 设置。<br/>如果未指定 **Transform**，则相机会使用其自身的 **Transform**。 |

<a name="Output"></a>

## 输出（Output）

> **注意**  
> 仅当 **Render Type** 设为 **Base** 时，该部分才可用。

> **注意**  
> 当相机的 **Render Type** 设为 **Base** 且 **Render Target** 设为 **Texture** 时，Unity **不会** 在 **Inspector** 中显示以下属性：
>
> - **Target Display**
> - **HDR Rendering**
> - **MSAA**
> - **Allow Dynamic Resolution**
>
> 这是因为这些属性由 **Render Texture** 决定，你可以在 **Render Texture 资源** 中进行更改。

| **属性** | **描述** |
| -------------------------- | ------------------------------------------------------------ |
| **Output Texture** | 若指定该字段，则该相机的输出将渲染到 **RenderTexture**，否则渲染到屏幕。 |
| **Target Display** | 选择相机渲染的外部显示设备。 |
| **Target Eye** | 选择该相机的目标视角（用于 **XR 渲染**）。<br/><br/>可选项：<ul><li>**Both**：允许该相机进行 **XR 渲染**。</li><li>**None**：禁用该相机的 **XR 渲染**。</li></ul> |
| **Viewport Rect** | 该相机在屏幕上的绘制区域，由 **Viewport 坐标**（值范围 0-1）表示。 |
| &#160;&#160;&#160;&#160;**X** | Unity 绘制相机视图的水平起始位置。 |
| &#160;&#160;&#160;&#160;**Y** | Unity 绘制相机视图的垂直起始位置。 |
| &#160;&#160;&#160;&#160;**W** | 相机在屏幕上的输出宽度。 |
| &#160;&#160;&#160;&#160;**H** | 相机在屏幕上的输出高度。 |
| **HDR Rendering** | 启用 **高动态范围（HDR）** 渲染。 |
| **MSAA** | 启用 [**多重采样抗锯齿（MSAA）**](anti-aliasing.md#msaa)。 |
| **Allow Dynamic Resolution** | 允许该相机进行 **动态分辨率渲染**。 |