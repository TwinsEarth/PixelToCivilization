
Contrast 节点
===========


描述
--


根据输入 **Contrast** 的大小调整输入 **In** 的对比度。**Contrast** 值为 1 将原封不动返回输入值。**Contrast** 值为 0 将返回输入值的中点。


端口
--




| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| In | 输入 | Vector 3 | 无 | 输入值 |
| Contrast | 输入 | Float | 无 | 对比度值 |
| Out | 输出 | Vector 3 | 无 | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out = (In - midpoint) * Contrast + midpoint;
}

```

