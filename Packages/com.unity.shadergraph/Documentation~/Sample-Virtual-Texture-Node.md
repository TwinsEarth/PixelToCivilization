Sample Virtual Texture 节点
=========================

描述
--

对[虚拟纹理](Property-Types#virtual-texture)进行采样，并返回 Vector 4 （最多达四个）颜色值以在着色器中使用。可以使用 UV 输入来覆盖 UV 坐标。Sample Virtual Texture 节点将一个 UV 坐标作为输入，并使用该 UV 坐标对 Virtual Texture 中的所有纹理进行采样。


如果要使用 Sample Virtual Texture 节点对法线贴图进行采样，请导航到要作为法线贴图进行采样的每个图层，打开 **Layer Type** 下拉选单，然后选择 **Normal**。


默认情况下，此节点只能在片元着色器阶段中使用。有关如何使用此节点或如何配置以在顶点着色器阶段使用的更多信息，请参见[Using Streaming Virtual Texturing in Shader Graph](https://docs.unity.cn/cn/tuanjiemanual/Manual/svt-use-in-shader-graph.html)。


如果在项目中禁用虚拟纹理 (Virtual Texturing)，则此节点的工作方式与 [Sample 2D Texture 节点](Sample-Texture-2D-Node.md)相同，并对每个纹理执行标准的 2D 采样。


您必须将 Sample Virtual Texture 节点连接到要编译的 Shader Graph 资源的 Virtual Texture 属性。如果未将节点连接到属性，则会出现错误，指示该节点需要连接。


有关 Streaming Virtual Texturing 的信息，请参见[Streaming Virtual Texturing](https://docs.unity.cn/cn/tuanjiemanual/Manual/svt-streaming-virtual-texturing.html)。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| UV | 输入 | Vector 2 | UV | UV 坐标。 |
| VT | 输入 | 虚拟纹理 | 无 | 要采样的虚拟纹理。必须连接到 Virtual Texture 属性。 |
| Out | 输出 | Vector 4 | 无 | 第 1 层的 RGBA 输出值。 |
| Out2 | 输出 | Vector 4 | 无 | 第 2 层的 RGBA 输出值。 |
| Out3 | 输出 | Vector 4 | 无 | 第 3 层的 RGBA 输出值。 |
| Out4 | 输出 | Vector 4 | 无 | 第 4 层的 RGBA 输出值。 |


设置
--
Sample Virtual Texture 节点提供了多个设置用于指定其行为。这些设置可与项目中设置的任何脚本结合使用。要查看设置，打开 [Graph Inspector](Internal-Inspector.md) 选择该节点。有关更多信息，请参阅[Streaming Virtual Texturing](https://docs.unity.cn/cn/tuanjiemanual/Manual/svt-streaming-virtual-texturing.html)。


| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Lod Mode | 下拉选单 | Automatic、Lod Level、Lod Bias、Derivatives | 设置采样纹理时要使用的特定 Lod 模式。 |
| Quality | 下拉选单 | Low、High | 设置采样纹理时要使用的特定质量模式。 |
| Automatic Streaming | 开关 | Enabled/Disabled | 决定节点是使用自动流媒体还是手动流媒体。|
| Enable Global Mip Bias | 开关 | Enabled/Disabled | 启用团结引擎在运行时自动施加的全局 mip 贴图偏置。团结引擎会在某些动态分辨率缩放算法中设置该偏置，以改善细节重建。|
| Layer 1 Type | 下拉选单 | Default、Normal | 第 1 层的纹理类型。 |
| Layer 2 Type | 下拉选单 | Default、Normal | 第 2 层的纹理类型。 |
| Layer 3 Type | 下拉选单 | Default、Normal | 第 3 层的纹理类型。此选项仅在 Virtual Texture 至少具有 3 层时才会出现。 |
| Layer 4 Type | 下拉选单 | Default、Normal | 第 4 层的纹理类型。此选项仅在 Virtual Texture 至少具有 4 层时才会出现。 |



生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
float4 SampleVirtualTexture(float2 uv, VTPropertyWithTextureType vtProperty, out float4 Layer0)
{
    VtInputParameters vtParams;
    vtParams.uv = uv;
    vtParams.lodOrOffset = 0.0f;
    vtParams.dx = 0.0f;
    vtParams.dy = 0.0f;
    vtParams.addressMode = VtAddressMode_Wrap;
    vtParams.filterMode = VtFilter_Anisotropic;
    vtParams.levelMode = VtLevel_Automatic;
    vtParams.uvMode = VtUvSpace_Regular;
    vtParams.sampleQuality = VtSampleQuality_High;
    #if defined(SHADER_STAGE_RAY_TRACING)
    if (vtParams.levelMode == VtLevel_Automatic || vtParams.levelMode == VtLevel_Bias)
    {
        vtParams.levelMode = VtLevel_Lod;
        vtParams.lodOrOffset = 0.0f;
    }
    #endif
    StackInfo info = PrepareVT(vtProperty.vtProperty, vtParams);
    Layer0 = SampleVTLayerWithTextureType(vtProperty, vtParams, info, 0);
    return GetResolveOutput(info);
}

```

