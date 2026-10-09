
Reflection 节点
=============


描述
--

返回使用输入 **In** 和表面法线 **Normal** 的反射矢量。


端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| In | 输入 | 动态矢量 | 入射矢量值 |
| Normal | 输入 | 动态矢量 | 法线矢量值 |
| Out | 输出 | 动态矢量 | 输出值 |


生成的代码示例
-------

以下示例代码表示此节点的一种可能结果。



```
void Unity_Reflection_float4(float4 In, float4 Normal, out float4 Out)
{
    Out = reflect(In, Normal);
}

```

