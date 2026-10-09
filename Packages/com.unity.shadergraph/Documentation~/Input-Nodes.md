
输入节点
====


基本 Basic
--

| [Boolean](Boolean-Node.md) | [Color](Color-Node.md) |
| --- | --- |
| ![Image](images/BooleanNodeThumb.png) | ![](images/ColorNodeThumb.png) |
| 在着色器中定义一个常量布尔值。 | 使用 Color 字段在着色器中定义一个常量 Vector 4 值。 |
| [**Constant**](Constant-Node.md) | [**Integer**](Integer-Node.md) |
| ![Image](images/ConstantNodeThumb.png) | ![Image](images/IntegerNodeThumb.png) |
| 在着色器中定义一个 Float 数学常量值。 | 使用 Integer 字段在着色器中定义一个常量 Float 值。 |
| [**Slider**](Slider-Node.md) | [**Time**](Time-Node.md) |
| ![Image](images/SliderNodeThumb.png) | ![Image](images/TimeNodeThumb.png) |
| 使用 Slider 字段在着色器中定义一个常量 Float 值。 | 允许访问着色器中的各种 Time 参数。 |
| [**Float**](Float.md) | [**Vector 2**](Vector-2-Node.md) |
| ![Image](images/Vector1NodeThumb.png) | ![Image](images/Vector2NodeThumb.png) |
| 在着色器中定义一个 Float 值。 | 在着色器中定义一个 Vector 2 值。 |
| [**Vector 3**](Vector-3-Node.md) | [**Vector 4**](Vector-4-Node.md) |
| ![Image](images/Vector3NodeThumb.png) | ![Image](images/Vector4NodeThumb.png) |
| 在着色器中定义一个 Vector 3 值。 | 在着色器中定义一个 Vector 4 值。 |


几何 Geometry
--

| [Bitangent Vector](Bitangent-Vector-Node.md) | [Normal Vector](Normal-Vector-Node.md) |
| --- | --- |
| ![Image](images/BitangentVectorNodeThumb.png) | ![](images/NormalVectorNodeThumb.png) |
| 允许访问网格顶点或片元的副切线矢量。 | 允许访问网格顶点或片元的法线矢量。 |
| [**Position**](Position-Node.md) | [**Screen Position**](Screen-Position-Node.md) |
| ![Image](images/PositionNodeThumb.png) | ![Image](images/ScreenPositionNodeThumb.png) |
| 允许访问网格顶点或片元的位置。 | 允许访问网格顶点或片元的屏幕位置。 |
| [**Tangent Vector**](Tangent-Vector-Node.md) | [**UV**](UV-Node.md) |
| ![Image](images/TangentVectorNodeThumb.png) | ![Image](images/UVNodeThumb.png) |
| 允许访问网格顶点或片元的切线矢量。 | 允许访问网格顶点或片元的 UV 坐标。 |
| [**Vertex Color**](Vertex-Color-Node.md) | [**View Direction**](View-Direction-Node.md) |
| ![Image](images/VertexColorNodeThumb.png) | ![Image](images/ViewDirectionNodeThumb.png) |
| 允许访问网格顶点或片元的顶点颜色值。 | 允许访问网格顶点或片元的视图方向矢量。 |
| [**Vertex ID**](Vertex-ID-Node.md) | [**Billboard**](Billboard-Node.md) |
| ![Image](images/VertexIDNodeThumb.png) | ![](./Images/BillboardNodeThumb.png) |
| 允许访问网格顶点或片段的 Vertex ID 值。 | 对齐物体坐标轴到相机，实现面向相机的效果。 |


渐变 Gradient
--

| [Blackbody](Blackbody-Node.md) | [Gradient](Gradient-Node.md) |
| --- | --- |
| ![Image](images/BlackbodyNodeThumb.png) | ![Image](images/GradientNodeThumb.png) |
| 从温度输入（以开尔文为单位）对基于辐射的渐变进行采样。 | 在着色器中定义一个常量渐变。 |
| [Sample Gradient](Sample-Gradient-Node.md) |  |
| ![](images/SampleGradientNodeThumb.png) |  |
| 根据给定的 Time 输入对渐变进行采样。 |  |

光照 Lighting
---
|[Additional Lights Loop](Additional-Lights-Loop-Node.md) | [Baked GI](Baked-GI-Node.md) |
|--| -- |
| ![](images/AdditionalLightsLoopNodeThumb.png)| ![](./Images/BakedGINodeThumb.png) |
| 进行自定义的额外光源计算。 | 允许访问顶点或片元位置的 Baked GI 值。 |
|[Ambient](Ambient-Node.md) | [Ambient Occlusion](Ambient-Occlusion-Node.md) |
| ![](./Images/AmbientNodeThumb.png) | ![](./images/AmbientOcclusionNodeThumb.png) |
| 允许访问场景的环境颜色值。| 获取环境光遮蔽信息。|
| [Custom Diffuse Term](Custom-Diffuse-Term-Node.md) | [Custom Specular Term](Custom-Specular-Term-Node.md) |
| ![](./images/CustomDiffuseTermNodeThumb.png) | ![](./images/CustomSpecularTermNodeThumb.png) |
| 自定义计算漫反射项。| 自定义计算镜面反射项。|
| [CustomLighting](CustomLighting-Node.md) | [Main Light Color](Main-Light-Color-Node.md) |
| ![](./Images/SampleGraphLitNodeThumb.png) | ![](./Images/MainLightColorNodeThumb.png) |
| 复合节点，提供简单的 PBR 光照计算，适用于基础材质渲染。| 获取主光源颜色信息。 |
| [Main Light Direction](Main-Light-Direction-Node.md) | [Main Light Realtime Shadow](Main-Light-Realtime-Shadow-Node.md) |
| ![](./Images/MainLightDirectionNodeThumb.png) | ![](./Images/MainLightRealtimeShadowNodeThumb.png) |
| 获取主光源的方向。 | 获取主光源的实时阴影信息。 |
| [Main Light Shadow](Main-Light-Shadow-Node.md) | [Reflection Probe](Reflection-Probe-Node.md) |
| ![](./Images/mainLightShadowNodeThumb.png) | ![](./Images/ReflectionNodeThumb.png) |
| 获取主光源的阴影信息。 | 允许访问对象最近的反射探针。 |
| [ShadowMask](ShadowMask-Node.md) |  |
| ![](./Images/ShadowMaskNodeThumb.png) |  |
| 获取烘焙后的ShadowMask信息。 |  |




矩阵 Matrix
--

| [Matrix 2x2](Matrix-2x2-Node.md) | [Matrix 3x3](Matrix-3x3-Node.md) |
| --- | --- |
| ![Image](images/Matrix2x2NodeThumb.png) | ![](images/Matrix3x3NodeThumb.png) |
| 在着色器中定义一个常量矩阵 2x2 值。 | 在着色器中定义一个常量矩阵 3x3 值。 |
| [**Matrix 4x4**](Matrix-4x4-Node.md) | [**Transformation Matrix**](Transformation-Matrix-Node.md) |
| ![Image](images/Matrix4x4NodeThumb.png) | ![Image](images/TransformationMatrixNodeThumb.png) |
| 在着色器中定义一个常量矩阵 4x4 值。 | 在着色器中定义一个默认变换矩阵的常量矩阵 4x4 值。 |


Mesh Deformation
----------------

| [Compute Deformation Node](Compute-Deformation-Node.md) | [Linear Blend Skinning Node](Linear-Blend-Skinning-Node.md) |
| --- | --- |
| ![Image](images/ComputeDeformationNodeThumb.png) | ![Image](images/LinearBlendSkinningNodeThumb.png) |
| 将计算变形的顶点数据传递给顶点着色器。仅适用于 [Entities Graphics package](https://docs.unity3d.com/Packages/com.unity.entities.graphics@latest/)。 | 应用线性混合顶点蒙皮。仅适用于 [Entities Graphics package](https://docs.unity3d.com/Packages/com.unity.entities.graphics@latest/)。 |


PBR
---

| [**Dielectric Specular**](Dielectric-Specular-Node.md) | [**Metal Reflectance**](Metal-Reflectance-Node.md) |
| --- | --- |
| ![Image](images/DielectricSpecularNodeThumb.png) | ![](images/MetalReflectanceNodeThumb.png) |
| 返回物理材质的介电镜面反射 (Dielectric Specular) F0 值。 | 返回物理材质的金属反射 (Metal Reflectance) 值。 |


场景 Scene
--

| [Camera](Camera-Node.md) | [Depth Fade](Depth-Fade-Node.md) |
| --- | --- |
| ![](images/CameraNodeThumb.png) | ![](./Images/DepthFadeNodeThumb.png) |
| 允许访问当前摄像机的各种参数。 | 根据深度信息生成渐变效果。 |
| [**Eye Index**](Eye-Index-Node.md) | [**Fog**](Fog-Node.md) |
| ![Image](images/EyeIndexNodeThumb.png) | ![Image](images/FogNodeThumb.png) |
| 在立体渲染时允许访问 Eye Index。 | 允许访问场景的 Fog 参数。 |
| [**Object**](Object-Node.md) | [**Scene Color**](Scene-Color-Node.md) |
| ![Image](images/ObjectNodeThumb.png) | ![Image](images/SceneColorNodeThumb.png) |
| 允许访问对象的各种参数。 | 允许访问当前摄像机的颜色缓冲区。 |
| [**Scene Depth**](Scene-Depth-Node.md) | [**Screen**](Screen-Node.md) |
| ![Image](images/SceneDepthNodeThumb.png)  | ![Image](images/ScreenNodeThumb.png) |
| 允许访问当前摄像机的深度缓冲区。 | 允许访问屏幕的参数。 |


纹理 Texture
--

| [**Cubemap Asset**](Cubemap-Asset-Node.md) | [**Sample Cubemap**](Sample-Cubemap-Node.md) |
| --- | --- |
| ![Image](images/CubemapAssetNodeThumb.png)| ![](images/SampleCubemapNodeThumb.png) |
| 定义一个要在着色器中使用的常量立方体贴图资源。 | 对立方体贴图进行采样并返回 Vector 4 颜色值以在着色器中使用。 |
| [**Sample Reflected Cubemap Node**](Sample-Reflected-Cubemap-Node.md) | [**Sample Texture 2D**](Sample-Texture-2D-Node.md) |
| ![Image](images/SampleReflectedCubemapThumb.png) | ![Image](images/SampleTexture2DNodeThumb.png) |
| 使用反射矢量对立方体贴图进行采样，并返回一个 Vector 4 颜色值以在着色器中使用。 | 对 2D 纹理进行采样，并返回一个颜色值以在着色器中使用。 |
| [**Sample Texture 2D Array**](Sample-Texture-2D-Array-Node.md) | [**Sample Texture 2D LOD**](Sample-Texture-2D-LOD-Node.md) |
| ![Image](images/SampleTexture2DArrayNodeThumb.png) | ![Image](images/SampleTexture2DLODNodeThumb.png) |
| 在 Index 位置对 2D 纹理数组进行采样，并返回一个颜色值以在着色器中使用。 | 以指定的 LOD 对 2D 纹理进行采样，并返回一个颜色值以在着色器中使用。 |
| [**Sample Texture 3D**](Sample-Texture-3D-Node.md) | [**Sample Virtual Texture**](Sample-Virtual-Texture-Node.md) |
| ![Image](images/SampleTexture3DNodeThumb.png) | ![image](images/SampleVirtualTextureNodeThumb.png) |
| 对 3D 纹理进行采样，并返回一个颜色值以在着色器中使用。 | 对虚拟纹理进行采样，并返回颜色值以在着色器中使用。 |
| [**Sampler State**](Sampler-State-Node.md) | [**Texel Size**](Texel-Size-Node.md) |
| ![Image](images/SamplerStateNodeThumb.png) | ![Image](images/TexelSizeNodeThumb.png) |
| 定义一个采样纹理的采样器状态。 | 返回 2D 纹理输入的纹素大小的宽度 (Width) 和高度 (Height)。 |
| [**Texture 2D Array Asset**](Texture-2D-Array-Asset-Node.md) | [**Texture 2D Asset**](Texture-2D-Asset-Node.md) |
| ![Image](images/Texture2DArrayAssetNodeThumb.png) | ![Image](images/Texture2DAssetNodeThumb.png) |
| 定义一个要在着色器中使用的常量 2D 纹理数组资源。 | 定义一个要在着色器中使用的常量 2D 纹理资源。 |
| [**Texture 3D Asset**](Texture-3D-Asset-Node.md) |  |
| ![Image](images/Texture3DAssetNodeThumb.png) |  |
| 定义一个要在着色器中使用的常量 3D 纹理资源。 |  |
