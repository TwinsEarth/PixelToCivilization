# SpeedTree Shaders

Universal Render Pipeline 使用 SpeedTree 系统来处理树木的着色器。要了解更多信息，[请阅读 Unity 主手册中的 SpeedTree 文档](https://docs.unity.cn/cn/tuanjiemanual/Manual/SpeedTree.html)。

在 URP 中使用 SpeedTree 着色器时，请注意以下几点：

* URP 中的树木没有全局光照 (Global Illumination)。

* 树木无法接收阴影。

* 在 URP 中，你可以在 [URP Asset](universalrp-asset.md) 中配置光源是否按顶点或按像素计算。
