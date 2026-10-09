Sample Texture 2D LOD 节点
========================


描述
--

对 **2D 纹理**进行采样并返回 **Vector 4** 颜色值以在着色器中使用。可使用 **UV** 输入覆盖 **UV** 坐标，并使用 **Sampler** 输入自定义**采样器状态**（Sampler State）。使用 **LOD** 输入调整样本的细节级别。


要使用 **Sample Texture 2D LOD 节点**对法线贴图进行采样，请将 **Type** 下拉选单参数设置为 **Normal**。


此[节点](Node.md)对于在顶点[着色器阶段](Shader-Stage.md)中对**纹理**进行采样非常有用，因为该[着色器阶段](Shader-Stage.md)中的 [Sample Texture 2D 节点](Sample-Texture-2D-Node.md)不可用。


在不支持此操作的平台上，将返回不透明黑色。

> [!NOTE]
> 如果在包含自定义函数节点或子图形的图形中使用此节点时遇到纹理采样错误，可以通过升级到 10\.3 或更高版本来解决这些问题。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Texture | 输入 | 2D 纹理 | 无 | 要采样的 2D 纹理 |
| UV | 输入 | Vector 2 | UV | UV 坐标 |
| Sampler | 输入 | 采样器状态 | 默认采样器状态 | 纹理采样器 |
| LOD | 输入 | Float | 无 | 要采样的细节级别 |
| RGBA | 输出 | Vector 4 | 无 | RGBA 输出值 |
| R | 输出 | Float | 无 | RGBA 输出的红色 (x) 分量 |
| G | 输出 | Float | 无 | RGBA 输出的绿色 (y) 分量 |
| B | 输出 | Float | 无 | RGBA 输出的蓝色 (z) 分量 |
| A | 输出 | Float | 无 | RGBA 输出的 Alpha (w) 分量 |


控件
--

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Type | 下拉选单 | Default、Normal | 选择纹理类型 |


生成的代码示例
-------


以下示例代码表示此节点在每个 **Type** 模式下的一种可能结果。


**Default**



```
float4 _SampleTexture2DLOD_RGBA = SAMPLE_TEXTURE2D_LOD(Texture, Sampler, UV, LOD);
float _SampleTexture2DLOD_R = _SampleTexture2DLOD_RGBA.r;
float _SampleTexture2DLOD_G = _SampleTexture2DLOD_RGBA.g;
float _SampleTexture2DLOD_B = _SampleTexture2DLOD_RGBA.b;
float _SampleTexture2DLOD_A = _SampleTexture2DLOD_RGBA.a;

```
**Normal**



```
float4 _SampleTexture2DLOD_RGBA = SAMPLE_TEXTURE2D_LOD(Texture, Sampler, UV, LOD);
_SampleTexture2DLOD_RGBA.rgb = UnpackNormalRGorAG(_SampleTexture2DLOD_RGBA);
float _SampleTexture2DLOD_R = _SampleTexture2DLOD_RGBA.r;
float _SampleTexture2DLOD_G = _SampleTexture2DLOD_RGBA.g;
float _SampleTexture2DLOD_B = _SampleTexture2DLOD_RGBA.b;
float _SampleTexture2DLOD_A = _SampleTexture2DLOD_RGBA.a;

```

