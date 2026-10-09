
Reflection Probe 节点
===================


描述
--


允许访问对象最近的**反射探针（Reflection Probe）**。需要 **Normal** 和 **View Direction** 输入才能采样探针。可以使用 **LOD** 输入在不同的细节级别进行采样，从而获得模糊效果。


注意：此[节点](Node.md)的行为未在全局范围内定义。Shader Graph 未定义此节点的函数。但每个渲染管线都为此节点定义了要执行的 HLSL 代码。


不同的渲染管线可能会产生不同的结果。如果要在一个渲染管线中构建希望在两个渲染管线中使用的着色器，请在实际应用之前尝试在这两个管线中对其进行检查。节点可能在一个渲染管线中已定义，而在另一个渲染管线中未定义。如果此节点未定义，则会返回 0（黑色）。


#### 支持的渲染管线


* 通用渲染管线


高清渲染管线**不**支持此节点。


端口
--




| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| View Dir | 输入 | Vector 3 | 视图方向（对象空间） | 网格的视图方向 |
| Normal | 输入 | Vector 3 | 法线（对象空间） | 网格的法线Vector |
| LOD | 输入 | Float | 无 | 采样的细节级别 |
| Out | 输出 | Vector 3 | 无 | 输出颜色值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_ReflectionProbe_float(float3 ViewDir, float3 Normal, float LOD, out float3 Out)
{
    Out = SHADERGRAPH_REFLECTION_PROBE(ViewDir, Normal, LOD);
}

```

