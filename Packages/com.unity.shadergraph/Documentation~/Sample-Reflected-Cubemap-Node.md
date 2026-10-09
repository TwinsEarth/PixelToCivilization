
Sample Reflected Cubemap 节点
===========================


描述
--


使用反射矢量对立方体贴图进行采样，并返回一个Vector 4 颜色值以在着色器中使用。需要 View Direction (**View Dir**) 和 **Normal** 输入才能采样立方体贴图。可以使用 **LOD** 输入在不同的细节级别进行采样，从而获得模糊效果。还可以使用 **Sampler** 输入来定义一个自定义采样器状态。


如果在包含自定义函数节点或子图形的图形中使用此节点时遇到纹理采样错误，可以通过升级到 10\.3 或更高版本来解决这些问题。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Cube | 输入 | 立方体贴图 | 无 | 要采样的立方体贴图 |
| View Dir | 输入 | Vector 3 | 视图方向（object space） | 网格的视图方向 |
| Normal | 输入 | Vector 3 | 法线（object space） | 网格的法线矢量 |
| Sampler | 输入 | 采样器状态 | 默认采样器状态 | 立方体贴图的采样器 |
| LOD | 输入 | Float | 无 | 采样的细节级别 |
| Out | 输出 | Vector 4 | 无 | 输出值 |


生成的代码示例
-------

以下示例代码表示此节点的一种可能结果。

```
float4 _SampleCubemap_Out = SAMPLE_TEXTURECUBE_LOD(Cubemap, Sampler, reflect(-ViewDir, Normal), LOD);

```

