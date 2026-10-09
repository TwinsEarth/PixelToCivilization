# 将摄像机的输出渲染到渲染纹理

在 Universal Render Pipeline (URP) 中，摄像机可以将画面渲染到屏幕或者 [Render Texture](https://docs.unity.cn/cn/tuanjiemanual/Manual/class-RenderTexture.html)。默认情况下，渲染目标为屏幕，这是最常见的用法，但将画面渲染到 Render Texture 可以实现例如闭路电视 (CCTV) 显示器这样的效果。

当某个摄像机将画面渲染到 Render Texture 时，必须有另一个摄像机将该 Render Texture 再次渲染到屏幕。在 URP 中，所有渲染到 Render Texture 的摄像机会在渲染到屏幕的摄像机之前完成其渲染循环。这保证了 Render Texture 在显示到屏幕前已被正确生成。有关 URP 中摄像机渲染顺序的更多信息，请参阅 [Rendering order and overdraw](cameras-advanced.md)。

## 渲染到可显示在屏幕上的渲染纹理

1. 在项目中创建一个 Render Texture 资源。选择 **Assets** > **Create** > **Render Texture**。
2. 在场景中创建一个 Quad 游戏对象。
3. 在项目中创建一个材质。
4. 在 Inspector 中，将 Render Texture 拖动到材质的 **Base Map** 属性字段。
5. 在场景视图中，将材质拖动到 Quad 上。
6. 在场景中创建一个摄像机。
7. 选择这个基础摄像机，然后在 Inspector 中将 Render Texture 拖动到 **Output Texture** 属性。
8. 在场景中创建另一个摄像机。
9. 将 Quad 放置在新的基础摄像机视野内。

现在，第一个摄像机将其画面渲染到 Render Texture，而第二个摄像机将场景（包括该 Render Texture）渲染到屏幕。

你可以通过脚本设置摄像机的输出目标，方法是设置摄像机的 `targetTexture` 属性：

```c#
myCamera.targetTexture = myRenderTexture;
```
