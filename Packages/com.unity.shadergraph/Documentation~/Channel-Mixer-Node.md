
Channel Mixer 节点
================


描述
--


控制输入 **In** 的每个通道对输出 **Out** 的每个通道的贡献量。节点上的滑动条参数用于控制每个输入通道的贡献。切换按钮参数用于控制当前正在编辑哪个输出通道。用于编辑每个输入通道的贡献的滑动条控件介于 \-2 和 2 之间。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| In | 输入 | Vector 3 | 无 | 输入值 |
| Out | 输出 | Vector 3 | 无 | 输出值 |


控件
--

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
|  | 切换按钮阵列 | R、G、B | 选择要编辑的输出通道。 |
| R | 滑动条 |  | 控制输入红色通道对所选输出通道的贡献。 |
| G | 滑动条 |  | 控制输入绿色通道对所选输出通道的贡献。 |
| B | 滑动条 |  | 控制输入蓝色通道对所选输出通道的贡献。 |


着色器函数
-----


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
_ChannelMixer_Red = float3 (OutRedInRed, OutRedInGreen, OutRedInBlue);
_ChannelMixer_Green = float3 (OutGreenInRed, OutGreenInGreen, OutGreenInBlue);
_ChannelMixer_Blue = float3 (OutBlueInRed, OutBlueInGreen, OutBlueInBlue);

void Unity_ChannelMixer_float(float3 In, float3 _ChannelMixer_Red, float3 _ChannelMixer_Green, float3 _ChannelMixer_Blue, out float3 Out)
{
    Out = float3(dot(In, _ChannelMixer_Red), dot(In, _ChannelMixer_Green), dot(In, _ChannelMixer_Blue));
}

```

