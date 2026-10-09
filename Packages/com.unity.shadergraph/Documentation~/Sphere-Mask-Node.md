
Sphere Mask 节点
==============

描述
--

创建源自输入 **Center** 的球体遮罩。球体是使用 [Distance](Distance-Node.md) 计算的，并使用 **Radius** 和 **Hardness** 输入进行修改。球体遮罩功能适用于 2D 和 3D 空间，并基于 **Coords** 输入中的矢量坐标。这些矢量坐标可以是类似世界空间位置的 3D 坐标，也可以是类似 UV 坐标的 2D 坐标。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Coords | 输入 | 动态矢量 | 无 | 坐标空间输入 |
| Center | 输入 | 动态矢量 | 无 | 球体原点的坐标 |
| Radius | 输入 | Float | 无 | 球体的半径 |
| Hardness | 输入 | Float | 无 | 软化球体的衰减 |
| Out | 输出 | 动态矢量 | 无 | 输出遮罩值 |


生成的代码示例
-------

以下示例代码表示此节点的一种可能结果。

```
void Unity_SphereMask_float4(float4 Coords, float4 Center, float Radius, float Hardness, out float4 Out)
{
    Out = 1 - saturate((distance(Coords, Center) - Radius) / (1 - Hardness));
}

```

