
生产级着色器
========================

Shader Graph 生产级着色器示例是一个 Shader Graph 着色器资产集合，您可以直接使用或根据需要进行修改。您可以分解它们来学习，也可以直接将它们放入项目中使用。该示例还包括逐步教程，介绍如何结合多个着色器来创建一个森林溪流环境。

在使用 URP 时，为确保正确显示内容，请对项目设置进行以下更改：

*   打开项目设置（**Edit -> Project Settings**）并选择 Shader Graph 选项卡。将着色器变体限制设置为513（这将确保 URP Lit 着色器正常工作）。
*   选择项目的 SRP 设置资源，并启用深度纹理（Depth Texture）和不透明纹理（Opaque Texture）（这将确保水面着色器渲染正确）。
*   选择项目的渲染器数据资源。在底部点击 Add Renderer Feature 按钮，添加贴花功能（这将确保贴花着色器正常渲染）。

示例内容分为以下类别：

| 主题 | 描述 |
| --- | --- |
| **[光照着色器](Shader-Graph-Sample-Production-Ready-Lit.md)** | 介绍HDRP和URP光照着色器的Shader Graph版本。用户通常希望修改光照着色器但因为它们是代码编写的而难以进行。现在您可以使用这些而无需从头开始。 |
| **[贴花着色器](Shader-Graph-Sample-Production-Ready-Decal.md)** | 介绍可以增强和丰富环境的着色器，例如流动的水、湿度、水的焦散效果和材质投影。 |
| **[细节着色器](Shader-Graph-Sample-Production-Ready-Detail.md)** | 介绍如何创建高效渲染的[地形细节](https://docs.unity.cn/cn/tuanjiemanual/Manual/terrain-Grass.html)的着色器，这些着色器渲染速度快且使用较少的纹理内存，包括三叶草、蕨类、草、荨麻和鹅卵石。 |
| **[岩石](Shader-Graph-Sample-Production-Ready-Rock.md)** | 一个强大且模块化的岩石着色器，包含基础纹理、大范围和微小细节、苔藓投影和天气效果。 |
| **[水](Shader-Graph-Sample-Production-Ready-Water.md)** | 用于池塘、溪流、湖泊和瀑布的水体着色器，包括深度雾、表面波纹、流动映射、折射和表面泡沫。 |
| **[后处理](Shader-Graph-Sample-Production-Ready-Post.md)** | 为场景添加后处理效果的着色器，包括边缘检测、半色调、镜头上的雨滴、水下效果和VHS录像带图像降解效果。 |
| **[天气](Shader-Graph-Sample-Production-Ready-Weather.md)** | 天气效果，包括雨滴、雨水滴落、程序生成的水坑、水坑波纹和雪。 |
| **[其他](Shader-Graph-Sample-Production-Ready-Misc.md)** | 一些额外的着色器，包括体积冰和关卡分块着色器。 |
| **[森林溪流构建教程](Shader-Graph-Sample-Production-Ready-Tutorial.md)** | 介绍如何使用该示例中的多个资源来创建一个森林溪流的教程。 |