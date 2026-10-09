# 设置分屏渲染

在 Universal Render Pipeline (URP) 中，多个 [基础摄像机](camera-types-and-render-type.md#base-camera) 或 [摄像机堆栈](camera-stacking.md) 可以同时渲染到同一个渲染目标。这使得创建分屏渲染效果成为可能。

如果多个基础摄像机或摄像机堆栈渲染到渲染目标的同一区域，Unity 将对该区域的每个像素多次绘制。优先级最高的基础摄像机或摄像机堆栈会在最后被绘制，从而覆盖之前绘制的像素。有关摄像机渲染顺序优化的更多信息，请参阅 [理解摄像机渲染顺序](cameras-advanced.md)。

你可以使用基础摄像机的 **Viewport Rect** 属性来定义渲染目标上的渲染区域。有关视口坐标的更多信息，请参阅 [Unity 手册](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-Camera.html) 和 [API 文档](https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/Camera-rect.html)。

## 设置分屏渲染

![在 URP 中设置分屏渲染](Images/camera-split-screen-viewport.png)

1. 在场景中创建一个摄像机。它的 **Render Mode** 默认设置为 **Base**，使其成为一个基础摄像机。
2. 选择该摄像机。在 Inspector 中，滚动到输出部分。将 **Viewport rect** 的值更改为以下内容：
    * X: 0
    * Y: 0
    * W: 0.5
    * H: 1
3. 在场景中再创建一个摄像机。它的 **Render Mode** 默认设置为 **Base**，使其成为一个基础摄像机。
4. 选择该摄像机。在 Inspector 中，滚动到输出部分。将 **Viewport rect** 的值更改为以下内容：
    * X: 0.5
    * Y: 0
    * W: 0.5
    * H: 1

Unity 将第一个摄像机渲染到屏幕的左侧，第二个摄像机渲染到屏幕的右侧。

你可以通过脚本设置摄像机的 Viewport rect，方法是设置其 `rect` 属性，例如：

```c#
myUniversalAdditionalCameraData.rect = new Rect(0.5f, 0f, 0.5f, 0f);
```
