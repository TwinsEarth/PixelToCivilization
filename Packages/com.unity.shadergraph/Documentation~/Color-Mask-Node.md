
Color Mask 节点
=============


描述
--

根据输入 **In** 中的等于输入 **Mask Color** 的值创建遮罩。输入 **Range** 可用于在输入 **Mask Color** 周围定义更宽范围的值以便创建遮罩。此范围内的颜色将返回 1，否则节点将返回 0。输入 **Fuzziness** 可用于软化选择范围周围的边缘，类似于抗锯齿效果。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| In | 输入 | Vector 3 | 无 | 输入值。 |
| Mask Color | 输入 | Vector 3 | Color | 用于遮罩的颜色。 |
| Range | 输入 | Float | 无 | 根据输入 **Mask Color** 选择此范围内的颜色。 |
| Fuzziness | 输入 | Float | 无 | 选择范围周围的羽毛边缘。值越高，选择范围遮罩越柔和。 |
| Out | 输出 | Float | 无 | 输出遮罩值。 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_ColorMask_float(float3 In, float3 MaskColor, float Range, float Fuzziness, out float4 Out)
{
    float Distance = distance(MaskColor, In);
    Out = saturate(1 - (Distance - Range) / max(Fuzziness, 1e-5));
}

```

