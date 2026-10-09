
Triangle Wave 节点
==============

描述
--

从输入 **In** 的值返回三角波（Triangle Wave）。

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
void Unity_TriangleWave_float4(float4 In, out float4 Out)
{
    Out = 2.0 * abs( 2 * (In - floor(0.5 + In)) ) - 1.0;
}

```

