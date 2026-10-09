# Load CameraBuffer

菜单路径 : **Operator > Sampling > Load CameraBuffer**

**Load CameraBuffer** Operator 允许您读取指定坐标的 CameraBuffer 纹素值。此运算符返回 float4 纹素值，而不进行任何筛选。

这转换为对高级着色语言 （HLSL） 纹理的**Load** 调用。

## 运算符属性

| **Input**        | **类型**                             | **描述**                        |
| ---------------- | ------------------------------------ | -------------------------------------- |
| **CameraBuffer** | [CameraBuffer](Type-CameraBuffer.md) | 要从中读取的 CameraBuffer。        |
| **X**            | uint                                 | 要读取的纹素的 X 坐标。 |
| **Y**            | uint                                 | 要读取的纹素的 Y 坐标。 |

| **Output** | **类型** | **描述**         |
| ---------- | -------- | ----------------------- |
| **s**      | Vector4  | 纹素的值。 |

## 局限性

这是仅限 GPU 的 Operator，因此在插入 **Spawn Context** 端口时不起作用。
