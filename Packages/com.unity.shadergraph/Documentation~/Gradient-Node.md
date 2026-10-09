
Gradient 节点
===========


描述
--


定义用于 Shader Graph 中的常量 **渐变 （Gradient）**，但这在着色器内部定义为**结构**（struct）。要对**渐变**采样，必须将其与 [Sample Gradient 节点](Sample-Gradient-Node.md)结合使用。使用单个 **Gradient 节点** 时，可使用不同的 Time 参数对**渐变**进行多次采样。


端口
--

| 名称 | 方向 | 类型 | 描述 |
| --- | --- | --- | --- |
| Out | 输出 | 渐变 | 输出值 |


控件
--

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
|  | Gradient Field |  | 定义渐变。 |


生成的代码示例
-------


以下示例代码表示此节点的一种可能结果。

```
Gradient Unity_Gradient_float()
{
    Gradient g;
    g.type = 1;
    g.colorsLength = 4;
    g.alphasLength = 4;
    g.colors[0] = 0.1;
    g.colors[1] = 0.2;
    g.colors[2] = 0.3;
    g.colors[3] = 0.4;
    g.colors[4] = 0;
    g.colors[5] = 0;
    g.colors[6] = 0;
    g.colors[7] = 0;
    g.alphas[0] = 0.1;
    g.alphas[1] = 0.2;
    g.alphas[2] = 0.3;
    g.alphas[3] = 0.4;
    g.alphas[4] = 0;
    g.alphas[5] = 0;
    g.alphas[6] = 0;
    g.alphas[7] = 0;
    return g;
}

Gradient _Gradient = Unity_Gradient_float();

```

