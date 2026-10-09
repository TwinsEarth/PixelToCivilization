All 节点
======


描述
--


如果输入 **In** 的所有分量都不为零，则返回 true。这对于[分支](Branch-Node.md)（Branching）很有用。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| In | 输入 | 动态矢量 | 无 | 输入值 |
| Out | 输出 | 布尔值 (Boolean) | 无 | 输出值 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
void Unity_All_float4(float4 In, out float Out)
{
    Out = all(In);
}

```
