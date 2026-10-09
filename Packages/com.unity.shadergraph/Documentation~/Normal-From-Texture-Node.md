
Normal From Texture 节点
======================


描述
--


将输入 **Texture** 定义的高度贴图转换为法线贴图。UV 值和采样器状态可分别由输入 **UV** 和 **Sampler** 定义。如果这些端口未进行任何连接，它们将使用输入中的默认值。请参阅[端口绑定](Port-Bindings.md)以了解更多信息。


可以使用输入 **Offset** 和 **Strength** 来定义所创建的法线贴图的强度，其中 **Offset** 定义法线细节的最大距离，而 **Strength** 用作结果的乘数。


如果在包含自定义函数节点或子图形的图形中使用此节点时遇到纹理采样错误，可以通过升级到 10\.3 或更高版本来解决这些问题。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Texture | 输入 | Texture | 无 | 高度贴图 |
| UV | 输入 | Vector 2 | UV | 纹理坐标 |
| Sampler | 输入 | 采样器状态 | 无 | **纹理**采样器 |
| Offset | 输入 | Float | 无 | 样本偏移量 |
| Strength | 输入 | Float | 无 | 强度乘数 |
| Out | 输出 | Vector 3 | 无 | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_NormalFromTexture_float(Texture texture, SamplerState Sampler, float2 UV, float Offset, float Strength, out float3 Out)
{
    Offset = pow(Offset, 3) * 0.1;
    float2 offsetU = float2(UV.x + Offset, UV.y);
    float2 offsetV = float2(UV.x, UV.y + Offset);
    float normalSample = Texture.Sample(Sampler, UV);
    float uSample = Texture.Sample(Sampler, offsetU);
    float vSample = Texture.Sample(Sampler, offsetV);
    float3 va = float3(1, 0, (uSample - normalSample) * Strength);
    float3 vb = float3(0, 1, (vSample - normalSample) * Strength);
    Out = normalize(cross(va, vb));
}

```

