Evaluate Scattering Color 节点
=========================

描述
---

此节点计算水体的散射漫反射颜色。

高清渲染管线（HDRP）在默认的水面着色器图中使用此节点。有关 [水系统](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/WaterSystem.html) 的更多信息，请参阅 HDRP 文档。

[](#render-pipeline-compatibility) 渲染管线兼容性
---------------------------------------------------------------

| **节点** | **通用渲染管线（URP）** | **高清渲染管线（HDRP）** |
| --- | --- | --- |
| **Evaluate Scattering Color** | 否 | 是 |

[](#ports) 端口
---------------

| **名称** | **方向** | **类型** | **描述** |
| --- | --- | --- | --- |
| **AbsorptionTint** | 输入 | Vector3 | 吸收因子，HDRP 使用该因子在水面颜色与折射的水下颜色之间进行混合。 |
| **LowFrequencyHeight** | 输入 | Float | 水面的垂直位移，不包含波纹。 |
| **HorizontalDisplacement** | 输入 | Float | 水面的水平位移。 |
| **SSSMask** | 输入 | Float | 用于定义水面次表面散射区域的遮罩。 |
| **DeepFoam** | 输入 | Float | 水下泡沫的量。 |
| **ScatteringColor** | 输出 | Vector3 | 水的漫反射颜色。 |