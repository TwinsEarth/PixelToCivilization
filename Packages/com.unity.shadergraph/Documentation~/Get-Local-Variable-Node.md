# Get Local Variable 节点

## 描述

**Get Local Variable** 节点通过在 Node Settings 面板选择局部变量 Variable 属性来访问 [Local Variable Register](./Local-Variable-Register-Node.md) 节点创建的局部变量。

使用请参考[局部变量 Local Variable](./LocalVariable.md)。

## 端口

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Out | 输出 | 动态矢量 | 无 | 输出对应局部变量的值。 |


## 生成的代码示例

以下示例代码表示此节点的一种可能结果。

```
float GetLocalVariableOutputValue = variableName;
```