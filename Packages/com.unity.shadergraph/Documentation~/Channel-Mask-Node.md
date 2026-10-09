
Channel Mask 节点
===============

描述
--

在下拉选单 **Channels** 中选择的通道上屏蔽输入 **In** 的值。输出一个与输入 Vector 长度相同但所选通道设置为 0 的 Vector。下拉选单 **Channels** 中可用的通道将表示输入 **In** 中存在的通道数量。


端口
--




| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| In | 输入 | 动态矢量 | 无 | 输入值 |
| Out | 输出 | 动态矢量 | 无 | 输出值 |


控件
--




| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Channels | Mask 下拉选单 | Dynamic | 选择要屏蔽的任意数量的通道 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_ChannelMask_RedGreen_float4(float4 In, out float4 Out)
{
    Out = float4(0, 0, In.b, In.a);
}

```

