Baked GI 节点
=============

[](#description)描述
---------------------------

提供顶点或片元位置处的**烘焙全局光照 (Baked GI)** 值访问。需要**位置**和**法线**输入用于光照探针采样，以及用于所有可能光照贴图采样的光照贴图坐标 **静态 UV** 和 **动态 UV**。

注意：此[节点](Node.md)的行为未在全局范围内定义。Shader Graph 不定义此节点的功能，而是由每个渲染管线决定要为此节点执行的 HLSL 代码。

不同的渲染管线可能产生不同的结果。如果您在一种渲染管线中构建了希望在两种管线中使用的着色器，请尝试在生产之前在两种管线中进行检查。一个节点可能在一种渲染管线中被定义，而在另一种管线中未定义。如果此节点未定义，它将返回 0（黑色）。

### [](#unity-render-pipelines-support)支持的渲染管线

此节点兼容高清渲染管线 (HDRP) 和通用渲染管线 (URP)。但在任一管线中，均无法在无光照着色器中使用此节点。

[](#ports)端口
---------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Position | 输入 | Vector 3 | 位置（world space） | 网格顶点/片元的**位置** |
| Normal | 输入 | Vector 3 | 法线（world space） | 网格顶点/片元的**法线** |
| Static UV | 输入 | Vector 2 | UV1 | 静态光照贴图的光照贴图坐标 |
| Dynamic UV | 输入 | Vector 2 | UV2 | 动态光照贴图的光照贴图坐标 |
| Out | 输出 | Vector 3 | 无 | 输出的颜色值 |

[](#controls)控件
---------------------

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Apply Lightmap Scaling | 切换 | True, False | 启用时，光照贴图会自动缩放和偏移。 |

[](#generated-code-example)生成代码示例
-------------------------------------------------

以下示例代码代表了此节点的一个可能输出：

```
void Unity_BakedGI_float(float3 Position, float3 Normal, float2 StaticUV, float2 DynamicUV, out float Out)
{
    Out = SHADERGRAPH_BAKED_GI(Position, Normal, StaticUV, DynamicUV, false);
}
```