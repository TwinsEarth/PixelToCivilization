# Local Variable Register 节点

## 描述

**Local Variable Register** 节点会在 Shader Graph 中创建一个**局部变量**，其名称由 Node Settings 面板中的 Name 属性指定，并将输入接口中的值赋值给该变量；然后通过 [Get Local Variable](./Get-Local-Variable-Node.md) 节点访问对应的局部变量。

需要注意的是，如果 Name 属性为空，则默认设置为该节点的 guid；只支持输入 Vector1，Vector2，Vector3，Vector4 数据类型的值，局部变量的值类型会自动随之变化。

使用请参考[局部变量 Local Variable](./LocalVariable.md)。

## 端口

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| In | 输入 | 动态矢量 | 无 | 赋值给新创建的局部变量的值。 |

## 生成的代码示例

以下示例代码表示此节点的一种可能结果。

```
float variableName = InputValue;
```