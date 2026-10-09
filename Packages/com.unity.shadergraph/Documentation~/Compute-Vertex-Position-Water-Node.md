Compute Water Vertex Position 节点
=============================

描述
----

此节点提供访问水面网格顶点位置的功能。它在水面渲染中替代了 [Position 节点](Position-Node.md)。

高清渲染管线（HDRP）在默认的水面着色器图中使用此节点。请勿修改该节点的设置。有关 [水系统](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/WaterSystem.html) 的更多信息，请参阅 HDRP 文档。

[](#render-pipeline-compatibility) 渲染管线兼容性
---------------------------------------------------------------

| **节点** | **通用渲染管线（URP）** | **高清渲染管线（HDRP）** |
| --- | --- | --- |
| **Compute Water Vertex Position** | 否 | 是 |

[](#ports) 端口
---------------
| **名称** | **方向** | **类型** | **描述** |
| --- | --- | --- | --- |
| **PositionWS** | 输出 | Vector3 | 水面顶点在世界空间中的位置。 |