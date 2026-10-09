
Diffusion Profile 节点
========


扩散配置文件节点（Diffusion Profile Node）允许在 Shader Graph 中采样[扩散配置文件](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Diffusion-Profile)资源。如需了解什么是扩散配置文件以及扩散配置文件包含哪些属性，请参阅[扩散配置文件文档](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Diffusion-Profile)。


渲染管线兼容性
-------

| **节点** | **通用渲染管线 (URP)** | **高清渲染管线 (HDRP)** |
| --- | --- | --- |
| Diffusion Profile Node | 否 | 是 |


端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| **Out** | 输出 | Float | 输出一个唯一浮点数，供着色器用于标识扩散配置文件。 |


注意
--

该节点的输出是一个表示扩散配置文件的浮点值。着色器可以使用该值查找该值表示的扩散配置文件资源的设置。

如果修改输出值，则着色器将无法再使用该值来查找扩散配置文件资源的设置。您可以使用此行为在 Shader Graph 中启用和禁用扩散配置文件。要禁用某个扩散配置文件，请将输出乘以 **0**。要启用某个扩散配置文件，请将输出乘以 **1**。这使您可以在 Shader Graph 的不同部分中使用多个扩散配置文件。请注意，高清渲染管线 (HDRP) 不支持扩散配置文件之间的混合。这是因为 HDRP 只能评估每个像素的一个扩散配置文件。



