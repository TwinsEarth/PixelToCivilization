# SpeedTree 8 子图资源

## 前置信息
本文档假定您已熟悉以下页面中描述的概念：

* [SpeedTree](https://docs.unity.cn/cn/tuanjiemanual/Manual/SpeedTree.html)
* [子图节点](Sub-graph-Node.md)
* [子图资源](Sub-graph-Asset.md)
* [关键字](Keywords.md)

有关 [ShaderLab 材质属性](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-Properties.html) 的文档也可能对理解上下文有所帮助。

## 描述
[SpeedTree](https://docs.unity.cn/cn/tuanjiemanual/Manual/SpeedTree.html) 是一个第三方解决方案，提供现成的树木资源和用于创建自定义树木资源的建模软件。

Shader Graph 提供了三个内置的 SpeedTree 子图资源：

* [SpeedTree8ColorAlpha](#SpeedTree8ColorAlpha)
* [SpeedTree8Wind](#SpeedTree8Wind)
* [SpeedTree8Billboard](#SpeedTree8Billboard)

这些子图资源在[通用渲染管线 (URP)](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest) 和[高清渲染管线 (HDRP)](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest)中均可使用，使您能够使用 SpeedTree 8 资源并创建自定义的 SpeedTree 8 Shader Graph。

> [!NOTE]
> URP 特定版本的这些 SpeedTree 8 子图资源使用透明的 Billboard 背面，而不是剔除 Billboard 背面。仅在 URP 支持 Shader Graph 的材质级别剔除覆盖后，才能将这些子图资源替换为默认的 URP 等效资源。

## SpeedTree8ColorAlpha <a name="SpeedTree8ColorAlpha"></a>
每个 SpeedTree 资源有四种贴图：基础贴图（颜色/反照率）、凹凸贴图（提供表面法线）、额外贴图（提供金属度和环境光遮蔽数据）和次表面贴图（提供次表面散射颜色）。基础贴图提供输入的颜色和透明度数据。

该子图资源应用所有能修改基础贴图颜色和透明度数据的 SpeedTree 8 功能，适用于以下操作：

* 对基础贴图颜色进行染色
* 变化树木的色调
* 在不同细节层次 (LOD) 间平滑切换
* 隐藏几何接缝

### 对基础贴图颜色进行染色
您可以使用 SpeedTree8ColorAlpha 子图资源对基础贴图颜色进行染色。这在调整树木的季节性颜色时非常有用。

| 属性 | 支持 | 目的 | 行为 |
| --- | --- | --- | --- |
| `_ColorTint` | URP, HDRP | 给基础贴图染色。 | 将 `_ColorTint` 属性值与基础贴图颜色相乘。 |

### 变化树木的色调
要提高 SpeedTrees 的视觉多样性，您可以使用该子图资源来修改每棵树实例的颜色。`_OldHueVarBehavior` 和 `_HueVariationColor` 均使用树的绝对[世界空间位置](https://learnopengl.com/Getting-started/Coordinate-Systems)来确定[伪随机](https://en.wikipedia.org/wiki/Pseudorandomness)的色调变化强度值。

| 属性 | 支持 | 目的 | 行为 |
| --- | --- | --- | --- |
| `_OldHueVarBehavior` | URP | 匹配 URP 特定的和内置 SpeedTree 8 着色器的行为。 | 使用伪随机色调变化强度值对基础贴图颜色 (t=0) 和 HueVariation 颜色 (t=1) 之间的[线性插值](https://en.wikipedia.org/wiki/Linear_interpolation)进行[参数化](https://en.wikipedia.org/wiki/Parametrization_(geometry))。 |
| `_HueVariationColor` | URP, HDRP | 为 SpeedTree 提供更多的色调多样性。 | 使用伪随机色调变化强度值作为不透明度，并将其应用到基础贴图颜色上，作为[覆盖混合](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-BlendOp.html)。效果比 `_OldHueBehavior` 更为细腻。 |
| `EFFECT_HUE_VARIATION` | N/A | N/A | 此关键字用于手写的 SpeedTree 8 着色器，但在这些 SpeedTree 8 Shader Graph 中未使用，以确保符合默认着色器变体限制。 |
| `_HueVariationKwToggle` | URP，HDRP，Built-In | 仅用于支持升级功能。 | 参见 [SpeedTreeImporter.hueVariation](https://docs.unity3d.com/ScriptReference/SpeedTreeImporter-hueVariation.html)。 |

### 在不同细节层次 (LOD) 间平滑切换
交叉渐变在不同细节层次（[LODs](https://docs.unity.cn/cn/tuanjiemanual/Manual/LevelOfDetail.html)）之间进行抖动，以减少突然切换时的[画面跳动](https://docs.unity.cn/cn/tuanjiemanual/Manual/LevelOfDetail.html)。SpeedTree8ColorAlpha 子图资源使用了[自定义函数节点](Custom-Function-Node.md)来实现此目的。

该自定义函数节点并非特定于 SpeedTree。启用 **Animate Cross-fading** 并选择一个 **LOD Fade** 设置，即可将其应用于任何带有 LOD 组组件的资源。详情请参见[在 LOD 层级之间过渡](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-LODGroup.html#transitions)。


### 隐藏几何接缝
SpeedTree8ColorAlpha 子图资源应用渐变透明度，以柔化采样基础贴图不同部分的几何体段之间的过渡。

## SpeedTree8Wind <a name="SpeedTree8Wind"></a>
SpeedTree8Wind 子图资源使用 [自定义函数节点](Custom-Function-Node.md)根据应用程序的风数据变形 SpeedTree 8 模型的顶点，以使树木看起来随风摆动。

团结引擎将风数据应用于 SpeedTree8Wind 子图资源的过程如下：

* 当 [WindZone](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-WindZone.html) 影响具有 [Wind](https://docs.unity.cn/cn/tuanjiemanual/Manual/com.unity.modules.wind.html) 启用的 SpeedTree 8 游戏对象时，团结引擎会生成 SpeedTree 8 风模拟数据。
* 团结引擎将该风模拟数据填充到 SpeedTreeWind 缓存中。
* SpeedTree8Wind 子图资源的变形行为基于 SpeedTreeWind 缓存中的数据。

## SpeedTree8Billboard <a name="SpeedTree8Billboard"></a>
SpeedTree8Billboard 子图资源从 SpeedTree 8 模型的凹凸贴图、几何切线和副法线数据中计算出广告牌的法线。它包含抖动功能，以改善模型在对角视角下广告牌的显示效果。

与该功能关联的关键字开关名为 `EFFECT_BILLBOARD`。这支持与旧版本 ShaderGraph 的向后兼容性，旧版本要求关键字和其切换属性名称相同。

## SpeedTree 8 InterpolatedNormals
团结引擎提供的所有 SpeedTree 8 着色器在顶点阶段对几何法线、切线和副法线进行插值处理，因为这比 Shader Graph 节点提供的逐像素数据具有更好的视觉效果。如果您的 SpeedTree 8 Shader Graph 不包含自定义插值器，则无需使用此功能。

HDRP 和 URP 在背面法线变换行为上不完全一致。当您对几何法线、切线和副法线数据使用[自定义插值器](Custom-Interpolators.md)时，这可能会带来问题。SpeedTree 8 InterpolatedNormals 子图资源的目的是支持这种差异。它将几何法线数据与凹凸贴图结合，以与目标渲染管线的背面法线变换行为兼容。