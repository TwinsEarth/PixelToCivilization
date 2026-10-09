# Fabric

## 描述

**Fabric** 材质提供两种模式可供选择：CottonWool 和 Silk。两种模式均基于 Specular 工作流，适用于不同类型的织物效果。

#### 支持的渲染管线

* [通用渲染管线（URP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest) 

[高清渲染管线（HDRP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest) 暂**不**支持此着色器。

![](./Images/FabricShader.png)

## 属性

| 属性 | 描述 |
| -- | -- |
| **CottonWool** | 常用于模拟绵毛等织物。<br> ![](./Images/FabricCottonWoolSample.jpg) |
| **Silk** | 多用于模拟丝绸等具有各向异性的织物，在使用 Silk 模式时，高光质量会强制为 **High**。<br> ![](./Images/FabricSilkSample.jpg) 同时，用户可以选择漫反射质量（Diffuse Quality）为低还是高。![](./Images/FabricDiffuseQuality.png)|
| **Shading Feature** | 和 [Scalable Lit](./ScalableLit.md) 相同，使用者可以自行选择需要用到的特性。![](./Images/FabricShadingFeature.png) |
| [**Removed Pass**](./RenderingOptimization.md) | 用于选择性剔除一些不需要的 Pass。![](./Images/RemovedPass1.png) |
| [**Removed Keywords**](./RenderingOptimization.md)  | 用于选择性剔除一些不需要的 Keywords。![](./Images/RemovedKeywords1.png) |

