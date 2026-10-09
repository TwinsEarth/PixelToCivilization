
Square Wave 节点
==============


描述
--

从输入 **In** 的值返回方波。


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
void Unity_SquareWave_float4(float4 In, out float4 Out)
{
    Out = 1.0 - 2.0 * round(frac(In));
}

```

