Graph Settings 选项卡
=================


描述
--

[Graph Inspector](Internal-Inspector.md) 上的 Graph Settings 选项卡可以更改影响整个 Shader Graph 的设置。

![](images/GraphSettings_Menu.png)


### Graph Settings 选项

| 菜单项 | 描述 |
| --- | --- |
| Precision | 一种[精度模式](Precision-Modes.md)下拉菜单，用于设置整个图形的默认精度。您可以在图表中的节点级别覆盖 Precision （精度） 设置。 |
| Preview Mode | （仅限子图）选项包括 **Inherit**、**Preview 2D** 和 **Preview 3D** 。 |
| Active Targets | 包含您选择的 Targets 的列表。您可以使用 Add(+) 和 Remove(-) 按钮添加或删除条目。Shader Graph 支持三个目标：[通用渲染管线（URP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest)、[高清渲染管线（HDRP）](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest)和[内置 Built-in 渲染管线](https://docs.unity.cn/cn/tuanjiemanual/Manual/built-in-render-pipeline.html)。特定于目标的设置显示在标准设置选项的下方。显示的 Target 特定设置会根据您选择的 Targets 而变化。 |
