
Comparison 节点
=============


描述
--


根据下拉选单上选择的条件比较两个输入值 **A** 和 **B**。这通常用作 [Branch 节点](Branch-Node.md)的输入。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| A | 输入 | Float | 无 | 第一个输入值 |
| B | 输入 | Float | 无 | 第二个输入值 |
| Out | 输出 | 布尔值 (Boolean) | 无 | 输出值 |


控件
--

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
|  | 下拉选单 | Equal、NotEqual、Less、LessOrEqual、Greater、GreaterOrEqual | 比较的条件 |


生成的代码示例
-------


以下示例代码表示此节点在每个比较类型中的一种可能结果。


**Equal**



```
void Unity_Comparison_Equal_float(float A, float B, out float Out)
{
    Out = A == B ?1 : 0;
}

```
**NotEqual**



```
void Unity_Comparison_NotEqual_float(float A, float B, out float Out)
{
    Out = A != B ?1 : 0;
}

```
**Less**



```
void Unity_Comparison_Less_float(float A, float B, out float Out)
{
    Out = A < B ?1 : 0;
}

```
**LessOrEqual**



```
void Unity_Comparison_LessOrEqual_float(float A, float B, out float Out)
{
    Out = A <= B ?1 : 0;
}

```
**Greater**



```
void Unity_Comparison_Greater_float(float A, float B, out float Out)
{
    Out = A > B ?1 : 0;
}

```
**GreaterOrEqual**



```
void Unity_Comparison_GreaterOrEqual_float(float A, float B, out float Out)
{
    Out = A >= B ?1 : 0;
}

```
