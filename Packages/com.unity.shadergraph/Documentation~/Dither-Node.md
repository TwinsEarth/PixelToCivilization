
Dither 节点
=========

描述
--

抖动 (Dither) 是特意用于将量化误差随机化的噪声形式。用于防止大尺寸图案，例如图像中的色带。**Dither** 节点在屏幕空间中应用抖动来确保图案均匀分布。这可通过将另一个节点连接到输入 **Screen Position** 进行调整。

此[节点](Node.md)通常用作[主栈](Master-Stack.md) 上 **Alpha Clip Threshold** 的输入，以便为不透明对象提供透明外观。这对于创建看似透明但具有不透明渲染优势（例如写入深度和延迟渲染）的对象非常有用。


端口
--

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| In | 输入 | 动态矢量 | 无 | 输入值 |
| Screen Position | 输入 | Vector 4 | 屏幕位置 | 用于应用抖动模式的坐标 |
| Out | 输出 | 动态矢量 | 无 | 输出值 |


生成的代码示例
-------

以下示例代码表示此节点的一种可能结果。

```
void Unity_Dither_float4(float4 In, float4 ScreenPosition, out float4 Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
}

```

