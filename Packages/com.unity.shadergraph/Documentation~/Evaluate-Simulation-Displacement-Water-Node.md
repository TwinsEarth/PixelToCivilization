Evaluate Water Simulation Displacement 节点
======================================

描述
---

此节点计算水面位移（water surface displacement）。

高清渲染管线（HDRP）在默认的水面着色器图中使用此节点。有关 [水系统](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/WaterSystem.html) 的更多信息，请参阅 HDRP 文档。

[](#render-pipeline-compatibility) 渲染管线兼容性
---------------------------------------------------------------

| **节点** | **通用渲染管线（URP）** | **高清渲染管线（HDRP）** |
| --- | --- | --- |
| **Evaluate Water Simulation Displacement** | 否 | 是 |

[](#ports) 端口
---------------

| **名称** | **方向** | **类型** | **描述** |
| --- | --- | --- | --- |
| **PositionWS** | 输入 | Vector3 | 水面顶点的位置，以世界空间表示。 |
| **BandsMultiplier** | 输入 | Vector4 | 每个水波带的位移衰减量。水波带是不同的波频率，用于产生海浪、波动或水面波纹。 |
| **Displacement** | 输出 | Vector3 | 水面的垂直和水平位移。 |
| **LowFrequencyHeight** | 输出 | Float | 水面的垂直位移，不包括波纹。 |
| **SSSMask** | 输出 | Float | 定义水面具有次表面散射的区域的遮罩。 |