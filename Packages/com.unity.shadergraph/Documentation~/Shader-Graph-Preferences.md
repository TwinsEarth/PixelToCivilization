
Shader Graph 偏好设置
=================


要访问 Shader Graph 项目范围的设置，请单击 **Edit -> Preferences**，然后选择 **Shader Graph**。


设置
--

| 名称 | 描述 |
| --- | --- |
| Shader Variant Limit | 输入一个值以设置着色器变体的最大数量。如果您的图形超过此最大值，团结引擎会抛出以下错误：<br>*验证：图表生成了过多的变体。请删除关键词，减少关键词变体，或在 Preferences > Shader Graph 中增加着色器变体限制。* <br>有关着色器变体的更多信息，请参见[制作多个着色器程序变体](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-MultipleProgramVariants.html)。 |
| Automatically Add or Remove Block Nodes | 打开或关闭此选项。如果打开，当更改图表设置时，所需的块节点将自动添加到主堆栈中。没有输入连接的、不兼容的块节点将从主堆栈中删除。如果关闭，则不会有块节点被添加或从主堆栈中删除。 |
| Enable Deprecated Nodes | 启用此设置以关闭有关已弃用节点和属性的警告，同时也允许您创建旧版本的节点和属性。如果未启用此设置，Shader Graph 会显示已弃用节点和属性的警告，且您创建的任何新节点和属性都会使用最新版本。 |



