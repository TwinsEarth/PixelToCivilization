# 从 URP 着色器库导入文件

Universal Render Pipeline (URP) 的高级着色语言（HLSL）着色器文件位于项目中的 `Packages/com.unity.render-pipelines.universal/ShaderLibrary/` 文件夹中。

要将着色器文件导入到自定义着色器文件中，请在着色器文件中的 `HLSLPROGRAM` 内部添加 `#include` 指令。例如：

```hlsl
HLSLPROGRAM
...
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
...
ENDHLSL
```

然后，您可以使用文件中的辅助方法。例如：

```hlsl
float3 cameraPosition = GetCameraPositionWS();
```

有关不同着色器文件的更多信息，请参考 [Shader methods in URP](use-built-in-shader-methods.md)。

您还可以从核心脚本渲染管线（SRP）导入着色器文件。请参考 [Shader methods in Scriptable Render Pipeline (SRP) Core](built-in-shader-methods.md)。

## 示例

请参考 [编写自定义着色器](writing-custom-shaders-urp.md)，了解如何使用 URP 着色器库文件中的变量和辅助方法的示例。

## 其他资源

- [HLSL 中的 include 和 include_with_pragmas 指令](https://docs.unity.cn/cn/tuanjiemanual/Manual/shader-include-directives.html)
