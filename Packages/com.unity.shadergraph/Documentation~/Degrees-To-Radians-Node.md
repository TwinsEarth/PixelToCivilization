
Degrees To Radians 节点
=====================


描述
--


返回输入 **In** 从度转换为弧度的值。


1 度等于约 0\.0174533 弧度，360 度的完整旋转等于 2 Pi 弧度。


端口
--




| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| In | 输入 | 动态矢量 | 输入值 |
| Out | 输出 | 动态矢量 | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_DegreesToRadians_float4(float4 In, out float4 Out)
{
    Out = radians(In);
}

```

