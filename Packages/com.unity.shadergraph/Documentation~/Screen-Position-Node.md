Screen Position 节点
====================

[](#description)描述
---------------------------

提供对网格顶点或片元的屏幕位置（Screen Position）的访问。X 和 Y 值分别表示水平和垂直位置。使用 **模式** 下拉控件选择输出值的模式。可用模式如下：

*   **Default** - 返回表示标准化 **屏幕位置** 的 X 和 Y 值。标准化 **屏幕位置** 是屏幕位置除以裁剪空间位置的 W 分量的值。X 和 Y 的值范围在 0 到 1 之间，位置 `float2(0,0)` 在屏幕的左下角。此模式下不使用 Z 和 W 值，因此它们始终为 0。
    
*   **RAW** - 返回原始的 **Screen Position** 值，即在裁剪空间位置的 W 分量被除去之前的 **屏幕位置**。位置 `float2(0,0)` 在屏幕的左下角。此模式适合投影使用。
    
*   **Center** - 返回表示标准化 **屏幕位置** 偏移的 X 和 Y 值，使得位置 `float2(0,0)` 在屏幕的中心。X 和 Y 的值范围为 -1 到 1。此模式下不使用 Z 和 W 值，因此它们始终为 0。
    
*   **Tiled** - 返回 **屏幕位置** 偏移，使得位置 `float2(0,0)` 在屏幕中心并通过 `frac` 进行平铺。
    
*   **Pixel** - 以屏幕的实际像素宽度和高度值返回 **屏幕位置**。在此模式下，位置 `float2(0,0)` 在屏幕的左下角。与默认模式范围始终为 0 到 1 不同，像素模式的范围取决于屏幕分辨率。此模式下不使用 Z 和 W 值，因此它们始终为 0。
    

[](#ports)端口
---------------

| 名称 | 方向 | 类型 | 绑定 | 描述 |
| --- | --- | --- | --- | --- |
| Out | 输出 | Vector 4 | 无 | 获取网格的 **屏幕位置**。 |

[](#controls)控件
---------------------

| 名称 | 类型 | 选项 | 描述 |
| --- | --- | --- | --- |
| Mode | 下拉菜单 | Default, Raw, Center, Tiled, Pixel | 选择要用于 **屏幕位置** 输出的坐标空间。 |

[](#generated-code-example)生成代码示例
-------------------------------------------------

以下代码示例表示每种模式的可能输出结果。

**Default**

```
float4 Out = float4(IN.NDCPosition.xy, 0, 0);

```

**Raw**

```
float4 Out = IN.ScreenPosition;

```

**Center**

```
float4 Out = float4(IN.NDCPosition.xy * 2 - 1, 0, 0);

```

**Tiled**

```
float4 Out = frac(float4((IN.NDCPosition.x * 2 - 1) * _ScreenParams.x / _ScreenParams.y, IN.NDCPosition.y * 2 - 1, 0, 0));

```

**Pixel**

```
float4 Out = float4(IN.PixelPosition.xy, 0, 0);

```