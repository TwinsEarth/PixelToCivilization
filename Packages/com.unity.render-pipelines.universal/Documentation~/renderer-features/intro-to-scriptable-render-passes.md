# Scriptable Render Pass 介绍

Scriptable Render Pass 允许修改 Unity 渲染场景或场景内对象的方式，使您可以对项目中每个场景的渲染方式进行精细调整。

以下部分介绍 Scriptable Render Pass 的基本概念：

- [Scriptable Render Pass 介绍](#scriptable-render-pass-介绍)
  - [什么是 Scriptable Render Pass?](#什么是-scriptable-render-pass)
  - [场景中的 Scriptable Render Pass](#场景中的-scriptable-render-pass)
  - [其他资源](#其他资源)

可以使用 Scriptable Renderer Feature 将 Scriptable Render Pass 注入渲染器。更多信息请参考 [场景中的 Scriptable Render Pass](#scriptable-render-passes-in-scenes)。


## <a name="scriptable-render-pass"></a>什么是 Scriptable Render Pass?

Scriptable Render Pass 通过 `EnqueuePass` 方法注入到渲染管线，以实现自定义视觉效果。您可以在 MonoBehaviour 脚本中添加 Scriptable Render Pass，并将该脚本作为组件附加到渲染器、相机或 GameObject。

Scriptable Render Pass 允许您：

* 修改场景中材质的属性。
* 更改 Unity 渲染 GameObject 的顺序。
* 读取相机缓冲区并在 Shader 中使用它们。

例如，可以使用 Scriptable Render Pass 在游戏菜单打开时对相机视图应用模糊效果。

Unity 在 URP 渲染循环的特定注入点插入 Scriptable Render Pass。这些注入点决定了 Scriptable Render Pass 影响场景外观的方式。可以更改 Scriptable Render Pass 的注入点，以精确控制其作用效果。有关详细信息，请参阅 [注入点](../customize/custom-pass-injection-points.md)。


## <a name="scriptable-render-passes-in-scenes"></a>场景中的 Scriptable Render Pass

可以通过场景中的任何 GameObject 注入 Scriptable Render Pass，从而精确控制何时激活渲染 Pass。然而，如果需要在多个位置使用相同的渲染效果，建议使用 Scriptable Renderer Feature 进行全局管理，而不是在每个需要的 GameObject 上单独添加 Scriptable Render Pass。

当通过 GameObject 在场景中注入 Scriptable Render Pass 时，需要注意 URP 处理该脚本的方式：

1. **首个渲染的相机将独占 Scriptable Render Pass**  
   该渲染 Pass 仅适用于首个渲染它的相机，之后的相机无法应用该效果。例如，如果场景中有两个相机，并在 `Update` 方法中添加 Scriptable Render Pass，则只有第一个相机能够应用该效果。

2. **后续相机无法使用相同的 Scriptable Render Pass**  
   由于第一个相机已经使用了 Scriptable Render Pass 的实例，而在 `Update` 方法的下一次调用之前不会创建新的实例，因此在此期间渲染的其他相机不会应用该渲染 Pass。

例如，在场景中有两个相机，并在 `Update` 方法中添加 Scriptable Render Pass 时，只有第一个相机能够应用该效果。由于 `Update` 方法在下一个帧之前不会再次运行，第二个相机无法使用 Scriptable Render Pass，因此不会应用该效果。


## 其他资源

* [如何创建自定义 Renderer Feature](create-custom-renderer-feature.md)
* [Scriptable Renderer Feature 参考](scriptable-renderer-features/scriptable-renderer-feature-reference.md)
* [如何通过脚本注入自定义 Render Pass](../customize/custom-pass-injection-points.md)