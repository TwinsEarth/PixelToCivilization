
Normal Unpack 节点
================

描述
--

解压缩由输入 **In** 定义的法线贴图。针对在纹理导入设置 (Texture Import Settings) 中定义为**法线贴图 (Normal Map)** 的纹理，此节点可以在采样该纹理时对其进行解压缩，就像是默认纹理一样。

请注意，在大多数情况下，此节点是不必要的，因为当使用 [Sample Texture 2D](Sample-Texture-2D-Node.md) 或 [Triplanar](Triplanar-Node.md) 节点对法线贴图进行采样时，应该通过将其 **Type** 参数设置为 **Normal** 对法线贴图进行这样的采样。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| In | 输入 | Vector 4 | 无 | 输入值 |
| Out | 输出 | Vector 3 | 无 | 输出值 |


控件
--




| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Space | 下拉选单 | Tangent、Object | 设置输入法线的坐标空间。 |


生成的代码示例
-------


以下示例代码表示此节点在每个 **Space** 模式下的一种可能结果。


**Tangent**



```
void Unity_NormalUnpack_float(float4 In, out float3 Out)
{
    Out = UnpackNormalmapRGorAG(In);
}

```
**Object**



```
void Unity_NormalUnpackRGB_float(float4 In, out float3 Out)
{
    Out = UnpackNormalmapRGB(In);
}

```

