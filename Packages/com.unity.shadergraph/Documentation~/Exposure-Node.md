
Exposure 节点
====


曝光节点（Exposure Node） 允许从当前帧或上一帧获取摄像机的曝光值。


渲染管线兼容性
-------

| **节点** | **通用渲染管线 (URP)** | **高清渲染管线 (HDRP)** |
| --- | --- | --- |
| Exposure | 否 | 是 |


端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| **Output** | 输出 | Float | 曝光值。 |


曝光类型
----

可以使用曝光类型 (Exposure Type) 来选择要获取的曝光值。


| 名称 | 描述 |
| --- | --- |
| **CurrentMultiplier** | 从当前帧获取摄像机的曝光值。 |
| **InverseCurrentMultiplier** | 从当前帧获取摄像机的曝光值的倒数。 |
| **PreviousMultiplier** | 从上一帧获取摄像机的曝光值。 |
| **InversePreviousMultiplier** | 从上一帧获取摄像机的曝光值的倒数。 |



