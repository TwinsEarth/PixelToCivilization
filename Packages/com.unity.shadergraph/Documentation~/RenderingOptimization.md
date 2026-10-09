# 渲染优化工具包

本工具包旨在提供精细化的渲染优化选项，帮助开发者减少不必要的性能开销，提高渲染效率。

#### 支持的渲染管线

* 通用渲染管线 URP

高清渲染管线 HDRP 暂**不**支持此功能。

## Shader Keywords剔除

在 [Graph Settings 选项卡](./Graph-Settings-Tab.md) 中，选择性地剔除当前 Shader 未使用的 Keywords，可有效减少无用变体的生成，节省性能开销。

![](./Images/RemovedKeywords.gif)

![](./Images/Remove.PNG) 

以下展示了当剔除了 Additional Lights 和 Additional Light Shadows 相关 Keywords 的前后效果：

* 上图：未剔除相关 Keywords 的 Shader 会生成变体，并且该 Shader 的物体仍会参与额外的光源与阴影计算。
* 下图：剔除相关 Keywords 后，Shader 不会生成对应的变体，使用此 Shader 的物体也不会参与相关的计算，从而提升性能。

![](./Images/WithKeywords.JPEG) 

![](./Images/WithoutKeywords.JPEG) 

## Pass 剔除

您也可以根据需求，选择性剔除一些 Pass 的生成，从而减少渲染开销和冗余操作。

![](./Images/RemovedPasses.gif)

![](./Images/RemovedP.PNG) 


以下展示了左侧物体的 Shader 在移除 Depth Normals Pass 和 Shadow Caster Pass 后的效果：

* 移除后效果：该物体不再生成这两个 Pass，因此：
* 阴影绘制：物体不参与阴影的生成和绘制，场景中不会显示其阴影效果。
* 深度法线图：物体不会出现在深度法线图中，因而对依赖此图的屏幕空间遮蔽效果不再产生影响。

![](./Images/RemovedPassesSample.JPEG) 