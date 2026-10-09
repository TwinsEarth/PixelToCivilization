
Graph Target
============


Target 决定了使用 Shader Graph 生成的着色器的端点兼容性。您可以为每个 Shader Graph 资源选择 Target，并使用 [Graph Settings 选项卡](Graph-Settings-Tab.md)更改 Target。


![image](images/GraphSettings_Menu.png)


Target 包含所需的生成格式等信息以及变量，使得与不同渲染管线或集成功能（如 [Visual Effect Graph](https://docs.unity.cn/cn/Packages-cn/com.unity.visualeffectgraph@latest) 兼容。您可以为每个 Shader Graph 资源选择任意数量的 Target。如果您选择的 Target 与已选择的其他目标不兼容，则会出现一条解释问题的错误消息。


Target Settings 特定于每个目标，并且可能因资源而异，具体取决于您选择的 Target。请注意，通用渲染管线 (URP) 的 Target Settings 和高清渲染管线 (HDRP) 的 Target Settings 可能会在未来版本中更改。


通常，您选择的每个 Target 都会从图形生成一个有效的子着色器。例如，同时具有 URP 和 HDRP Target 的 Shader Graph 资源将生成两个子着色器。当您使用面向多个渲染管线的图形时，如果更改活动渲染管线，则必须重新导入 Shader Graph 资源。这将更新使用该图形的任何材质的材质检视面板。


Shader Graph 支持三个目标：[通用渲染管线 (URP)](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest) 、[高清渲染管线 (HDRP)](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest) 和[内置渲染管线](https://docs.unity.cn/cn/tuanjiemanual/Manual/built-in-render-pipeline.html)。

并非所有块都与所有目标兼容。如果在选择目标时图表中的某个块变为非活动状态，则该块与该目标不兼容。

由于 URP、Built-in 和 HDRP 之间的技术差异，图表在不同渲染管线中的视觉效果会有所不同。

面向内置渲染管线的 Shader Graph 会模拟手写 ShaderLab 着色器的效果，但法线贴图除外。为了数学上的准确性，即使构建目标是内置渲染管线，通过 Shader Graph 创建的法线贴图也会与 URP 中的行为一致。

