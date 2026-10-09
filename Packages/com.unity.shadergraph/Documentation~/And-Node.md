
And 节点
======


描述
--


如果输入 **A** 和 **B** 均为 true，则返回 true。这对于[分支](Branch-Node.md)（Branching）很有用。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| A | 输入 | 布尔值 (Boolean) | 无 | 第一个输入值 |
| B | 输入 | 布尔值 (Boolean) | 无 | 第二个输入值 |
| Out | 输出 | 布尔值 (Boolean) | 无 | 输出值 |


生成的代码示例
-------

```
void Unity_And(float A, float B, out float Out)
{
    Out = A && B;
}

```
