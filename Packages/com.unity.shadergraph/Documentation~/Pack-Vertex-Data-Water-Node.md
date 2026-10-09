Pack Water Vertex Data 节点
======================

描述
---

此节点将多个水属性打包到顶点上下文的两个 UV 属性中。

高清渲染管线（HDRP）在默认的水面着色器图中使用此节点。请勿修改此节点的设置。有关 [水系统](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/WaterSystem.html) 的更多信息，请参阅 HDRP 文档。

[](#render-pipeline-compatibility) 渲染管线兼容性
---------------------------------------------------------------

| **节点** | **通用渲染管线（URP）** | **高清渲染管线（HDRP）** |
| --- | --- | --- |
| **Pack Water Vertex Data** | 否 | 是 |

[](#ports) 端口
---------------

| **名称** | **方向** | **类型** | **描述** |
| --- | --- | --- | --- |
| **PositionWS** | 输入 | Vector3 | 水面顶点在世界空间中的位置。 |
| **Displacement** | 输入 | Vector3 | 水的垂直和水平位移。 |
| **LowFrequencyHeight** | 输入 | Float | 水面的垂直位移，不包括波纹。 |
| **SSSMask** | 输入 | Float | 定义水面子表面散射区域的遮罩。 |
| **PositionOS** | 输出 | Vector3 | 水面顶点在对象空间中的位置。 |
| **NormalOS** | 输出 | Vector3 | 对象空间中的水面法线。 |
| **uv0** | 输出 | Vector4 | 打包到顶点上下文中的一组 UV 坐标的输入。 |
| **uv1** | 输出 | Vector4 | 打包到顶点上下文中的另一组 UV 坐标的输入。 |