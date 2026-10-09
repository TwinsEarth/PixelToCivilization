# URP 包示例

URP Package Samples 是通用渲染管线（URP）的一个[包示例](package-samples.md)。它包含示例 Shader、C# 脚本和其他资源，可用于学习功能的使用方式、进行扩展，或直接在应用程序中使用。有关如何将 URP Package Samples 导入到项目中的信息，请参阅[导入包示例](package-samples.md#importing-package-samples)。

每个示例都使用自己的 [URP 资源](universalrp-asset.md)，因此如果要构建示例场景，请[将示例的 URP 资源添加到图形设置](InstallURPIntoAProject.md#set-urp-active)。否则，Unity 可能会剥离示例使用的 Shader 或渲染通道。

<a name="camera-stacking"></a>
## 相机叠加（Camera Stacking）

`URP Package Samples/CameraStacking` 文件夹包含[相机叠加](camera-stacking.md)的示例。下表描述了该文件夹中的每个相机叠加示例。

| **示例**                 | **描述**                                                     |
| ----------------------- | ------------------------------------------------------------ |
| **混合视野（Mixed field of view）** | `CameraStacking/MixedFOV` 示例演示了如何在第一人称应用程序中使用相机叠加，以防止角色装备的物品与环境发生裁剪。此设置还允许环境相机和装备物品相机使用不同的视野（FOV）。 |
| **分屏（Split screen）** | `CameraStacking/SplitScreenPPUI` 示例展示了如何创建一个每个屏幕都有自己相机叠加的分屏相机设置。此外，它还展示了如何在世界空间和屏幕空间的相机 UI 上应用后处理效果。 |
| **3D 天空盒（3D skybox）** | `CameraStacking/3D Skybox` 示例使用相机叠加将一个微型环境转换为天空盒。一个叠加相机渲染微型城市，另一个渲染微型行星。叠加相机会渲染主相机未覆盖的像素。通过额外的脚本位移处理，该微型环境看起来像是完整尺寸，并显示在主相机视图的背景中。 |


<a name="decals"></a>
<a name="decals"></a>
## 贴花（Decals）

`URP Package Samples/Decals` 文件夹包含[贴花](renderer-feature-decal.md)的示例。下表描述了该文件夹中的每个贴花示例。

| **示例**        | **描述**                                                     |
| -------------- | ------------------------------------------------------------ |
| **斑点阴影（Blob shadows）** | `Decals/BlobShadow` 示例使用 [Decal Projector 组件](renderer-feature-decal.md#decal-projector-component) 在角色下方投射阴影。这种阴影渲染方式比阴影贴图消耗的资源更少，适用于低端设备。 |
| **油漆飞溅（Paint splat）** | `Decals/PaintSplat` 示例使用 **WorldSpaceUV 子图（Sub Graph）** 和 [Simple Noise](https://docs.unity.cn/cn/Packages-cn/com.unity.shadergraph@latest/index.html?subfolder=/manual/Simple-Noise-Node.html) Shader Graph 节点来创建程序化贴花。每个油漆飞溅的噪声基于 Decal Projector 组件的世界坐标。 |
| **代理光照（Proxy lighting）** | `Decals/ProxyLighting` 示例基于 **斑点阴影** 示例，并使用 Decal Projector 添加代理聚光灯（Proxy Spotlights）。这些贴花会修改投影器体积内表面的自发光（Emission）。<br/>**注意**：为演示光照模拟效果，此示例禁用了常规实时光照。 |

<a name="lens-flares"></a>
## 镜头光晕（Lens Flares）

`URP Package Samples/LensFlares` 文件夹包含镜头光晕的示例。下表描述了该文件夹中的每个镜头光晕示例。

| **示例**               | **描述**                                                     |
| --------------------- | ------------------------------------------------------------ |
| **太阳光晕（Sun flare）** | `LensFlares/SunFlare` 示例演示如何使用 [Lens Flare 组件](shared/lens-flare/lens-flare-component.md) 为场景中的主定向光添加镜头光晕效果。 |
| **镜头光晕展示间（Lens flare showroom）** | `LensFlares/LensFlareShowroom` 示例帮助用户创建和调整镜头光晕。使用方法如下：</br>1. 在 **Hierarchy** 窗口中，选择 **Lens Flare** GameObject。</br>2. 在 Lens Flare 组件中，将一个 [LensFlareDataSRP](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.core@latest/api/UnityEngine.Rendering.LensFlareDataSRP.html) 资源分配给 **Lens Flare Data** 属性。</br>3. 调整 Lens Flare 组件和数据属性，并在 **Game View** 中查看光晕效果。<br/>**注意**：如果文本框遮挡视图，可禁用场景中的 Canvas。 |

<a name="lighting"></a>
## 光照

`URP Package Samples/Lighting` 文件夹包含[光照](lighting.md)的示例。下表描述了该文件夹中的每个光照示例。

| **示例**                 | **描述**                                                     |
| ----------------------- | ------------------------------------------------------------ |
| **反射探针（Reflection probes）** | `Lighting/Reflection Probes` 示例使用[反射探针](lighting/reflection-probes.md) 为一个反射球体 GameObject 创建反射贴图。此示例展示了 **Probe Blending** 和 **Box Projection** 设置如何改变场景中反射探针的效果。 |

## <a name="renderer-features"></a>渲染器功能

`URP Package Samples/RendererFeatures` 文件夹包含[渲染器功能](urp-renderer-feature.md)的示例。下表描述了该文件夹中的每个渲染器功能示例。

| **示例**                | **描述**                                                     |
| ---------------------- | ------------------------------------------------------------ |
| **环境光遮蔽（Ambient occlusion）** | `RendererFeatures/AmbientOcclusion` 示例使用渲染器功能为 URP 添加[屏幕空间环境光遮蔽（SSAO）](post-processing-ssao.md)。查看 `SSAO_Renderer` 资源，了解如何设置此效果。 |
| **Blit 到 RTHandle** | 此示例展示了如何将相机颜色纹理进行 Blit 传输到输出纹理，并将其设置为全局属性，使 Scene 中的 Shader 能够使用该全局纹理。<br/>参阅[Blit 相机颜色纹理到 RTHandle](customize/blit-to-rthandle.md)以了解详情。 |
| **深度 Blit（Depth Blit）** | 该示例使用自定义渲染器功能将深度纹理复制或渲染到 RTHandle，并执行全屏 Blit 到屏幕，实现渐隐效果。 |
| **扭曲隧道（Distort Tunnel）** | 此示例展示了涉及多个 `RTHandle` 纹理和自定义 Shader 效果的 Blit 操作。<br/>参阅[Blit 多个 RTHandle 纹理](customize/blit-multiple-rthandles.md)以了解详情。 |
| **故障效果（Glitch effect）** | `RendererFeatures/GlitchEffect` 示例使用 [Render Objects](renderer-features/renderer-feature-render-objects.md) 渲染器功能和 [Scene Color](https://docs.unity.cn/cn/Packages-cn/com.unity.shadergraph@latest/index.html?subfolder=/manual/Scene-Color-Node.html) Shader Graph 节点，为部分 GameObjects 添加故障（Glitch）效果。<br/>查看 `Glitch_Renderer` 资源，了解如何设置此效果。 |
| **保持帧（Keep frame）** | `RendererFeatures/KeepFrame` 示例使用自定义渲染器功能，在帧之间保持颜色数据。此示例通过简单的粒子系统创建漩涡（Swirl）效果。<br/>**注意**：该效果仅在播放模式（Play Mode）下可见。 |
| **遮挡效果（Occlusion effect）** | `RendererFeatures/OcclusionEffect` 示例使用 Render Objects 渲染器功能来绘制被遮挡的几何体。<br/>示例完全在 `OcclusionEffect_Renderer` 资源中进行配置，无需额外代码。 |
| **拖尾效果（Trail effect）** | `RendererFeatures/TrailEffect` 示例在额外的相机上使用 **Keep frame** 示例的渲染器功能，以创建拖尾贴图。<br/>额外相机会将深度渲染到 RenderTexture，而 `Sand_Graph` Shader 采样该贴图并偏移地面的顶点。 |

<a name="shaders"></a>
## Shader 示例

`URP Package Samples/Shaders` 文件夹包含 Shader 示例。下表描述了该文件夹中的每个 Shader 示例。

| **示例**  | **描述**                                                     |
| -------- | ------------------------------------------------------------ |
| **Lit**  | `Shaders/Lit` 示例展示了[Lit Shader](lit-shader.md) 的不同属性如何影响几何体表面。你可以使用示例中的材质和纹理作为 URP 材质设置的参考。 |

