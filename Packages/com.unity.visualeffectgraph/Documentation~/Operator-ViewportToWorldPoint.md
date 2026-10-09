# Viewport To World Point

菜单路径：**Operator > Camera > Viewport To World Point**

**Viewport To World Point** 运算符将位置从视口空间转换到世界空间。输入视口空间经过归一化并相对于摄像机。摄像机的左下角是 (0,0)，右上角是 (1,1)。z 位置采用摄像机的世界单位。

注意，[Viewport To World Point](https://docs.unity3d.com/ScriptReference/Camera.ViewportToWorldPoint.html) 将 x-y 屏幕位置变换为 3D 空间中的 x-y-z 位置。

请为该函数提供一个矢量，其中矢量的 x-y 分量为屏幕坐标，z 分量为最终平面到摄像机的距离。

## 运算符设置

| **设置** | **类型** | **描述** |
| --- | --- | --- |
| **Camera** | Enum | 指定要对其深度进行采样的摄像机。选项：  
• **Main**：使用场景中的第一个具有 **MainCamera** 标签的摄像机。  
• **Custom**：使用您在 **Camera** 端口中指定的摄像机。 |

## 运算符属性

| **输入** | **类型** | **描述** |
| --- | --- | --- |
| **Viewport Position** | Vector3 | 视口空间中的位置，经过归一化并相对于摄像机。摄像机的左下角是 (0,0)，右上角是 (1,1)。z 位置采用摄像机的世界单位。 |
| **Camera** | Camera | 要使用的摄像机。  
此属性仅在将 **Camera** 设置为 **Custom** 时显示。 |

| **输出** | **类型** | **描述** |
| --- | --- | --- |
| **position** | [Position](Type-Position.md) | 世界空间中的变换位置。 |
