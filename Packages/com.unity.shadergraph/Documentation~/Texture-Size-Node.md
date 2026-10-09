Texture Size 节点
=================

Texture Size 节点接收一个 Texture 2D 输入，并返回纹理的宽度和高度（以 texel 为单位），还返回每个 texel 的宽度和高度。该节点使用内置变量 `{texturename}_TexelSize` 来访问给定 2D 纹理输入的特殊属性。

“texel” 是“texture element”或“texture pixel”的缩写，表示纹理中的单个像素。例如，如果纹理分辨率为 512x512 texel，则在 UV 空间中纹理会被采样在范围 [0-1] 内，因此每个 texel 的尺寸为 UV 坐标中的 1/512 x 1/512。

如果在包含自定义功能节点或子图的图形中使用该节点时遇到纹理采样错误，可通过将 Shader Graph 更新至 10.3 版或更高版本来解决。

> [!NOTE]
> 不要使用默认输入来引用您的 **Texture 2D**，因为这样会影响图形的性能。将 [Texture 2D Asset 节点](Texture-2D-Asset-Node.md) 连接到 Texture Size 节点的纹理输入端口，并重新使用此定义进行采样。

[](#create-node-menu-category)创建节点菜单类别
-------------------------------------------------------

纹理尺寸节点位于创建节点菜单（Create Node menu）的 **Input -> Texture** 类别中。

[](#compatibility)兼容性
-------------------------------

纹理尺寸节点兼容所有渲染管线。

[](#ports)端口
---------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Texture | 输入 | 纹理 | 无 | 要测量的 2D 纹理资产。 |
| Width | 输出 | Float | 无 | 2D 纹理资产的宽度，以 texel 为单位。 |
| Height | 输出 | Float | 无 | 2D 纹理资产的高度，以 texel 为单位。 |
| Texel Width | 输出 | Float | 无 | 2D 纹理资产在 UV 坐标中的 texel 宽度。 |
| Texel Height | 输出 | Float | 无 | 2D 纹理资产在 UV 坐标中的 texel 高度。 |

[](#generated-code-example)生成代码示例
-------------------------------------------------

以下示例代码展示了该节点可能的实现方式之一：

```
float _TexelSize_Width = Texture_TexelSize.z;
float _TexelSize_Height = Texture_TexelSize.w;
```