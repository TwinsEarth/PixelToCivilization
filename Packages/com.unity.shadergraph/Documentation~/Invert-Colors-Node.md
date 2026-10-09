
Invert Colors 节点
================


描述
--


基于每个通道反转输入 **In** 的颜色。此[节点](Node.md)假设所有输入值均在 0 \- 1 范围内。


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
| Red | 开关 | True、False | 如果为 true，则反转红色通道 |
| Green | 开关 | True、False | 如果为 true，则反转绿色通道。如果输入 Vector 维度小于 2，则禁用 |
| Blue | 开关 | True、False | 如果为 true，则反转蓝色通道。如果输入 Vector 维度小于 3，则禁用 |
| Alpha | 开关 | True、False | 如果为 true，则反转 Alpha 通道。如果输入 Vector 维度小于 4，则禁用 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
float2 _InvertColors_InvertColors = float4(Red, Green, Blue, Alpha);

void Unity_InvertColors_float4(float4 In, float4 InvertColors, out float4 Out)
{
    Out = abs(InvertColors - In);
}

```

