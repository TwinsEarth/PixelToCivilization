Split Texture Transform 节点
============================

[](#description)描述
---------------------------

该节点允许单独输出 **Texture 2D** 资源的平铺、偏移和纹理数据。这使得您可以在特定上下文中以不同的方式展示资源，例如在镜像中扭曲它，并将其放入 **UV** 中而无需修改原始资源。

该节点输出的纹理平铺设置为 (0,0)，缩放设置为 (1,1)。这会激活着色器属性 [NoScaleOffset](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-Properties.html)，您可以通过材质检查器修改**平铺偏移**（Tiling Offset）值。

在此上下文中，平铺的另一个术语是缩放。这两个术语都指纹理瓦片的大小。

### [](#ports)端口

| **名称** | **方向** | **类型** | **描述** |
| --- | --- | --- | --- |
| In | 输入 | Texture2D | Texture 2D 节点输入。 |
| Tiling | 输出 | Vector 2 | 每通道应用的平铺量，通过 Material Inspector 设置。 |
| Offset | 输出 | Vector 2 | 每通道应用的偏移量，通过 Material Inspector 设置。 |
| Texture Only | 输出 | Vector 2 | 无平铺和偏移数据的 Texture2D 输入。 |