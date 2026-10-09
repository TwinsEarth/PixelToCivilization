
Parallax Occlusion Mapping 节点
=============================


描述
--


视差遮挡贴图（Parallax Occlusion Mapping, POM）节点允许创建视差效果，这种效果可以对材质的 UV 和深度进行移位以便在材质内产生深度感。


如果在包含自定义函数节点或子图形的图形中使用此节点时遇到纹理采样错误，可以通过升级到 10\.3 或更高版本来解决这些问题。


将相同的 Texture2D 分配给 POM 节点和 [Sample Texture 2D 节点](Sample-Texture-2D-Node.md)时，需要避免 UV 坐标转换两次。为避免出现这种情况，请将 [Split Texture Transform 节点](Split-Texture-Transform-Node.md)的 **Texture Only** 端口连接到 Sample Texture 2D 节点的 **UV** 端口。

![](./Images/ParallaxOcclusionMappingThumb.png)

端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| **Heightmap** | 输入 | Texture2D | 用于指定位移深度的纹理。 |
| **Heightmap Sampler** | 输入 | Sampler State | 用于对**高度贴图**（Heightmap）进行采样的采样器。 |
| **Amplitude** | 输入 | Float | 应用于**高度贴图**的高度（以厘米为单位）的乘数。 |
| **Steps** | 输入 | Float | 算法线性搜索执行的步骤数。 |
| **UVs** | 输入 | Vector 2 | 采样器用来采样纹理的 UV。 |
| **Tiling** | 输入 | Vector 2 | 应用于输入 UV 的平铺。 |
| **Offet** | 输入 | Vector 2 | 应用于输入 UV 的偏移量。 |
| **Primitive Size** | 输入 | Vector 2 | 对象空间中 UV 空间的大小。例如，内置的平面网格的原始大小为 (10,10)。 |
| **Lod** | 输入 | Float | 用于采样**高度贴图**的细节级别。该值应始终为正值。 |
| **Lod Threshold** | 输入 | Float | POM 效果开始淡出的**高度贴图** Mip 级别。这等效于高清渲染管线 (HDRP) [光照材质](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.high-definition@latest/manual/Lit-Shader.html)中的 **Fading Mip Level Start** 属性。 |
| **Pixel Depth Offset** | 输出 | Float | 应用于深度缓冲区以产生深度感的偏移。要启用依赖深度缓冲区的效果，例如阴影和屏幕空间环境光遮挡，请将此输出连接到主节点上的**深度偏移 (Depth Offset)**。 |
| **Parallax UVs** | 输出 | Vector 2 | 添加视差偏移后的 UV。 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。

```
float3 ParallaxOcclusionMapping_ViewDir = IN.TangentSpaceViewDirection * GetDisplacementObjectScale().xzy;
float ParallaxOcclusionMapping_NdotV = ParallaxOcclusionMapping_ViewDir.z;
float ParallaxOcclusionMapping_MaxHeight = Amplitude * 0.01;
ParallaxOcclusionMapping_MaxHeight *= 2.0 / ( abs(Tiling.x) + abs(Tiling.y) );

float2 ParallaxOcclusionMapping_UVSpaceScale = ParallaxOcclusionMapping_MaxHeight * Tiling / PrimitiveSize;

// Transform the view vector into the UV space.
float3 ParallaxOcclusionMapping_ViewDirUV    = normalize(float3(ParallaxOcclusionMapping_ViewDir.xy * ParallaxOcclusionMapping_UVSpaceScale, ParallaxOcclusionMapping_ViewDir.z)); // TODO: skip normalize

PerPixelHeightDisplacementParam ParallaxOcclusionMapping_POM;
ParallaxOcclusionMapping_POM.uv = UVs.xy;

float ParallaxOcclusionMapping_OutHeight;
float2 _ParallaxOcclusionMapping_ParallaxUVs = UVs.xy + ParallaxOcclusionMapping(Lod, Lod_Threshold, Steps, ParallaxOcclusionMapping_ViewDirUV, ParallaxOcclusionMapping_POM, ParallaxOcclusionMapping_OutHeight);

float _ParallaxOcclusionMapping_PixelDepthOffset = (ParallaxOcclusionMapping_MaxHeight - ParallaxOcclusionMapping_OutHeight * ParallaxOcclusionMapping_MaxHeight) / max(ParallaxOcclusionMapping_NdotV, 0.0001);
```