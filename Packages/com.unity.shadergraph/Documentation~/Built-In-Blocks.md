
内置 Block
========

顶点 Block
--------

|  | 名称 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| ![](./Images/Blocks-Vertex-Position.png) |Position | Vector 3 | 对象空间位置 | 定义每个顶点的绝对对象空间顶点位置。 |
| ![](./Images/Blocks-Vertex-Normal.png) | Normal | Vector 3 | 对象空间法线 | 定义每个顶点的绝对对象空间顶点法线。 |
| ![](./Images/Blocks-Vertex-Tangent.png) | Tangent | Vector 3 | 对象空间切线 | 定义每个顶点的绝对对象空间顶点切线。 |
|![](./Images/Blocks-Vertex-Color.png)| Color | Vector 4 | 顶点颜色 | 定义顶点颜色。预期范围为 0 - 1。 |


片元 Block
--------

|  | 名称 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| ![](./Images/Blocks-Fragment-Base-Color.png) | Base Color | Vector 3 | 无 | 定义材质的基色值。期望范围 0 \- 1。 |
| ![](./Images/Blocks-Fragment-NormalTS.png) | Normal (Tangent Space) | Vector 3 | 切线空间法线 | 定义材质在切线空间中的法线值。 |
| ![](./Images/Blocks-Fragment-NormalOS.png) | Normal (Object Space) | Vector 3 | 对象空间法线 | 定义材质在对象空间中的法线值。 |
| ![](./Images/Blocks-Fragment-NormalWS.png) | Normal (World Space) | Vector 3 | 世界空间法线 | 定义材质在世界空间中的法线值。 |
| ![](./Images/Blocks-Fragment-Emission.png) | Emission | Vector 3 | 无 | 定义材质的发光颜色值。期望正值。 |
| ![](./Images/Blocks-Fragment-Metallic.png) | Metallic | Float | 无 | 定义材料的金属度值，其中 0 表示非金属，1 表示金属。 |
| ![](./Images/Blocks-Fragment-Specular.png) | Specular | Vector 3 | 无 | 定义材质的镜面反射颜色值。期望范围 0 \- 1。 |
| ![](./Images/Blocks-Fragment-Smoothness.png) | Smoothness | Float | 无 | 定义材质的平滑度值。期望范围 0 \- 1。 |
| ![](./Images/Blocks-Fragment-Ambient-Occlusion.png) | Ambient Occlusion | Float | 无 | 定义材质的环境光遮挡值。期望范围 0 \- 1。 |
| ![](./Images/Blocks-Fragment-Alpha.png) | Alpha | Float | 无 | 定义材质的 Alpha 值。用于透明度和/或 Alpha 裁剪。期望范围 0 \- 1。 |
| ![](./Images/Blocks-Fragment-Alpha-Clip-Threshold.png) | Alpha Clip Threshold | Float | 无 | Alpha 低于此值的片元将被丢弃。期望范围 0 \- 1。 |
