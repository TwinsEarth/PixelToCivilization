
Saturation 节点
=============


描述
--


根据输入 **Saturation** 的大小调整输入 **In** 的饱和度。**Saturation** 值为 1 将原封不动返回输入值。**Saturation** 值为 0 将返回完全去饱和的输入。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| In | 输入 | Vector 3 | 无 | 输入值 |
| Saturation | 输入 | Float | 无 | 饱和度值 |
| Out | 输出 | Vector 3 | 无 | 输出值 |


生成的代码示例
-------

以下示例代码表示此节点的一种可能结果。

```
void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
{
    float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
    Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
}

```

