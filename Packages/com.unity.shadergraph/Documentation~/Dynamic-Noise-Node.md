Dynamic Noise 节点
===================

[](#description)描述
---------------------------

基于输入 **UV** 生成动态噪声（Dynamic Noise）。

您还可以选择使用两种不同的哈希方法来计算噪声。团结引擎的 Gradient Noise 节点默认使用 **Deterministic** 哈希方法，以确保跨平台的一致性噪声生成效果。

[](#ports)端口
---------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | 输入的 UV 值 |
| Scale | 输入 | Float | 无 | 噪声比例 |
| Time | 输入 | Float | 无 | 时间值 |
| Out | 输出 | Float | 无 | 输出值 |

[](#controls)控件
---------------------

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Hash Type | 下拉菜单 | Deterministic、LegacyMod | 选择用于噪声生成的随机数哈希函数。 |

