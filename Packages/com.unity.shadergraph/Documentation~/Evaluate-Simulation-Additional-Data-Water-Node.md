Evaluate Simulation Additional Data 节点
===================================

描述
---

此节点提供对水体表面泡沫、表面梯度和深层泡沫的访问。您还可以使用此节点来衰减每个[水波段](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/WaterSystem-simulation.html#simulation-bands)的法线。

高清渲染管线（HDRP）在默认的水面着色器图中使用此节点从水体模拟中获取数据。请勿修改此节点的设置。有关 [水系统](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/WaterSystem.html) 的更多信息，请参阅 HDRP 文档。

[](#render-pipeline-compatibility) 渲染管线兼容性
---------------------------------------------------------------

| **节点** | **通用渲染管线（URP）** | **高清渲染管线（HDRP）** |
| --- | --- | --- |
| **Evaluate Simulation Additional Data** | 否 | 是 |

[](#ports) 端口
---------------

| **名称** | **方向** | **类型** | **描述** |
| --- | --- | --- | --- |
| **BandsMultiplier** | 输入 | Vector4 | 用于衰减每个水波段位移的量。水波段代表不同的波频率，形成水面上的波涛、波动或波纹。 |
| **SurfaceGradient** | 输出 | Vector3 | 法线扰动，作为表面梯度。 |
| **LowFrequencySurfaceGradient** | 输出 | Vector3 | 低频法线的扰动，作为表面梯度。低频法线是没有高频细节（如波纹）的水面法线。 |
| **SurfaceFoam** | 输出 | Float | 表面泡沫的量。 |
| **DeepFoam** | 输出 | Float | 水面下的泡沫量。 |