Evaluate Simulation Caustics 节点
============================

描述
---

此节点计算[水体焦散](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/WaterSystem-caustics.html)（water caustics）。

该节点将焦散以单色形式输出至红色通道。如果将该节点的输出连接到 **Base Color** 块，则所有通道都是红色。为避免这种情况，请分离输出并仅使用红色通道。无法为焦散添加色调。

除非通过脚本实现，否则焦散不会影响水面以上的区域。例如：

* 如果场景中包含一艘漂浮在水中的船，HDRP 不会将焦散投射到船体高于水面的部分。
* 室内的游泳池不会将焦散反射到墙壁或天花板上。

高清渲染管线（HDRP）在默认的水面着色器图中使用此节点。有关 [水系统](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/WaterSystem.html) 的更多信息，请参阅 HDRP 文档。

[](#render-pipeline-compatibility) 渲染管线兼容性
---------------------------------------------------------------

| **节点** | **通用渲染管线（URP）** | **高清渲染管线（HDRP）** |
| --- | --- | --- |
| **Evaluate Simulation Caustics** | 否 | 是 |

[](#ports) 端口
---------------

| **名称** | **方向** | **类型** | **描述** |
| --- | --- | --- | --- |
| **RefractedPositionWS** | 输入 | Vector3 | 通过水面观察到的水底折射位置，以世界空间表示。 |
| **DistortedWaterNDC** | 输入 | Vector2 | 折射点的屏幕空间位置。 |
| **Caustics** | 输出 | Float | 焦散的强度。 |