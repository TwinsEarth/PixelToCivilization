
Radians To Degrees 节点
=====================


描述
--


返回输入 **In** 从弧度转换为度的值。1 弧度等于约 57\.2958 度，2 Pi 弧度的完整旋转等于 360 度。


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
void Unity_RadiansToDegrees_float4(float4 In, out float4 Out)
{
    Out = degrees(In);
}

```

