
Flip 节点
=======


描述
--

翻转[节点](Node.md)参数选择的输入 **In** 的各个通道。正值变为负值，反之亦然。


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
| Red | 开关 | True、False | 如果为 true，则将翻转红色通道。 |
| Green | 开关 | True、False | 如果为 true，则将翻转绿色通道。如果 **In** 为 Float，则禁用。 |
| Blue | 开关 | True、False | 如果为 true，则将翻转蓝色通道。如果 **In** 为 Vector 2 或更小，则禁用。 |
| Alpha | 开关 | True、False | 如果为 true，则将翻转 Alpha 通道。如果 **In** 为 Vector 3 或更小，则禁用。 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
float2 _Flip_Flip = float4(Red, Green, Blue, Alpha);

void Unity_Flip_float4(float4 In, float4 Flip, out float4 Out)
{
    Out = (Flip * -2 + 1) * In;
}

```

