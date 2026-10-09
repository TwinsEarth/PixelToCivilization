
Normal Reconstruct Z 节点
=======================


描述
--


使用输入 **In** 中的给定 **X** 和 **Y** 值为生成的法线贴图导出正确的 Z 值。


端口
--




| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| In | 输入 | Vector 2 | 法线 X 和 Y 值 |
| Out | 输出 | Vector 3 | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_NormalReconstructZ_float(float2 In, out float3 Out)
{
    float reconstructZ = sqrt(1.0 - saturate(dot(In.xy, In.xy)));
    float3 normalVector = float3(In.x, In.y, reconstructZ);
    Out = normalize(normalVector);
}

```

