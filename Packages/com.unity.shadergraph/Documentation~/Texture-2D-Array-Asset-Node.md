
Texture 2D Array Asset 节点
=========================


描述
--


定义要在着色器中使用的常量 **2D 纹理数组资源**。要对 **2D 纹理数组资源**采样，必须将其与 [Sample Texture 2D Array 节点](Sample-Texture-2D-Array-Node.md)结合使用。使用单个 **Texture 2D Array Asset 节点**时，可使用不同的参数对 **2D 纹理数组**进行两次采样，无需对 **2D 纹理数组**本身进行两次定义。


端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| Out | 输出 | 2D 纹理数组 | 输出值 |


控件
--

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
|  | 对象字段（2D 纹理数组） |  | 定义项目中的 2D 纹理数组资源。 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
TEXTURE2D_ARRAY(_Texture2DArrayAsset); 
SAMPLER(sampler_Texture2DArrayAsset);

```

