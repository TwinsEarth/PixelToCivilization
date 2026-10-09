
Normal Strength 节点
==================


描述
--


根据输入 **Strength** 的大小调整输入 **In** 定义的法线贴图的强度。**Strength** 值为 1 将原封不动返回输入值。**Strength** 值为 0 将返回空白法线贴图。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| In | 输入 | Vector 3 | 无 | 输入值 |
| Strength | 输入 | Float | 无 | 强度值 |
| Out | 输出 | Vector 3 | 无 | 输出值 |


生成的代码示例
-------

以下示例代码表示此节点的一种可能结果。



```
void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = {precision}3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

```

