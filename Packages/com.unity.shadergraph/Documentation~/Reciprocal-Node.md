

Reciprocal 节点
=============


描述
--


返回 1 除以输入 **In** 的结果。通过将 **Method** 设置为 **Fast** 可以在 Shader Model 5 上快速模拟此计算。


端口
--




| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| In | 输入 | 动态矢量 | 输入值 |
| Out | 输出 | 动态矢量 | 输出值 |


控件
--




| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Method | 下拉选单 | Default、Fast | 选择使用的方法 |


生成的代码示例
-------


以下示例代码表示此节点在每个 **Method** 模式下的一种可能结果。


**Default**



```
void Unity_Reciprocal_float4(float4 In, out float4 Out)
{
    Out = 1.0/In;
}

```
**Fast**（需要 Shader Model 5）



```
void Unity_Reciprocal_Fast_float4(float4 In, out float4 Out)
{
    Out = rcp(In);
}

```

