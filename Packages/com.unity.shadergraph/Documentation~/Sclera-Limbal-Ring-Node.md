
Sclera Limbal Ring 节点
=====================

描述
---

计算巩膜缘环（Sclera ring）的强度，这是眼睛的明暗特征。


渲染管线兼容性
-------

| **节点** | **通用渲染管线 (URP)** | **高清渲染管线 (HDRP)** |
| --- | --- | --- |
| **Sclera Limbal Ring 节点** | 否 | 是 |


端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| **PositionOS** | 输入 | Vector3 | 要着色的片元在对象空间中的位置。 |
| **View Direction OS** | 输入 | Vector3 | 对象空间中的入射线方向。或者来自光栅化中的摄像机，或者来自光线追踪中的上一反弹。 |
| **IrisRadius** | 输入 | Float | 虹膜在所用模型中的半径。对于默认模型，此值应为 **0\.225**。 |
| **LimbalRingSize** | 输入 | Float | 标准化的 [0, 1] 值，定义角膜缘环相对大小。 |
| **LimbalRingFade** | 输入 | Float | 标准化的 [0, 1] 值，定义角膜缘环的淡出强度。 |
| **LimbalRing Intensity** | 输入 | Float | 定义角膜缘环明暗程度的正值。 |
| **Iris Limbal Ring Color** | 输出 | Color | 角膜缘环的强度（黑色级别）。 |

