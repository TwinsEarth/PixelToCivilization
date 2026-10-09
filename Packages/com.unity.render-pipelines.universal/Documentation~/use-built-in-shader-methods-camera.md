# 在自定义 URP 着色器中使用相机

要在自定义 Universal Render Pipeline (URP) 着色器中使用相机，请按照以下步骤操作：

1. 在着色器文件中的 `HLSLPROGRAM` 内部添加 `#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"`。`Core.hlsl` 文件会导入 `ShaderVariablesFunction.hlsl` 文件。
2. 使用以下方法之一，它们都在 `ShaderVariablesFunction.hlsl` 文件中定义。

| **方法** | **语法** | **描述** |
|----------|----------|----------|
| `GetCameraPositionWS` | `float3 GetCameraPositionWS()` | 返回相机的世界空间位置。 |
| `GetScaledScreenParams` | `float4 GetScaledScreenParams()` | 返回屏幕的宽度和高度（以像素为单位）。 |
| `GetViewForwardDir` | `float3 GetViewForwardDir()` | 返回视图的前向方向（世界空间）。 |
| `IsPerspectiveProjection` | `bool IsPerspectiveProjection()` | 如果相机的投影设置为透视投影，则返回 `true`。 |
| `LinearDepthToEyeDepth` | `half LinearDepthToEyeDepth(half linearDepth)` | 将线性深度缓冲区值转换为视图深度。有关更多信息，请参阅 [Cameras and depth textures](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-CameraDepthTexture.html)。 |
| `TransformScreenUV` | `void TransformScreenUV(inout float2 screenSpaceUV)` | 如果 Unity 使用的是颠倒的坐标空间，则翻转屏幕空间位置的 y 坐标。您还可以输入 `uv` 和屏幕高度（作为 `float`），方法将输出缩放到屏幕大小的像素位置。 |

## 示例

以下 URP 着色器绘制物体表面，颜色代表从表面到相机的方向。

```hlsl
Shader "Custom/DirectionToCamera"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv: TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 viewDirection : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // 获取顶点在不同坐标空间中的位置
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS);
                OUT.positionCS = positions.positionCS;

                // 获取从顶点到相机的方向（世界空间）
                OUT.viewDirection = GetCameraPositionWS() - positions.positionWS.xyz;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 设置片段颜色为方向向量
                return float4(IN.viewDirection, 1);
            }
            ENDHLSL
        }
    }
}
```

## 其他资源

- [URP 中的相机](cameras/camera-differences-in-urp.md)
- [编写自定义着色器](writing-custom-shaders-urp.md)
- [为 URP 兼容性升级自定义着色器](urp-shaders/birp-urp-custom-shader-upgrade-guide.md)
- [Unity 中的 HLSL](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-ShaderPrograms.html)
