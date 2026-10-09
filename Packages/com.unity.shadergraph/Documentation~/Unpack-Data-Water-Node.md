Unpack Water Data 节点
=================

描述
----

此节点解包并输出片段上下文的水属性。

有关 [水系统](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/WaterSystem.html) 的更多信息，请参阅 HDRP 文档。

[](#render-pipeline-compatibility) 渲染管线兼容性
---------------------------------------------------------------

| **节点** | **通用渲染管线（URP）** | **高清渲染管线（HDRP）** |
| --- | --- | --- |
| **Unpack Water Data** | 否 | 是 |

[](#ports) 端口
---------------

| **名称** | **方向** | **类型** | **描述** |
| --- | --- | --- | --- |
| **LowFrequencyHeight** | 输出 | Float | 水面的垂直位移，不包括波纹。 |
| **HorizontalDisplacement** | 输出 | Float | 水面的水平位移。 |
| **SSSMask** | 输出 | Float | 定义水面子表面散射区域的遮罩。 |