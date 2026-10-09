
Sclera Iris Blend 节点
====================

描述
----

该节点融合了虹膜（Iris）和巩膜（Sclera）的所有属性，从而将它们提供给主节点。


渲染管线兼容性
-------

| **节点** | **通用渲染管线 (URP)** | **高清渲染管线 (HDRP)** |
| --- | --- | --- |
| **Sclera Iris Blend 节点** | 否 | 是 |


端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| **Sclera Color** | 输入 | Color | 巩膜在目标片元的颜色。 |
| **Sclera Normal** | 输入 | Vector3 | 巩膜在目标片元的法线。 |
| **Sclera Smoothness** | 输入 | Float | 巩膜在目标片元的平滑度。 |
| **Iris Color** | 输入 | Color | 虹膜在目标片元的颜色。 |
| **Iris Normal** | 输入 | Vector3 | 虹膜在目标片元的法线。 |
| **Cornea Smoothness** | 输入 | Float | 角膜（cornea）在目标片元的平滑度。 |
| **IrisRadius** | 输入 | Float | 虹膜在模型中的半径。对于默认模型，此值应为 **0\.225**。 |
| **PositionOS** | 输入 | Vector3 | 要着色的片元在对象空间中的位置。 |
| **Diffusion Profile Sclera** | 输入 | 扩散配置文件（Diffusion Profile） | 用于计算巩膜的子面散射的扩散配置文件。 |
| **Diffusion Profile Iris** | 输入 | 扩散配置文件（Diffusion Profile） | 用于计算虹膜的子面散射的扩散配置文件。 |
| **EyeColor** | 输出 | Color | 眼睛的最终漫射颜色。 |
| **Surface Mask** | 输出 | Float | 定义片元所在位置的线性标准化值。位于角膜则为 1，位于巩膜则为 0。 |
| **Diffuse Normal** | 输出 | Vector3 | 漫射波瓣的法线。 |
| **Specular Normal** | 输出 | Vector3 | 镜面波瓣的法线。 |
| **EyeSmoothness** | 输出 | Float | 眼睛的最终平滑度。 |
| **SurfaceDiffusionProfile** | 输出 | 扩散配置文件（Diffusion Profile） | 目标片元的扩散配置文件。 |

