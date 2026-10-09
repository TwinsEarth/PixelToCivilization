
Sampler State 节点
================


描述
--


定义采样纹理的**采样器状态**。应当与采样[节点](Node.md)（如 [Sample Texture 2D 节点](Sample-Texture-2D-Node.md)）结合使用。可以使用下拉选单参数 **Filter** 设置过滤器模式，并使用下拉选单参数 **Wrap** 设置包裹模式。


使用单个 **Sample State 节点**时，可使用不同的采样器参数对 **2D 纹理**进行两次采样，无需对 **2D 纹理**本身进行两次定义。


有些过滤和包裹模式仅在特定平台上可用。


端口
--




| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Out | 输出 | 采样器状态 | 无 | 输出值 |


控件
--




| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Filter | 下拉选单 | Linear、Point、Trilinear | 定义采样的过滤模式。 |
| Wrap | 下拉选单 | Repeat、Clamp、Mirror、MirrorOnce | 定义采样的包裹模式。 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
SamplerState _SamplerState_Out = _SamplerState_Linear_Repeat_sampler;

```

