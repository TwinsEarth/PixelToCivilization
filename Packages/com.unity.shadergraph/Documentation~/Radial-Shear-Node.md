
Radial Shear 节点
===============

描述
--

将类似于波的径向剪切变形效果应用于输入 **UV** 的值。变形效果的中心参考点由输入 **Center** 定义，而效果的整体强度由输入 **Strength** 的值定义。输入 **Offset** 可用于偏移结果的各个通道。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | 输入 UV 值 |
| Center | 输入 | Vector 2 | 无 | 中心参考点 |
| Strength | 输入 | Float | 无 | 特效的强度 |
| Offset | 输入 | Vector 2 | 无 | 各个通道偏移 |
| Out | 输出 | Vector 2 | 无 | 输出 UV 值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_RadialShear_float(float2 UV, float2 Center, float Strength, float2 Offset, out float2 Out)
{
    float2 delta = UV - Center;
    float delta2 = dot(delta.xy, delta.xy);
    float2 delta_offset = delta2 * Strength;
    Out = UV + float2(delta.y, -delta.x) * delta_offset + Offset;
}

```
