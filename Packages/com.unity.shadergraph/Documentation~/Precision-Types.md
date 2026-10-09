
* [手册](https://docs.unity3d.com/cn/Packages/com.unity.shadergraph@10.5/manual/index)
* Shader Graph 详解
* [精度类型](https://docs.unity3d.com/cn/Packages/com.unity.shadergraph@10.5/manual/Precision-Types)


精度类型
====


描述
--


[Shader Graph](Shader-Graph.md) 中目前有两种**精度类型**。每个节点都可以使用 [Precision Modes](Precision-Modes) 中列出的选项设置自己的**精度类型**。


精度类型
----




| 名称 | 描述 |
| --- | --- |
| Half | 中等精度浮点值；通常为 16 位（范围为 –60000 至 \+60000，具有大约 3 位小数的精度）。`Half` 精度用于短矢量、方向、对象空间位置以及高动态范围颜色。 |
| Float | 最高精度浮点值；一般为 32 位（也就是常规编程语言中的 `float`）。完全的 `float` 精度通常用于世界空间位置、纹理坐标或涉及复杂函数（如三角函数、幂函数或指数函数）的标量计算。 |



