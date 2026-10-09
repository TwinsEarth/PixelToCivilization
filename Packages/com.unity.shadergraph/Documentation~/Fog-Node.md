
Fog 节点
======


描述
--


允许访问场景的 **Fog** 参数。


注意：此[节点](Node.md)的行为未在全局范围内定义。Shader Graph 未定义此节点的函数。但每个渲染管线都为此节点定义了要执行的 HLSL 代码。


不同的渲染管线可能会产生不同的结果。如果要在一个渲染管线中构建希望在两个渲染管线中使用的着色器，请在实际应用之前尝试在这两个管线中对其进行检查。节点可能在一个渲染管线中已定义，而在另一个渲染管线中未定义。如果此节点未定义，则会返回 0（黑色）。


#### 支持的渲染管线

* 通用渲染管线（URP）

高清渲染管线（HDRP）**不**支持此节点。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Position | 输出 | Vector 3 | 位置（object space） | 网格顶点/片元的位置 |
| Color | 输出 | Vector 4 | 无 | 雾效颜色 |
| Density | 输出 | Float | 无 | 顶点或片元的裁剪空间深度处的雾效强度 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_Fog_float(float3 Position, out float4 Color, out float Density)
{
    SHADERGRAPH_FOG(Position, Color, Density);
}

```

