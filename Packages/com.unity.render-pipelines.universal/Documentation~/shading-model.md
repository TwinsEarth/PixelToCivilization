# 通用渲染管线 (URP) 中的着色模型

着色模型定义了材质的颜色如何根据表面朝向、观察方向和光照等因素而变化。你应根据应用的艺术需求和性能预算来选择合适的着色模型。URP 提供以下着色模型：

- [通用渲染管线 (URP) 中的着色模型](#通用渲染管线-urp-中的着色模型)
  - [物理基准着色](#物理基准着色)
  - [简单着色](#简单着色)
  - [烘焙着色](#烘焙着色)
  - [无光照](#无光照)


## 物理基准着色

物理基准着色 (Physically Based Shading, PBS) 基于物理原理计算表面反射光的数量，从而模拟出近似真实世界的外观。这种方式可创建照片级真实感的对象和表面。

该着色模型遵循以下原则：

1. **能量守恒（Energy conservation）**：表面反射光的量不会超过其接收到的全部光能，除非对象本身会发光（如霓虹灯）。
2. **微观几何（Microgeometry）**：物体表面在微观层面存在几何结构。一些物体拥有平滑的微观几何，呈现出类似镜面的反射；另一些物体较粗糙，看起来更暗淡。在 URP 中，你可以通过调节物体表面的平滑度来模拟这种效果。

光线照射到物体表面时，部分光会被反射，部分光会折射。反射光被称为**镜面反射（Specular Reflection）**，其强度取决于摄像机方向和光线入射角（[angle of incidence](<https://en.wikipedia.org/wiki/Angle_of_incidence_(optics)>)）。该模型中，镜面高光的形状用 [GGX 函数](https://blogs.unity3d.com/2016/01/25/ggx-in-unity-5-3/)进行近似。

对于金属表面，表面会吸收并改变光线；对于非金属（也称[介电质（dielectric）](<https://en.wikipedia.org/wiki/Dielectric>)）表面，光线被部分反射。

光衰减仅受光照强度的影响，这意味着不必增大光的范围来调整衰减。

下列 URP 着色器采用此物理基准着色模型：
- [Lit](lit-shader.md)
- [Particles Lit](particles-lit-shader.md)

> **注意：** 若你面向低端移动硬件，此着色模型可能并不合适。建议改用采用[简单着色](#simple-shading)模型的着色器。

如果想了解更多物理基于渲染 (PBR) 的原理，请参阅 [Joe Wilson 在 Marmoset 编写的详细介绍](https://marmoset.co/posts/physically-based-rendering-and-you-can-too/)。


## 简单着色

该着色模型适合风格化视觉效果或针对硬件性能较弱的平台。采用这种模型的材质无法真实地模拟物理外观，并且不会遵循能量守恒原则。本模型基于 [Blinn-Phong](https://en.wikipedia.org/wiki/Blinn%E2%80%93Phong_shading_model) 着色模型。

在简单着色模型中，材质分别反射漫反射和镜面光，但二者之间并无能量关联。由于材质属性的设置，物体反射的总光量可能会超出入射光量。此外镜面反射强度仅随摄像机方向变化。

光衰减仅受光照强度影响。

下列 URP 着色器采用简单着色模型：
- [Simple Lit](simple-lit-shader.md)
- [Particles Simple Lit](particles-simple-lit-shader.md)


## 烘焙着色

采用烘焙着色模型的材质不支持实时光照，只能通过[烘焙光照](https://docs.unity.cn/cn/tuanjiemanual/Manual/LightMode-Baked.html)（如 [lightmaps](https://docs.unity.cn/cn/tuanjiemanual/Manual/Lightmapping.html) 或 [Light Probes](https://docs.unity.cn/cn/tuanjiemanual/Manual/LightProbes.html)）接收光照。这在性能开销较小的前提下为场景增加一定深度，非常适合运行在低端平台上的游戏。

URP 的 Baked Lit Shader 是唯一使用烘焙着色模型的着色器。


## 无光照

URP 提供了一些无光照类型（unlit-type）的着色器。使用此类着色器的材质既不会受到实时光照，也不受烘焙光照影响。无光照着色器能让你为场景中的对象创造独特的视觉风格，其编译速度也比含光照的着色器快得多。

下列 URP 着色器不使用光照：
- [Unlit](unlit-shader.md)
- [Particles Unlit](particles-unlit-shader.md)
