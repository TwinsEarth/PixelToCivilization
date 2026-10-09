# Main Camera

菜单路径：**Operator > BuiltIn > Main Camera**

Main Camera 运算符从当前主摄像机返回可用信息。它使用与 [Camera.main](https://docs.unity3d.com/ScriptReference/Camera-main.html) 等效的行为，以及与活动可编程渲染管线的集成，进行深度缓冲区和颜色缓冲区的访问。

## 运算符属性

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **o** | [Camera](Type-Camera.md) | 有关主摄像机的信息。 |

## 备注

如果主摄像机不可用，这可能在以下情况下发生：

+   如果场景中没有摄像机游戏对象标记为 **MainCamera**。
+   您手动触发 [模拟](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VisualEffect.Simulate.html)
+   在 Visual Effect 组件的预热更新期间。

如果是这种情况，此运算符返回的 [Camera](Type-Camera.md) 具有以下默认值：

| **属性** | **默认值** |
| --- | --- |
| **Transform** | 变换标识：位置 0，缩放 1，不旋转。 |
| **fieldOfView** | 60.0 |
| **nearPlane** | 0.3 |
| **farPlane** | 1000 |
| **aspectRatio** | 1.0 |
| **pixelDimension** | (1920, 1080) |
| **depthBuffer** | 默认 texture2D 数组（灰色纹理） |
| **colorBuffer** | 默认 texture2D 数组（灰色纹理） |

**depthBuffer** 和 **colorBuffer** 纹理仅在实现了 [VFXManager.SetCameraBuffer](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXManager.SetCameraBuffer.html) 的可编程渲染管线中可用。HDRP 正式支持这种集成。
