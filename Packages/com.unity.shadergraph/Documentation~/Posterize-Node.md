
Posterize 节点
============

描述
--

> 图像的分色需要将连续的色调渐变转换为几个较少色调的区域（从一个色调到另一个色调突然变化）。

*<https://en.wikipedia.org/wiki/Posterization>*


此节点返回将输入 **In** 的值分色 posterized（也称为量化 quantized）为输入 **Steps** 指定的值数量的结果。


端口
--




| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| In | 输入 | 动态矢量 | 输入值 |
| Steps | 输入 | 动态矢量 | 输入值 |
| Out | 输出 | 动态矢量 | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_Posterize_float4(float4 In, float4 Steps, out float4 Out)
{
    Out = floor(In / (1 / Steps)) * (1 / Steps);
}

```

