
Boolean 节点
==========


描述
--

在 [Shader Graph](index.md) 中定义一个常量**布尔值**，尽管位于着色器内部，但它被视为一个常量**浮点**值，即 0 或 1，类似于 Shaderlab 的 [Toggle](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/MaterialPropertyDrawer.html) 属性。可通过[节点](Node.md)的上下文菜单转换为**布尔值**类型的[属性](Property-Types.md)。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Out | 输出 | 布尔值 (Boolean) | 无 | 输出值 |


控件
--

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
|  | 开关 |  | 定义输出值。 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。



```
float _Boolean = 1;

```

