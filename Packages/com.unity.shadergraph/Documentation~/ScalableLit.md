# Scalable Lit

## 描述

您可以根据自己的需求，通过 **Scalable Lit** 调整不同消耗下的渲染质量和渲染特性，达到 Performance 和 Quality 的平衡。

#### 支持的渲染管线

* [通用渲染管线（URP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest) 

[高清渲染管线（HDRP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest) 暂**不**支持此着色器。

![](./Images/ScalableLit.PNG)

## 属性

| 属性 | 描述 |
| -- | -- |
| **Receive Global Illumination** |是否接受全局光照，左：On，右：Off。<br> ![GlobalIlluminationSample](./Images/GlobalIlluminationSample.jpg) |
| **Diffuse Quality** | 支持三种模式：None，Low，High。<br><ul><li>None 不接受直接漫反射。</li> <li>Low 和 High 之间会有细微差别，主要表现在光滑度较高时，High 的明暗交接处会更柔和。同样的，High的消耗也会更大</li></ul>![](./Images/DiffuseQuality.PNG)<br> 下图左侧为 Low，右侧为 High 模式下的效果。![](./Images/DiffuseQualitySample.jpg)|
| **Specular Quality** |支持四种模式：None，Low，Medium，High。<br><ul><li>None 不接受直接高光。</li><li>Low，Medium，High 质量依次递增，同样的，消耗也依次递增，其中 Medium 模式为 Lit 使用的标准模式，High 模式支持各向异性高光。</li></ul>![](./Images/SpecularQuality.PNG)![](./Images/SpecularQualitySample.JPEG) |
| **Shading Feature** | 您可以自行选择需要用到的特性。<br>**Clear Coat** 常用于带有清漆涂层的材质，比如车漆等。![](./Images/ShadingFeature.png)![](./Images/ShadingFeatureSample1.jpg)<br>**Thin Film** 常用于各种带有薄膜、油膜的材质。![](./Images/ShadingFeatureSample2.jpg)<br>**Diffraction Gratings** 多用于模拟类似于光盘的光栅衍射效果。![](./Images/ShadingFeatureSample3.jpg)<br>**Anisotropy** 多用于模拟各向异性高光，在使用 Anisotropy 时，高光质量会强制变成 **High**。![](./Images/ShadingFeatureSample4.jpg)<br>**Custom Indirect Diffuse** 和 **Custom Indirect Specular** 可以支持更方便地融合您的间接光照方案，如图，当用户输入自己的间接光以及间接光遮罩后，会将输入的间接光照通过遮罩进行混合。![](./Images/ShadingFeatureSample5.jpg)|
| [**Removed Pass**](./RenderingOptimization.md) | 用于选择性剔除一些不需要的 Pass。![](./Images/RemovedPass1.png) |
| [**Removed Keywords**](./RenderingOptimization.md)  | 用于选择性剔除一些不需要的 Keywords。![](./Images/RemovedKeywords1.png) |

