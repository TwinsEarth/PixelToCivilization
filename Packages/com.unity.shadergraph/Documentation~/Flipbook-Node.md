
Flipbook 节点
===========


描述
--

使用向输入 **UV** 提供的 UV 创建翻页或纹理帧动画。纹理层上的区块数量由输入 **Width** 和 **Height** 的值定义。当前区块的索引由输入 **Tile** 的值定义。


此节点可用于创建纹理动画功能，通常用于粒子效果和精灵，方法是将 [Time](Time-Node.md) 提供给输入 **Tile** 并输出到 [Texture Sampler](Sample-Texture-2D-Node.md) 的 UV 输入字段。


UV 数据通常在 0 到 1 的范围内，从 UV 空间的左下角开始。这可以通过 UV 预览左下角的黑色值看出。默认情况下，由于翻页通常从左上角开始，因此会启用参数 **Invert Y**，但可以通过切换 **Invert X** 和 **Invert Y** 参数来更改翻页的方向。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | 输入 UV 值 |
| Width | 输入 | Float | 无 | 水平区块的数量 |
| Height | 输入 | Float | 无 | 垂直区块的数量 |
| Tile | 输入 | Float | 无 | 当前区块索引 |
| Out | 输出 | Vector 2 | 无 | 输出 UV 值 |


控件
--

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Invert X | 开关 | True、False | 如果启用，则从右到左迭代区块 |
| Invert Y | 开关 | True、False | 如果启用，则从上向下迭代区块 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。

```
float2 _Flipbook_Invert = float2(FlipX, FlipY);

void Unity_Flipbook_float(float2 UV, float Width, float Height, float Tile, float2 Invert, out float2 Out)
{
    Tile = fmod(Tile, Width * Height);
    float2 tileCount = float2(1.0, 1.0) / float2(Width, Height);
    float tileY = abs(Invert.y * Height - (floor(Tile * tileCount.x) + Invert.y * 1));
    float tileX = abs(Invert.x * Width - ((Tile - Width * floor(Tile * tileCount.x)) + Invert.x * 1));
    Out = (UV + float2(tileX, tileY)) * tileCount;
}

```
