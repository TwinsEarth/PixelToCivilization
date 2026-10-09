
Parallax Mapping 节点
===================


描述
--


视差贴图（Parallax Mapping）节点允许创建视差效果，这种效果可以对材质的 UV 和深度进行移位以便在材质内产生深度感。此实现使用不考虑遮挡的单步过程。有关呈现的效果的信息，请参阅[高度贴图](https://docs.unity.cn/cn/tuanjiemanual/Manual/StandardShaderMaterialParameterHeightMap.html)页。


如果在包含自定义函数节点或子图形的图形中使用此节点时遇到纹理采样错误，可以通过升级到 10\.3 或更高版本来解决这些问题。


端口
--




| 名称 | **方向** | 类型 | 描述 |
| --- | --- | --- | --- |
| **Heightmap** | 输入 | Texture2D | 用于指定位移深度的纹理。 |
| **Heightmap Sampler** | 输入 | Sampler State | 用于对**高度贴图**进行采样的采样器。 |
| **Amplitude** | 输入 | Float | 应用于高度贴图的高度（以厘米为单位）的乘数。 |
| **UVs** | 输入 | Vector 2 | 采样器用来采样纹理的 UV。 |
| **Parallax UVs** | 输出 | Vector 2 | 添加视差偏移后的 UV。 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
float2 _ParallaxMapping_ParallaxUVs = UVs.xy + ParallaxMapping(Heightmap, Heightmap_Sampler, IN.TangentSpaceViewDirection, Amplitude * 0.01, UVs.xy);

```
