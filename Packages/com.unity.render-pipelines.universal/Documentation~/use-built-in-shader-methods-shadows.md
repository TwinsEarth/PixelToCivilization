# 在自定义 URP 着色器中使用阴影

要在自定义 Universal Render Pipeline (URP) 着色器中使用阴影，请按照以下步骤操作：

1. 在着色器文件的 `HLSLPROGRAM` 内部添加 `#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"`。`Core.hlsl` 文件会自动导入 `Shadows.hlsl` 和 `RealtimeLights.hlsl` 文件。
2. 使用以下部分的方法。

## 获取阴影空间中的位置

使用这些方法将位置转换为阴影贴图坐标。

| **方法** | **语法** | **描述** |
| --- | --- | --- |
| `GetShadowCoord` | `float4 GetShadowCoord(VertexPositionInputs vertexInputs)` | 将顶点位置转换为阴影空间。有关 `VertexPositionInputs` 结构体的信息，请参阅 [在自定义 URP 着色器中转换坐标](use-built-in-shader-methods-transformations.md)。 |
| `TransformWorldToShadowCoord` | `float4 TransformWorldToShadowCoord(float3 positionInWorldSpace)` | 将世界空间中的位置转换为阴影空间。 |

## 计算阴影

以下方法使用阴影贴图计算阴影。在使用这些方法之前，请先完成以下步骤：

1. 确保场景中存在包含 `ShadowCaster` 着色器 Pass 的物体，例如使用 `Universal Render Pipeline/Lit` 着色器的物体。
2. 在着色器中添加 `#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN`，以便访问主光源的阴影贴图。
3. 在着色器中添加 `#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS`，以便访问额外光源的阴影贴图。

| **方法** | **语法** | **描述** |
| --- | --- | --- |
| `GetMainLight` | `Light GetMainLight(float4 shadowCoordinates)` | 返回场景中的主光源，并根据阴影坐标位置计算 `shadowAttenuation` 值。 |
| `ComputeCascadeIndex` | `half ComputeCascadeIndex(float3 positionInWorldSpace)` | 返回世界空间中位置的阴影级联索引。有关详细信息，请参阅 [阴影级联](https://docs.unity.cn/cn/tuanjiemanual/Manual/shadow-cascades.html)。 |
| `MainLightRealtimeShadow` | `half MainLightRealtimeShadow(float4 shadowCoordinates)` | 在指定的阴影坐标位置从主光源的阴影贴图中获取阴影值。有关更多信息，请参阅 [阴影映射](https://docs.unity.cn/cn/tuanjiemanual/Manual/shadow-mapping.html)。 |
| `AdditionalLightRealtimeShadow` | `half AdditionalLightRealtimeShadow(int lightIndex, float3 positionInWorldSpace)` | 在世界空间中的指定位置，从额外光源的阴影贴图中获取阴影值。 |
| `GetMainLightShadowFade` | `half GetMainLightShadowFade(float3 positionInWorldSpace)` | 基于世界空间位置与相机的距离，返回主光源阴影的渐变程度。 |
| `GetAdditionalLightShadowFade` | `half GetAdditionalLightShadowFade(float3 positionInWorldSpace)` | 基于世界空间位置与相机的距离，返回额外光源阴影的渐变程度。 |
| `ApplyShadowBias` | `float3 ApplyShadowBias(float3 positionInWorldSpace, float3 normalWS, float3 lightDirection)` | 在世界空间的位置上添加阴影偏移。有关更多信息，请参阅 [阴影问题排查](https://docs.unity.cn/cn/tuanjiemanual/Manual/ShadowPerformance.html)。 |

## 示例

以下 URP 着色器在表面上绘制简单的阴影。

要生成阴影，请确保场景中存在包含 `ShadowCaster` 着色器 Pass 的物体，例如使用 `Universal Render Pipeline/Lit` 着色器的物体。

```hlsl
Shader "Custom/SimpleShadows"
{
    SubShader
    {

        Tags { "RenderType" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS  : POSITION;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float4 shadowCoords : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);

                // 获取顶点位置的 VertexPositionInputs
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);

                // 将顶点位置转换为阴影贴图坐标
                float4 shadowCoordinates = GetShadowCoord(positions);

                // 传递阴影坐标到片段着色器
                OUT.shadowCoords = shadowCoordinates;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 从阴影贴图中获取阴影值
                half shadowAmount = MainLightRealtimeShadow(IN.shadowCoords);

                // 设置片段颜色为阴影值
                return shadowAmount;
            }
            
            ENDHLSL
        }
    }
}
```

## 额外资源

- [阴影](https://docs.unity.cn/cn/tuanjiemanual/Manual/Shadows.html)
- [URP 中的阴影](Shadows-in-URP.md)
- [编写自定义着色器](writing-custom-shaders-urp.md)
- [将自定义着色器升级为 URP 兼容](urp-shaders/birp-urp-custom-shader-upgrade-guide.md)
- [Unity 中的 HLSL](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-ShaderPrograms.html)