开始使用 Shader Graph
=================================

将 Shader Graph 与团结引擎中可用的可编程渲染管线 (SRPs) 配合使用：
* [通用渲染管线（URP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest) 
* [高清渲染管线（HDRP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest)
* [内置渲染管线（Built-In Render Pipeline）](https://docs.unity.cn/cn/tuanjiemanual/Manual/built-in-render-pipeline.html)

> [!NOTE]
> Shader Graph 对内置渲染管线的支持仅出于兼容性目的。除了现有功能的错误修复外，Shader Graph 不会收到内置渲染管道支持的更新。建议将 Shader Graph 与 SRP 一起使用。

在项目中安装 HDRP 或 URP 时，团结引擎也会自动安装 Shader Graph 软件包。您也可以使用 **Window -> Package Manager** 手动安装 Shader Graph，以便与内置渲染管线配合使用。有关如何安装软件包的更多信息，请参阅[添加和删除软件包](https://docs.unity.cn/cn/tuanjiemanual/Manual/upm-ui-actions.html)。

有关如何设置 SRP 的更多信息，请参阅 [HDRP 入门](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/Getting-started-with-HDRP.html) 或 [URP 入门](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest/manual/creating-a-new-project-with-urp.html)。