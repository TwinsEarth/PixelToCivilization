Instance ID 节点
================

[](#description)描述
---------------------------

当团结引擎使用 GPU 实例化进行渲染时，会为每个几何体分配一个 **实例 ID**（Instance ID）。

使用此节点在 [`Graphics.DrawMeshInstanced`](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/Graphics.DrawMeshInstanced.html) 调用中捕获 **Instance ID** 值。

当团结引擎不使用 GPU 实例化进行渲染时，此 ID 为 0。

当团结引擎使用动态实例化时，实例 ID 在多个帧之间可能不一致。


[](#ports)端口
---------------

| Name | Direction | Type | Binding | Description |
| --- | --- | --- | --- | --- |
| Out | 输出 | Float | 无 | 给定实例化绘制调用的网格的 **Instance ID**。 |