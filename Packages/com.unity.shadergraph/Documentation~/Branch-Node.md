Branch 节点
=========


描述
--


为着色器提供动态分支。如果输入 **Predicate** 为 true，则返回输出将等于输入 **True**，否则它将等于输入 **False**。Branch 节点会根据着色器阶段对每个顶点或每个像素的 **Predicate** 进行评估。着色器会对分支的两边进行评估，未使用的分支会被丢弃。


端口
--




| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Predicate | 输入 | 布尔值 (Boolean) | 无 | 确定要返回的输入 |
| True | 输入 | 动态矢量 | 无 | 当 **Predicate** 为 true 时返回 |
| False | 输入 | 动态矢量 | 无 | 当 **Predicate** 为 false 时返回 |
| Out | 输出 | 布尔值 (Boolean) | 无 | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ?True : False;
}

```
