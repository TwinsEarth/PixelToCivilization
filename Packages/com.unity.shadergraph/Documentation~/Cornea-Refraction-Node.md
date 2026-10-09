
Cornea Refraction 节点
====================

描述
---

该节点在对象空间（object space）中执行注视光线的折射（refraction of the view ray），并返回获得的对象空间位置。这用于模拟注视眼睛时可以看到的折射。


渲染管线兼容性
-------

| **节点** | **通用渲染管线 (URP)** | **高清渲染管线 (HDRP)** |
| --- | --- | --- |
| **Cornea Refraction 节点** | 否 | 是 |


端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| **Position OS** | 输入 | Vector3 | 对象空间中要着色的片元的位置。 |
| **View Direction OS** | 输入 | Vector3 | 对象空间中的入射线方向。或者来自光栅化中的摄像机，或者来自光线追踪中的上一反弹。 |
| **Cornea Normal OS** | 输入 | Vector3 | 对象空间中眼睛表面的法线。 |
| **Cornea IOR** | 输入 | Float | 眼睛的折射率（默认为 **1.333**）。 |
| **Iris Plane Offset** | 输入 | Float | 角膜末端与虹膜平面之间的距离。对于默认模型，此值应为 **0.02** |
| **RefractedPositionOS** | 输出 | Vector3 | 对象空间中，折射点在虹膜平面上的位置。 |

