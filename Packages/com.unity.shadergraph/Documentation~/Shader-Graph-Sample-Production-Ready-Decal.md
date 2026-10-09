Decal
======

Decal 允许您在世界特定位置应用局部材质修改。您可以将其视为在墙上涂鸦或在树下撒落的落叶。但 Decal 的用途远不止于此。在这些示例中，我们可以看到 Decal 使物体看起来湿润，使表面看起来像是有流动的水，投射水的光影效果，并将特定材料混合到其他物体上。

Decal 可以在 HDRP 和 URP 中使用，但需要在两个渲染管线中启用。有关 Decal 的使用，请参阅 [HDRP](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/decals.html) 和 [URP](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest/manual/renderer-feature-decal.html) 的文档。

#### 材质投影

此 Decal 使用三轴投影在三维空间中投影材质。它能够正确地将材质投影到与 Decal 体积相交的任何网格上。可以将其用于将地形材质应用于其他物体（如岩石），以便它们更好地与地形融合。

#### 水面光影

当光线透过波动的水面时，水会扭曲和聚焦光线，在水下表面投射出非常有趣的波纹图案。此着色器创建这些波动的光影图案。如果您使用此着色器在水面下放置 Decal，您将获得模仿光线透过水面行为的投射光影。

#### 流动水

此 Decal 创建了流动水在其内部表面上流动的效果。它可用于溪流的河岸和瀑布周围，以支持水流的外观。通过材质参数，您可以控制水流的速度、湿润度和水的透明度，以及流动水法线的强度。

#### 水的湿润度

湿润度 Decal 通过加深颜色和增加光滑度使表面看起来湿润。它使用非常简单的数学运算，并且不需要纹理采样，因此性能效率非常高。可以在水体的岸边使用它，以更好地将水与环境融合。