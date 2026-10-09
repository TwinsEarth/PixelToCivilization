# URP 中的着色器方法

Universal Render Pipeline (URP) 提供了一套 High-Level Shader Language (HLSL) 着色器文件库，包含了许多辅助方法。你可以将这些文件导入到自定义着色器文件中，并使用其中的辅助方法。

|**页面**|**描述**|
|-|-|
|[从 URP 着色器库导入文件](use-built-in-shader-methods-import.md)|使用 `#include` 指令在 HLSL 中导入 URP 着色器文件。|
|[在自定义 URP 着色器中转换坐标](use-built-in-shader-methods-transformations.md)|在不同坐标空间之间转换顶点、片段、法线和切线位置。|
|[在自定义 URP 着色器中使用相机](use-built-in-shader-methods-camera.md)|获取相机的位置和方向。|
|[在自定义 URP 着色器中使用光照](use-built-in-shader-methods-lighting.md)|获取场景中的光源并计算光照。|
|[在自定义 URP 着色器中使用阴影](use-built-in-shader-methods-shadows.md)|从场景中的光源获取阴影数据，并计算阴影。|

## 额外资源

- [编写自定义着色器](writing-custom-shaders-urp.md)
- [将自定义着色器升级为 URP 兼容](urp-shaders/birp-urp-custom-shader-upgrade-guide.md)
- [Unity 中的 HLSL](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-ShaderPrograms.html)
- [Scriptable Render Pipeline (SRP) Core 中的着色器方法](https://docs.unity.cn/cn/Packages-cn/com.unity.render-pipelines.core@latest/manual/built-in-shader-methods.html)
