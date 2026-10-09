
Polar Coordinates 节点
====================


描述
--


将输入 **UV** 的值转换为极坐标。在数学中，极坐标系是二维坐标系，其中平面上的每个点由相对于参考点的距离和相对于参考方向的角度确定。


产生的效果是 **UV** 输入的 X 通道转换为相对于指定点（由输入 **Center** 的值指定）的距离值，而该相同输入的 Y 通道转换为相对于该点的旋转角度值。


这些值可以分别由输入 **Radial Scale** 和 **Length Scale** 的值进行缩放。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | 输入 UV 值 |
| Center | 输入 | Vector 2 | 无 | 中心参考点 |
| Radial Scale | 输入 | Float | 无 | 距离值的缩放 |
| Length Scale | 输入 | Float | 无 | 角度值的缩放 |
| Out | 输出 | Vector 2 | 无 | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_PolarCoordinates_float(float2 UV, float2 Center, float RadialScale, float LengthScale, out float2 Out)
{
    float2 delta = UV - Center;
    float radius = length(delta) * 2 * RadialScale;
    float angle = atan2(delta.x, delta.y) * 1.0/6.28 * LengthScale;
    Out = float2(radius, angle);
}

```
