# URP 图形设置升级

本页提供了 Universal Render Pipeline (URP) 图形 [Quality Level](xref:class-QualitySettings) 设置的建议值，分别适用于 **Low** 和 **High** 质量级别。这些设置大致匹配了内置渲染管线中等效的 **Low** 和 **High** 默认预设。

URP 更改了许多特性和设置的实现，因此它们对性能的影响通常与内置渲染管线的等效项不同。当你将项目从内置渲染管线升级到 URP 时，现有的质量级别可能会提供不同的性能水平，你可能需要更新或为项目创建新的质量级别。你可以将本页中的值作为起点。

本页分为以下几个部分：

* [项目设置](#project-settings)
* [URP 资源](#urp-asset)

> **注意**：在 URP 中，许多质量级别设置已从项目设置窗口转移到 URP 资源中。有关在 URP 项目中找到这些设置的位置，请参考 [Built-In Render Pipeline 质量设置参考](./birp-quality-settings-location.md)。

## 项目设置

你可以在 [项目设置](xref:class-QualitySettings) 中更改以下设置，路径为 **项目设置** > **质量**。

| **设置 (Setting)** | **"Low" 预设值 ("Low" Preset Value)** | **"High" 预设值 ("High" Preset Value)** |
| ------------------ | ---------------------------------- | ----------------------------------- |
| **渲染 (Rendering)** |                                    |                                     |
| 实时反射探针 (Real-time Reflection Probes) | 否 (No) | 是 (Yes) |
| 分辨率缩放固定 DPI 因子 (Resolution Scaling Fixed DPI Factor) | 1 | 1 |
| 垂直同步计数 (VSync Count) | 不同步 (Don't Sync) | 每垂直同步 (Every V Blank) |
| **纹理 (Textures)** |                                    |                                     |
| 全局 Mipmap 限制 (Global Mipmap Limit) | 半分辨率 (Half Resolution) | 全分辨率 (Full Resolution) |
| 各向异性纹理 (Anisotropic Textures) | 禁用 (Disabled) | 禁用 (Disabled) |
| 纹理流式加载 (Texture Streaming) | 否 (No) | 否 (No) |
| **粒子 (Particles)** |                                    |                                     |
| 粒子射线投射预算 (Particle Raycast Budget) | 16 | 256 |
| **地形 (Terrain)** |                                    |                                     |
| 广告牌面朝相机位置 (Billboards Face Camera Position) | 否 (No) | 是 (Yes) |
| **阴影 (Shadows)** |                                    |                                     |
| 阴影掩膜模式 (Shadowmask Mode) | 阴影掩膜 (Shadowmask) | 距离阴影掩膜 (Distance Shadowmask) |
| **异步资源上传 (Async Asset Upload)** |                                    |                                     |
| 时间片 (Time Slice) | 2 | 2 |
| 缓冲区大小 (Buffer Size) | 16 | 16 |
| 持久缓冲区 (Persistent Buffer) | 是 (Yes) | 是 (Yes) |
| **细节层级 (Level of Detail)** |                                    |                                     |
| LOD 偏差 (LOD Bias) | 0.4 | 1 |
| 最大 LOD 层级 (Maximum LOD level) | 0 | 0 |
| **网格 (Meshes)** |                                    |                                     |
| 骨骼权重 (Skin Weights) | 4 骨骼 (4 Bones) | 无限 (Unlimited) |

## URP 资源

你可以在任何 [URP 资源](./../universalrp-asset.md) 中更改以下设置。

| **设置 (Setting)** | **"Low" 预设值 ("Low" Preset Value)** | **"High" 预设值 ("High" Preset Value)** |
| ------------------ | ---------------------------------- | ----------------------------------- |
| **渲染 (Rendering)** |                                    |                                     |
| 深度纹理 (Depth Texture) | 否 (No) | 否 (No) |
| 不透明纹理 (Opaque Texture) | 否 (No) | 否 (No) |
| 地形孔洞 (Terrain Holes) | 是 (Yes) | 是 (Yes) |
| **质量 (Quality)** |                                    |                                     |
| HDR | 是 (Yes) | 是 (Yes) |
| 抗锯齿 (MSAA) (Anti Aliasing (MSAA)) | 禁用 (Disabled) | 2x |
| 渲染比例 (Render Scale) | 1 | 1 |
| **光照 (Lighting)** |                                    |                                     |
| 主光源 (Main Light) | 每像素 (Per Pixel) | 每像素 (Per Pixel) |
| &nbsp;&nbsp;&nbsp;&nbsp;投射阴影 (Cast Shadows) | 否 (No) | 是 (Yes) |
| &nbsp;&nbsp;&nbsp;&nbsp;阴影分辨率 (Shadows Resolution) | 不适用 (N/A) | 2048 |
| 附加光源 (Additional Lights) | 禁用 (Disabled) | 每像素 (Per Pixel) |
| &nbsp;&nbsp;&nbsp;&nbsp;每个对象限制 (Per Object Limit) | 不适用 (N/A) | 4 |
| &nbsp;&nbsp;&nbsp;&nbsp;投射阴影 (Cast Shadows) | 不适用 (N/A) | 是 (Yes) |
| &nbsp;&nbsp;&nbsp;&nbsp;阴影贴图分辨率 (Shadow Atlas Resolution) | 不适用 (N/A) | 2048 |
| &nbsp;&nbsp;&nbsp;&nbsp;阴影分辨率级别 (Shadow Resolution Tiers) | 不适用 (N/A) |  |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;低 (Low) | 不适用 (N/A) | 512 |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;中 (Medium) | 不适用 (N/A) | 1024 |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;高 (High) | 不适用 (N/A) | 2048 |
| &nbsp;&nbsp;&nbsp;&nbsp;Cookie 贴图分辨率 (Cookie Atlas Resolution) | 不适用 (N/A) | 2048 |
| &nbsp;&nbsp;&nbsp;&nbsp;Cookie 贴图格式 (Cookie Atlas Format) | 不适用 (N/A) | 高颜色 (Color High) |
| 反射探针 (Reflection Probes) |                                    |                                     |
| &nbsp;&nbsp;&nbsp;&nbsp;探针混合 (Probe Blending) | 否 (No) | 是 (Yes) |
| &nbsp;&nbsp;&nbsp;&nbsp;盒子投影 (Box Projection) | 否 (No) | 否 (No) |
| **阴影 (Shadows)** |                                    |                                     |
| 最大距离 (Max Distance) | 不适用 (N/A) | 50 |
| 级联数量 (Cascade Count) | 不适用 (N/A) | 3 |
| &nbsp;&nbsp;&nbsp;&nbsp;第 1 分割 (Split 1) | 不适用 (N/A) | 12.5 |
| &nbsp;&nbsp;&nbsp;&nbsp;第 2 分割 (Split 2) | 不适用 (N/A) | 33.8 |
| &nbsp;&nbsp;&nbsp;&nbsp;最后边界 (Last Border) | 不适用 (N/A) | 3.8 |
| 工作单位 (Working Unit) | 不适用 (N/A) | 公制 (Metric) |
| 深度偏差 (Depth Bias) | 不适用 (N/A) | 1 |
| 法线偏差 (Normal Bias) | 不适用 (N/A) | 1 |
| 柔和阴影 (Soft Shadows) | 不适用 (N/A) | 是 (Yes) |
| **后处理 (Post-processing)** |                                    |                                     |
| 色调映射模式 (Grading Mode) | 低动态范围 (Low Dynamic Range) | 低动态范围 (Low Dynamic Range) |
| LUT 大小 (LUT Size) | 16 | 32 |
| 快速 sRGB/线性转换 (Fast sRGB/Linear Conversion) | 否 (No) | 否 (No) |

## 其他资源

* [查找 URP 中的质量设置](./quality-settings-location.md)
* [URP 资源](./../universalrp-asset.md)
