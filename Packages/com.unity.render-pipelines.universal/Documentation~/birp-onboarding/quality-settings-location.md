# Find graphics quality settings in URP

URP 将其质量设置分为 **Project Settings** 和 **URP Asset**，以便为项目提供更灵活的质量级别。因此，一些在内置渲染管线 (BiRP) 的 **Quality** 部分列出的设置已经移动、改变或不再存在。

以下表格描述了内置渲染管线在 **Project Settings** 的 **Quality** 部分列出的所有设置，以及这些设置在 URP 中的新位置。

| **BiRP Setting** | **URP Setting** |
| ---------------- | --------------- |
| **Rendering** | |
| Render Pipeline Asset | **Project Settings** > **Quality** > **Rendering** > **Render Pipeline Asset** |
| Pixel Light Count | 在 URP 中，每个对象的实时光源最大数量取决于使用的渲染路径。更多信息请参见 [Rendering Path comparison](./../urp-universal-renderer.md#rendering-path-comparison)。<br/><br/>要设置每个对象的光源数量，请使用以下属性：**URP Asset** > **Lighting** > **Additional Lights** > **Per Pixel** > **Per Object Limit** |
| Anti-aliasing | URP 中有两种抗锯齿方式：你可以在 **URP Asset** 中控制多重采样抗锯齿 (MSAA)，并且可以在每个摄像机上控制其他抗锯齿类型。更多信息请参考 [Anti-aliasing in the Universal Render Pipeline](./../anti-aliasing.md)。<br/><br/>要控制 MSAA，请使用以下属性：<br/><br/>**URP Asset** > **Quality** > **Anti-aliasing (MSAA)**<br/><br/>要控制其他类型的抗锯齿，请在每个摄像机上使用以下属性：<br/><br/>**Camera** > **Rendering** > **Anti-aliasing** |
| Real-time Reflection Probes | **Project Settings** > **Quality** > **Rendering** > **Real-time Reflection Probes** |
| Resolution Scaling Fixed DPI Factor | 该属性在 URP 中的位置不变。然而，URP 还支持使用 Upscalers 来处理分辨率缩放，你可以在 **URP Asset** 中配置该选项。有关使用 Upscalers 的更多信息，请参考 [Quality in the URP Asset](./../universalrp-asset.md#quality)。<br/><br/>要设置 Resolution Scaling Fixed DPI Factor，请使用以下属性：<br/><br/>**Project Settings** > **Quality** > **Rendering** > **Resolution Scaling Fixed DPI Factor**<br/><br/>要在 **URP Asset** 中设置分辨率缩放，请使用以下属性：<br/><br/>**URP Asset** > **Quality** > **Render Scale** 和 **Upscaling Filter** |
| VSync Count | **Project Settings** > **Quality** > **Rendering** > **VSync Count** |
| **Textures** | |
| Global Mipmap Limit | **Project Settings** > **Quality** > **Textures** > **Global Mipmap Limit** |
| Anisotropic Textures | **Project Settings** > **Quality** > **Textures** > **Anisotropic Textures** |
| Texture Streaming | **Project Settings** > **Quality** > **Textures** > **Texture Streaming** |
| **Particles** | |
| Soft Particles | 要启用软粒子，请在相关粒子着色器中使用着色器关键字 `_SOFTPARTICLES_ON`。 |
| Particle Raycast Budget | **Project Settings** > **Quality** > **Particles** > **Particle Raycast Budget** |
| **Terrain** | |
| Billboards Face Camera Position | **Project Settings** > **Quality** > **Terrain** > **Billboards Face Camera Position** |
| Use Legacy Details Distribution | **Project Settings** > **Quality** > **Terrain** > **Use Legacy Details Distribution** |
| **Shadows** | |
| Shadowmask Mode | **Project Settings** > **Quality** > **Shadows** > **Shadowmask Mode** |
| Shadows | 在 URP 中，你可以分别启用两种类型的光源阴影。一种是场景中的主光源，另一种是所有其他附加光源。你可以按照需要设置以下属性：<br/><br/>要启用主光源投射的阴影，请使用以下属性：<br/><br/>**URP Asset** > **Lighting** > **Main Light** > **Cast Shadows**<br/><br/>要启用附加光源投射的阴影，请使用以下属性：<br/><br/>**URP Asset** > **Lighting** > **Additional Lights** > **Cast Shadows**<br/><br/>**注意**：启用阴影时，你不再选择阴影类型。要使用软阴影，请启用 **URP Asset** > **Shadows** > **Soft Shadows** 并选择适当的质量级别。 |
| Shadow Resolution | 你可以分别为主光源和附加光源设置阴影分辨率。附加光源使用具有三个级别（低、中、高）的阴影贴图。<br/><br/>要为主光源设置阴影分辨率，请使用以下属性：<br/><br/>**URP Asset** > **Lighting** > **Main Light** > **Shadow Resolution**<br/><br/>要为附加光源设置阴影分辨率，请使用以下属性：<br/><br/>**URP Asset** > **Lighting** > **Additional Lights** > **Shadow Atlas Resolution** 和 **Shadow Resolution Tiers** |
| Shadow Projection | URP 只支持 Stable Fit 阴影投影。 |
| Shadow Distance | **URP Asset** > **Shadows** > **Max Distance** |
| Shadow Near Plane Offset | 没有等效设置，因为 URP 的阴影系统不使用该属性。 |
| Shadow Cascades | **URP Asset** > **Shadows** > **Cascade Count** |
| Cascade Splits | 阴影级联拆分现在由一组基于级联计数的动态属性控制。URP 资源下方显示了级联拆分的可视化表示，每个分段代表给定拆分的大小。<br/><br/>你可以使用以下属性控制每个阴影级联拆分的大小：<br/><br/>**URP Asset** > **Shadows** > **Cascade Count** > **Split 1**、**Split 2**、**Split 3** 和 **Last Border** |
| **Async Asset Upload** | |
| Time Slice | **Project Settings** > **Quality** > **Async Asset Upload** > **Time Slice** |
| Buffer Size | **Project Settings** > **Quality** > **Async Asset Upload** > **Buffer Size** |
| Persistent Buffer | **Project Settings** > **Quality** > **Async Asset Upload** > **Persistent Buffer** |
| **Level of Detail** | |
| LOD Bias | **Project Settings** > **Quality** > **Level of Detail** > **LOD Bias** |
| Maximum LOD level | **Project Settings** > **Quality** > **Level of Detail** > **Maximum LOD Level** |
| LOD Cross Fade | **URP Asset** > **Quality** > **LOD Cross Fade**<br/><br/>**注意**：URP 提供了两种 LOD 交叉淡化选项：Bayer 和 Blue Noise。这些与内置渲染管线使用的抖动方法不同。 |
| **Meshes** | |
| Skin Weights | **Project Settings** > **Quality** > **Meshes** > **Skin Weights** |

## Additional resources

* [URP Quality Presets](./quality-presets.md)
* [URP Asset](./../universalrp-asset.md)
* [Shadows in URP](./../Shadows-in-URP.md)
