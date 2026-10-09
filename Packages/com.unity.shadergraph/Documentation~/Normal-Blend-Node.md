
Normal Blend 节点
===============


描述
--

将输入 **A** 和 **B** 定义的两个法线贴图混合到一起，并对结果进行标准化以创建有效的法线贴图。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| A | 输入 | Vector 3 | 无 | 第一个输入值 |
| B | 输入 | Vector 3 | 无 | 第二个输入值 |
| Out | 输出 | Vector 3 | 无 | 输出值 |


控件
--




| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Mode | 下拉选单 | Default、Reoriented | 选择用于混合的方法。 |


生成的代码示例
-------


以下示例代码表示此节点在每个**模式**下的一种可能结果。


**Default**



```
void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = normalize(float3(A.rg + B.rg, A.b * B.b));
}

```
**Reoriented**



```
void Unity_NormalBlend_Reoriented_float(float3 A, float3 B, out float3 Out)
{
    float3 t = A.xyz + float3(0.0, 0.0, 1.0);
    float3 u = B.xyz * float3(-1.0, -1.0, 1.0);
    Out = (t / t.z) * dot(t, u) - u;
}

```

