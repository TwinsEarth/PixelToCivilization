
Exponential 节点
==============


描述
--


返回输入 **In** 的幂值。可以使用节点上的 **Base** 下拉选单在 base\-e 和 base\-2 之间切换底数。


* **Base E**：返回输入 **In** 以 e 为底数的幂值
* **Base 2**：返回输入 **In** 以 2 为底数的幂值


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
| Base | 下拉选单 | BaseE、Base2 | 选择底数 |


生成的代码示例
-------


以下示例代码表示此节点在每个 **Base** 模式下的一种可能结果。


**Base E**



```
void Unity_Exponential_float4(float4 In, out float4 Out)
{
    Out = exp(In);
}

```
**Base 2**



```
void Unity_Exponential2_float4(float4 In, out float4 Out)
{
    Out = exp2(In);
}

```

