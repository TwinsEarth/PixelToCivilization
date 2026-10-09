
Tiling And Offset 节点
====================


描述
--

分别根据输入 **Tiling** 和 **Offset** 来平铺和偏移输入 **UV** 的值。这通常用于细节贴图和随[时间](Time-Node.md)滚动纹理。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | 输入 UV 值 |
| Tiling | 输入 | Vector 2 | 无 | 每个通道要应用的平铺量 |
| Offset | 输入 | Vector 2 | 无 | 每个通道要应用的偏移量 |
| Out | 输出 | Vector 2 | 无 | 输出 UV 值 |


生成的代码示例
-------

以下示例代码表示此节点的一种可能结果。

```
void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}

```