
Position 节点
===========


描述
--


允许访问网格顶点或片元的**位置**，具体取决于[节点](Node.md)所属图形部分的有效[着色器阶段](Shader-Stage.md)。可使用 **Space** 下拉选单参数选择输出值的坐标空间。


端口
--




| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Out | 输出 | Vector 3 | 无 | 网格顶点/片元的**位置**。 |


控件
--




| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Space | 下拉选单 | Object、View、World、Tangent、Absolute World | 选择 **Position** 输出的坐标空间。 |


World 和 Absolute World
----------------------


Position 节点为 **World** 和 **Absolute World** 空间位置均提供了下拉选项。对于所有可编程渲染管线，**Absolute World** 选项始终返回对象在场景中的绝对世界位置。**World** 选项返回所选的可编程渲染管线的默认世界空间。


[高清渲染管线](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest)使用 [Camera Relative](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest?preview=1&subfolder=/manual/Camera-Relative-Rendering) 作为其默认世界空间。


[通用渲染管线](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.universal@latest)使用 **Absolute World** 作为其默认世界空间。


### 从之间的版本升级


如果在 Shader Graph 6\.7\.0 或更早版本创作的图形上使用 **World** 空间中的 Position 节点，它会自动将选择升级为 **Absolute World**。这可确保该图形上的计算精度始终符合您的预期，因为 **World** 输出可能会改变。


如果在高清渲染管线中使用 **World** 空间中的 Position 节点手动计算[Camera Relative](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest?preview=1&subfolder=/manual/Camera-Relative-Rendering) 世界空间，现在可将该节点从 **Absolute World** 更改为 **World**，从而使用现成可用的 [Camera Relative](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest?preview=1&subfolder=/manual/Camera-Relative-Rendering) 世界空间。



