Evaluate Refraction Data 节点
========================

描述
---

此节点计算水面的折射效果。

高清渲染管线（HDRP）在默认的水面着色器图中使用此节点。有关 [水系统](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/WaterSystem.html) 的更多信息，请参阅 HDRP 文档。

[](#render-pipeline-compatibility) 渲染管线兼容性
---------------------------------------------------------------

| **节点** | **通用渲染管线（URP）** | **高清渲染管线（HDRP）** |
| --- | --- | --- |
| **Evaluate Refraction Data** | 否 | 是 |

[](#ports) 端口
---------------

| **名称** | **方向** | **类型** | **描述** |
| --- | --- | --- | --- |
| **NormalWS** | 输入 | Vector3 | 世界空间中的水面法线。 |
| **LowFrequencyNormalWS** | 输入 | Vector3 | 世界空间中的低频水面法线，即不包含高频细节（如波纹）的水面法线。 |
| **RefractedPositionWS** | 输出 | Vector3 | 观察水面时的折射水底位置，位于世界空间中。 |
| **DistortedWaterNDC** | 输出 | Vector2 | 折射点的屏幕空间位置。 |
| **AbsorptionTint** | 输出 | Vector3 | 吸收因子，HDRP 使用该因子在水面颜色与折射的水下颜色之间进行混合。 |