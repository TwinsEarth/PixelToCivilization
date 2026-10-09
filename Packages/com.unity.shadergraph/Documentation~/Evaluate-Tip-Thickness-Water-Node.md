Evaluate Tip Thickness 节点
======================

此节点计算波尖处水体的厚度。

高清渲染管线（HDRP）在默认的水面着色器图中使用此节点。有关 [水系统](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/WaterSystem.html) 的更多信息，请参阅 HDRP 文档。

[](#render-pipeline-compatibility) 渲染管线兼容性
---------------------------------------------------------------

| **节点** | **通用渲染管线（URP）** | **高清渲染管线（HDRP）** |
| --- | --- | --- |
| **Evaluate Tip Thickness** | 否 | 是 |

[](#ports) 端口
---------------

| **名称** | **方向** | **类型** | **描述** |
| --- | --- | --- | --- |
| **LowFrequencyNormal** | 输入 | Vector3 | 水面低频法线，以世界空间表示。此法线不包含高频细节（如波纹）。 |
| **LowFrequencyHeight** | 输入 | Float | 水面的垂直位移，不包括波纹。 |
| **TipThickness** | 输出 | Float | 波尖处水体的厚度。 |