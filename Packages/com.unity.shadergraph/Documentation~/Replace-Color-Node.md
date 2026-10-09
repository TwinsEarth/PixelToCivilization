
Replace Color 节点
================


描述
--


将输入 **In** 中等于输入 **From** 的值替换为输入 **To** 的值。输入 **Range** 可用于在输入 **From** 周围定义更宽范围的值以便进行替换。输入 **Fuzziness** 可用于软化选择范围周围的边缘，类似于抗锯齿效果。


端口
--




| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| In | 输入 | Vector 3 | 无 | 输入值 |
| From | 输入 | Vector 3 | Color | 要替换的颜色 |
| To | 输入 | Vector 3 | Color | 要替换为的颜色 |
| Range | 输入 | Float | 无 | 根据输入 **From** 替换此范围内的颜色 |
| Fuzziness | 输入 | Float | 无 | 软化选择范围周围的边缘 |
| Out | 输出 | Vector 3 | 无 | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_ReplaceColor_float(float3 In, float3 From, float3 To, float Range, float Fuzziness, out float3 Out)
{
    float Distance = distance(From, In);
    Out = lerp(To, In, saturate((Distance - Range) / max(Fuzziness, e-f)));
}

```

