# Shader Stripping

通用渲染管线（URP）中的着色器使用了许多[着色器关键字（Shader Keywords）](https://docs.unity.cn/cn/tuanjiemanual/Manual/shader-keywords)来支持多种不同功能，这通常会导致 Unity 编译大量的[着色器变体](https://docs.unity.cn/cn/tuanjiemanual/Manual/shader-variants)。  

如果你在 [URP 资源（URP Asset）](universalrp-asset.md) 中禁用了某些功能，URP 会自动排除（“剥离”）相关的着色器变体，从而加速构建并减少内存使用和文件体积。

例如，如果你的项目不使用方向光（Directional Light）的阴影，但默认情况下 Unity 仍会将支持方向光阴影的变体包含在构建中。若你在 URP Asset 中禁用 **Cast Shadows**，URP 将剥离这些变体。

若你想查看 URP 中负责剥离着色器的代码，可以参考 `Editor/ShaderPreprocessor.cs` 文件。该文件使用了 [IPreprocessShaders](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/Build.IPreprocessShaders.html) API。

想了解更多关于着色器变体剥离的信息，可参阅以下内容：
- [检查项目中的着色器变体数量](https://docs.unity.cn/cn/tuanjiemanual/Manual/shader-how-many-variants.html)
- [标准的着色器变体剥离指导](https://docs.unity.cn/cn/tuanjiemanual/Manual/shader-variant-stripping.html)，适用于所有渲染管线


## 剥离功能相关的着色器变体

默认情况下，URP 会同时编译功能开启和功能关闭这两类变体。

为了减少变体数量，你可以在 [URP Global Settings](urp-global-settings.md) 中启用 **Strip Unused Variants**，并根据以下方法进行操作：

- 在构建中所有 URP 资产中均禁用某功能，这样 URP 仅保留禁用该功能时的变体。
- 在构建中所有 URP 资产中均启用某功能，这样 URP 仅保留启用该功能时的变体。

如果你禁用 **Strip Unused Variants** 设置，URP 将无法剥离功能关闭时的变体，这可能会导致变体数量增加。

### 禁用某项功能

若要使 Unity 剥离（Strip）与某功能相关的变体，必须在构建中使用的所有 URP 资源（URP Assets）中都禁用该功能。

Unity 会将以下 URP 资源包含到构建中：
- 在[图形设置](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-GraphicsSettings.html)中作为默认渲染管线资源（Render Pipeline Asset）指定的 URP 资源。
- 在[质量设置](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-QualitySettings.html)中启用、并为当前构建目标指定了“渲染管线资源（Render Pipeline Asset）”的 URP 资源。

避免在构建中包含使用不同[渲染路径](urp-universal-renderer.html#rendering-path-comparison)的 URP 资源，因为这样会使 Unity 针对每个关键字创建两个变体集合。

| **功能**                   | **如何禁用**                                                  | **剥离的着色器关键字**                                            | **渲染路径**      |
|----------------------------|------------------------------------------------------------|--------------------------------------------------------------------|-------------------|
| Accurate G-buffer normals  | 在 URP 资源中禁用 **Accurate G-buffer normals**。对使用 Vulkan 图形 API 的平台无效。 | `_GBUFFER_NORMALS_OCT`                                            | Deferred          |
| Additional lights          | 在 URP 资源的 **Lighting** 部分，禁用 **Additional Lights**。 | `_ADDITIONAL_LIGHTS`, `_ADDITIONAL_LIGHTS_VERTEX`                  | Forward           |
| Ambient occlusion          | 在所有 URP 资源所使用的 Renderer 中移除 [Ambient Occlusion](post-processing-ssao.html) Renderer Feature。 | `_SCREEN_SPACE_OCCLUSION`                                          | Forward & Deferred|
| Decals                     | 在所有 URP 资源所使用的 Renderer 中移除 [Decals](renderer-feature-decal.html) Renderer Feature。 | `_DBUFFER_MRT1`, `_DBUFFER_MRT2`, `_DBUFFER_MRT3`, `_DECAL_NORMAL_BLEND_LOW`, `_DECAL_NORMAL_BLEND_MEDIUM`, `_DECAL_NORMAL_BLEND_HIGH`, `_DECAL_LAYERS` | Forward & Deferred |
| Fast sRGB to linear conversion | 在 URP 资源的 **Post-processing** 部分，禁用 **Fast sRGB/Linear conversions**。 | `_USE_FAST_SRGB_LINEAR_CONVERSION`                                  | Forward & Deferred |
| Holes in terrain           | 在 URP 资源的 **Rendering** 部分，禁用 **Terrain Holes**。   | `_ALPHATEST_ON`                                                    | Forward           |
| Light cookies              | 在项目中所有光源（Light）上移除[Cookie 纹理](https://docs.unity.cn/cn/tuanjiemanual/Manual/Cookies.html)。 | `_LIGHT_COOKIES`                                                   | Forward & Deferred |
| Rendering Layers for lights| 禁用[光源的渲染层（Rendering Layers）](features/rendering-layers.html)。 | `_LIGHT_LAYERS`                                                    | Forward & Deferred |
| Reflection Probe blending  | 禁用[反射探针混合（Probe Blending）](lighting/reflection-probes.html#configuring-reflection-probe-settings)。 | `_REFLECTION_PROBE_BLENDING`                                       | Forward & Deferred |
| Reflection Probe box projection | 禁用[Box Projection](lighting/reflection-probes.html#configuring-reflection-probe-settings)。 | `_REFLECTION_PROBE_BOX_PROJECTION`                                 | Forward & Deferred |
| Render Pass                | 在所有 URP 资源使用的 Renderer 中禁用 **Native Render**。  | `_RENDER_PASS_ENABLED`                                             | Forward & Deferred |
| Shadows from additional lights | 在 URP 资源的 **Additional Lights** 部分，禁用 **Cast Shadows**。 | `_ADDITIONAL_LIGHT_SHADOWS`                                        | Forward & Deferred |
| Shadows from the main light | 在 URP 资源的 **Main Light** 部分，禁用 **Cast Shadows**。Unity 所剥离的关键字取决于你的具体设置。 | `_MAIN_LIGHT_SHADOWS`, `_MAIN_LIGHT_SHADOWS_CASCADE`, `_MAIN_LIGHT_SHADOWS_SCREEN` | Forward & Deferred |
| Soft shadows               | 在 URP 资源的 **Shadows** 部分，禁用 **Soft shadows**。     | `_SHADOWS_SOFT`                                                    | Forward & Deferred |

---

## 剥离后处理（Post-processing）着色器变体

在 [URP Global Settings](urp-global-settings.md) 中启用 **Strip Unused Post Processing Variants**，可剥离项目中未使用的[后处理](VolumeOverrides.md)着色器变体。

例如，如果你的项目仅使用 Bloom 效果，那么 URP 会保留 Bloom 变体，但剥离所有其他后处理变体。

Unity 会在项目中所有场景（Scenes）中查找后处理组件，因此，单纯地将一个场景从构建中移除而仍保留在项目中，并不能达到剥离该场景所用变体的目的。

| **移除的后处理**            | **剥离的着色器关键字**                                       |
|----------------------------|--------------------------------------------------------------|
| Bloom                      | `_BLOOM_HQ`, `BLOOM_HQ_DIRT`, `_BLOOM_LQ`, `BLOOM_LQ_DIRT`  |
| Chromatic Aberration       | `_CHROMATIC_ABERRATION`                                     |
| Film Grain                 | `_FILM_GRAIN`                                               |
| HDR Grading                | `_HDR_GRADING`                                              |
| Lens Distortion            | `_DISTORTION`                                               |
| Tonemapping                | `_TONEMAP_ACES`, `_TONEMAP_NEUTRAL`, `_TONEMAP_GRADING`     |

若你并未针对“大量多显示屏（‘cluster’ displays）”进行后处理屏幕坐标覆盖，则建议在 URP Global Settings 中启用 **Strip Screen Coord Override Variants**。


## 剥离 XR 和 VR 着色器变体

如果你未使用 [XR](https://docs.unity.cn/cn/tuanjiemanual/Manual/XR.html) 或 [VR](https://docs.unity.cn/cn/tuanjiemanual/Manual/VROverview.html)，可[禁用 XR 与 VR 模块](https://docs.unity.cn/cn/tuanjiemanual/Manual/upm-ui.html)，从而使 URP 剥离其标准着色器中与 XR/VR 相关的变体。


## 使用自定义 Renderer Feature 时移除变体

若你创建了[自定义 Renderer Feature](xref:UnityEngine.Rendering.Universal.ScriptableRendererFeature)，可使用 [FilterAttribute](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/ShaderKeywordFilter.FilterAttribute.html) API，在[URP 资源（URP Asset）](universalrp-asset.md)中启用或禁用设置时移除相关着色器变体。

例如，你可以进行以下操作：
1. 使用 [[SerializeField]](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/SerializeField.html) 为自定义 Renderer Feature 添加一个布尔变量（Boolean），并在 URP Asset Inspector 中以复选框形式显示。
2. 使用 [ShaderKeywordFilter.RemoveIf](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/ShaderKeywordFilter.RemoveIfAttribute.html)，当启用该复选框时，移除相关着色器变体。
