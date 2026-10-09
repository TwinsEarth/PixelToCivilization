
Ambient 节点
==========

描述
--

允许访问场景的**环境**（Ambient）颜色值。当 Environment Lighting Source 设置为 **Gradient** 时，[端口](Port.md) **Color/Sky** 将返回值 **Sky Color**。当 Environment Lighting Source 设置为 **Color** 时，端口 **Color/Sky** 将返回值 **Ambient Color**。无论当前 Environment Lighting Source 设置为何值，端口 **Equator** 和 **Ground** 都始终返回值 **Equator Color** 和 **Ground Color**。


注意：仅当进入运行模式或保存当前场景/项目时，才会更新此[节点](Node.md)的值。


注意：此节点的行为未在全局范围内定义。Shader Graph 未定义此节点的函数。但每个渲染管线都为此节点定义了要执行的 HLSL 代码。

不同的渲染管线可能会产生不同的结果。如果要在一个渲染管线中构建希望在两个渲染管线中使用的着色器，请在实际应用之前尝试在这两个管线中对其进行检查。节点可能在一个渲染管线中已定义，而在另一个渲染管线中未定义。如果此节点未定义，则会返回 0（黑色）。


#### 支持的渲染管线


* 通用渲染管线


高清渲染管线**不**支持此节点。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Color/Sky | 输出 | Vector 3 | 无 | Color (Color) 或 Sky (Gradient) 颜色值 |
| Equator | 输出 | Vector 3 | 无 | Equator (Gradient) 颜色值 |
| Ground | 输出 | Vector 3 | 无 | Ground (Gradient) 颜色值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
float3 _Ambient_ColorSky = SHADERGRAPH_AMBIENT_SKY;
float3 _Ambient_Equator = SHADERGRAPH_AMBIENT_EQUATOR;
float3 _Ambient_Ground = SHADERGRAPH_AMBIENT_GROUND;

```

