Evaluate Foam Data 节点
==================

描述
---

此节点计算水面泡沫的强度。

该节点以单色形式在红色通道输出泡沫数据。如果将该节点的输出连接到 **Base Color** 块，所有通道将显示为红色。为避免此问题，可以将输出拆分，并仅使用红色通道。无法对泡沫应用色调（tint）。

高清渲染管线（HDRP）在默认的水面着色器图中使用此节点。有关 [水系统](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/WaterSystem.html) 的更多信息，请参阅 HDRP 文档。

[](#render-pipeline-compatibility) 渲染管线兼容性
---------------------------------------------------------------

| **节点** | **通用渲染管线（URP）** | **高清渲染管线（HDRP）** |
| --- | --- | --- |
| **计算泡沫数据** | 否 | 是 |

[](#ports) 端口
---------------
| **名称** | **方向** | **类型** | **描述** |
| --- | --- | --- | --- |
| **SurfaceGradient** | 输入 | Vector3 | 法线的扰动，作为表面梯度。 |
| **LowFrequencySurfaceGradient** | 输入 | Vector3 | 低频法线的扰动，作为表面梯度。低频法线是没有高频细节（如波纹）的水面法线。 |
| **SimulationFoam** | 输入 | Float | 泡沫的数量。HDRP 在默认的水面着色器图中使用此属性来从模拟中获取泡沫数据。 |
| **CustomFoam** | 输入 | Float | 如果你创建了自定义泡沫，则指定泡沫的数量。 |
| **SurfaceGradient** | 输出 | Vector3 | 计算得到的水面法线，作为表面梯度。 |
| **Foam** | 输出 | Float | 泡沫的数量与泡沫纹理的组合。 |
| **Smoothness** | 输出 | Float | 水面光滑度。有关此属性的更多信息，请参阅 [与水系统相关的设置和属性](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/WaterSystem-Properties.html)。 |