# 通用渲染管线 (URP) 中的阴影

通用渲染管线 (URP) 的 [光源 (Lights)](light-component.md) 可以使一个 GameObject 投射阴影到另一个 GameObject 上。这可以突出 GameObject 的位置和尺寸，为场景增加一定的深度和真实感，否则场景可能会显得过于平面化。

URP 使用 [阴影贴图 (Shadow Maps)](https://docs.unity.cn/cn/tuanjiemanual/Manual/shadow-mapping.html) 和 [阴影级联 (Shadow Cascades)](https://docs.unity.cn/cn/tuanjiemanual/Manual/shadow-cascades.html)。

你可以添加 [屏幕空间阴影渲染器功能 (Screen Space Shadows Renderer Feature)](renderer-feature-screen-space-shadows.md)，使 URP 通过单一渲染纹理计算并绘制主方向光的阴影，而不是使用多个阴影级联贴图。



## 阴影贴图分辨率

光源的阴影贴图分辨率决定了阴影贴图的大小。分辨率越高，阴影的精度越高，URP 能够捕捉到更多投影几何体的细节，使阴影更清晰。但更高的分辨率也会增加计算成本。

URP 为每种类型的光源生成的阴影贴图数量如下：

- **聚光灯 (Spot Light)** 生成 1 张阴影贴图。
- **点光源 (Point Light)** 生成 6 张阴影贴图（即立方体贴图的 6 个面）。
- **方向光 (Directional Light)** 每个级联生成 1 张阴影贴图。可在 **URP 资源 (Universal Render Pipeline Asset)** 中设置方向光的级联数量。

URP 会根据场景中所需的阴影贴图数量以及阴影图集的大小，自动选择最佳的阴影分辨率。



## 阴影图集 (Shadow Atlases)

URP 使用统一的阴影贴图图集 (Shadow Map Atlas) 来渲染实时阴影：
- **点光源 (Point Lights)** 和 **聚光灯 (Spot Lights)** 共享同一个阴影贴图图集。
- **方向光 (Directional Light)** 另有单独的阴影贴图图集。

你可以在 **URP 资源 (Universal Render Pipeline Asset)** 中设置这些图集的大小。图集大小决定了场景中阴影的最大分辨率。例如：

- **1024 × 1024** 的图集可容纳：
  - 4 张 **512 × 512** 的阴影贴图。
  - 16 张 **256 × 256** 的阴影贴图。

### 使阴影图集分辨率匹配 Built-In RP 设置

在 **内置渲染管线 (Built-In Render Pipeline)** 中，你可以在 **质量设置 (Quality Settings)** 里选择阴影分辨率等级（“低 (Low)”、“中 (Medium)”、“高 (High)”、“超高 (Very High)”），Unity 会根据具体情况自动调整阴影贴图的实际分辨率。

在 **URP** 中，你需要直接指定 **阴影图集 (Shadow Atlas)** 的分辨率，这样可以精确控制应用程序分配给阴影的显存占用。

如果你想确保 URP 在项目中不会使用低于某个特定值的阴影分辨率，应考虑：
- 场景中所需的阴影贴图数量。
- 选择足够大的阴影图集分辨率。

例如：如果场景中有 4 个聚光灯 (Spot Lights) 和 1 个点光源 (Point Light)，并希望每张阴影贴图的分辨率至少为 **256 × 256**：
- 由于 1 个点光源需要 **6** 张阴影贴图，而 4 个聚光灯需要 **4** 张阴影贴图，总计 **10** 张阴影贴图。
- **512 × 512** 大小的图集最多只能容纳 **4** 张 **256 × 256** 贴图，因此不够用。
- **1024 × 1024** 大小的图集可容纳 **16** 张 **256 × 256** 贴图，因此应选择此大小。



## 阴影偏移 (Shadow Bias)

阴影贴图本质上是从光源视角投影的纹理。URP 使用 **偏移 (Bias)** 来防止投射阴影的几何体自我阴影 (Self-Shadowing)。

每个光源组件的阴影偏移参数如下：

- **深度偏移 (Depth Bias)**
- **法线偏移 (Normal Bias)**
- **近裁剪面 (Near Plane)**

这些设置位于光源的 **阴影 (Shadows)** 部分。如果它们不可见，请将 **偏移 (Bias)** 选项从 “使用管线设置 (Use Pipeline Settings)” 改为 “自定义 (Custom)”。

如果阴影偏移值过高，可能会导致光线“泄漏” (Light Leaking)，即阴影与投射物之间存在可见的间隙，导致阴影形状无法准确匹配其投射物。



## 调整阴影以优化性能

请参考 [优化性能的配置 (Configure for better performance)](configure-for-better-performance.md) 了解如何调整阴影设置以提高性能。
