
Clamp 节点
========


描述
--


返回输入 **In** 在最小值和最大值（分别由输入 **Min** 和 **Max** 定义）之间钳制的结果。


端口
--




| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| In | 输入 | 动态矢量 | 未经钳制的输入值 |
| Min | 输入 | 动态矢量 | 最小值 |
| Max | 输入 | 动态矢量 | 最大值 |
| Out | 输出 | 动态矢量 | 经过钳制的输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_Clamp_float4(float4 In, float4 Min, float4 Max, out float4 Out)
{
    Out = clamp(In, Min, Max);
}

```

