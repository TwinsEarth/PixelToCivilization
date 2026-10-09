
Texel Size 节点
=============


描述
--


返回 **2D 纹理**输入的纹素大小的**宽度 (Width)** 和**高度 (Height)**。使用内置的变量 `{texturename}_TexelSize` 可以访问 **2D 纹理**的特殊属性。


如果在包含自定义函数节点或子图形的图形中使用此节点时遇到纹理采样错误，可以通过升级到 10\.3 或更高版本来解决这些问题。


**注意：**不要使用默认输入来引用 **2D 纹理**。这会降低图形性能。应该将此节点连接到单独的 [Texture 2D Asset 节点](Texture-2D-Asset-Node)，并将此定义重复用于采样。


端口
--




| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Texture | 输入 | Texture | 无 | 纹理资源 |
| Width | 输出 | Float | 无 | 纹素宽度 |
| 高度 | 输出 | Float | 无 | 纹素高度 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
float _TexelSize_Width = Texture_TexelSize.z; 
float _TexelSize_Height = Texture_TexelSize.w;

```

