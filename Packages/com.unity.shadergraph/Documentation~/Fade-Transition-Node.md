Fade Transition 节点
====================

[](#description)描述
---------------------------

淡入过渡（Fade Transition）是一种通过加入噪声来增加变化性的方法，用于在功能从开启到关闭的过程中产生过渡效果。该节点接收一个淡入值（FadeValue），并使用噪声值（通常来自纹理）对其重新映射。当 FadeValue 为 0 时，输出始终为 0；当 FadeValue 为 1 时，输出始终为 1。在 0 和 1 之间时，过渡将遵循噪声中的图案。

此[节点](Node.md)通常用作 **Alpha** 输入，应用于[主栈](Master-Stack.md)中的LOD过渡。

[](#ports)端口
---------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Texture | 输入 | Texture 2D | 无 | 输入值 |
| Noise | 输入 | Float | 无 | 应用于淡入函数的噪声变化 |
| FadeValue | 输入 | Float | 无 | 需要应用的过渡量 |
| FadeContrast | 输入 | Float | 无 |从完全透明到完全不透明的单像素过渡对比度。较高的值会导致更清晰的过渡边缘 |
| Fade | Output | Float | 无 | 结果淡入值 |

[](#generated-code-example)生成的代码示例
-------

以下示例代码表示此节点的一种可能结果。

```
float Unity_FadeTransitionNode_ApplyFade_float(float noise, float fadeValue, float fadeContrast)
{
    float ret = saturate(fadeValue*(fadeContrast+1)+(noise-1)*fadeContrast);
    return ret;
}

float Result = Unity_FadeTransitionNode_ApplyFade_float(
        _NoiseValue,
        _FadeValue,
        _FadeContrast);

```