# 在自定义 URP 着色器中使用光照

要在自定义 Universal Render Pipeline (URP) 着色器中使用光照，请按照以下步骤操作：

1. 在着色器文件中的 `HLSLPROGRAM` 内部添加 `#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"`。
2. 使用以下部分中的任何方法。

## 获取光照数据

`Lighting.hlsl` 文件导入了 `RealtimeLights.hlsl` 文件，该文件包含以下方法。

| **方法** | **语法** | **描述** |
|:-|:-|:-|
| `GetMainLight` | `Light GetMainLight()` | 返回场景中的主光源。 |
| `GetAdditionalLight` | `Light GetAdditionalLight(uint lightIndex, float3 positionInWorldSpace)` | 返回影响 `positionWS` 的 `lightIndex` 额外光源。例如，如果 `lightIndex` 为 `0`，该方法返回第一个额外光源。 |
| `GetAdditionalLightsCount` | `int GetAdditionalLightsCount()` | 返回额外光源的数量。 |

请参考 [在自定义 URP 着色器中使用阴影](use-built-in-shader-methods-shadows.md)，了解如何使用这些方法的不同版本来计算阴影。

### 为表面法线计算光照

| **方法** | **语法** | **描述** |
|:-|:-|:-|
| `LightingLambert` | `half3 LightingLambert(half3 lightColor, half3 lightDirection, half3 surfaceNormal)` | 使用 Lambert 模型计算并返回表面法线的漫反射光照。 |
| `LightingSpecular` | `half3 LightingSpecular(half3 lightColor, half3 lightDirection, half3 surfaceNormal, half3 viewDirection, half4 specularAmount, half smoothnessAmount)` | 使用 [简单阴影](shading-model.md#simple-shading) 计算并返回表面法线的高光反射光照。 |

## 计算环境光遮蔽

`Lighting.hlsl` 文件导入了 `AmbientOcclusion.hlsl` 文件，该文件包含以下方法。

| **方法** | **语法** | **描述** |
|:-|:-|:-|
| `SampleAmbientOcclusion` | `half SampleAmbientOcclusion(float2 normalizedScreenSpaceUV)` | 返回屏幕空间中指定位置的环境光遮蔽值，其中 0 表示遮蔽，1 表示未遮蔽。 |
| `GetScreenSpaceAmbientOcclusion` | `AmbientOcclusionFactor GetScreenSpaceAmbientOcclusion(float2 normalizedScreenSpaceUV)` | 返回屏幕空间中指定位置的间接和直接环境光遮蔽值，其中 0 表示遮蔽，1 表示未遮蔽。 |

有关更多信息，请参考 [环境光遮蔽](post-processing-ssao.md)。


## 结构体

### AmbientOcclusionFactor

使用 `GetScreenSpaceAmbientOcclusion` 方法返回此结构体。

| **字段** | **描述** |
|:-|:-|
| `half indirectAmbientOcclusion` | 由物体遮挡间接光导致的阴影量，表示物体受到环境光遮蔽的程度。 |
| `half directAmbientOcclusion` | 由物体遮挡直接光导致的阴影量，表示物体受到直接光遮蔽的程度。 |

### Light

使用 `GetMainLight` 和 `GetAdditionalLight` 方法返回此结构体。

| **字段** | **描述** |
|:-|:-|
| `half3 direction` | 光源的方向。 |
| `half3 color` | 光源的颜色。 |
| `float distanceAttenuation` | 光源的强度，基于光源与物体之间的距离。 |
| `half shadowAttenuation` | 光源的强度，基于物体是否在阴影中。 |
| `uint layerMask` | 光源的层掩码。 |

## 示例

以下是一个 URP 着色器，它绘制物体表面接收到的来自主方向光的光照量。

```hlsl
Shader "Custom/LambertLighting"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv: TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv: TEXCOORD0;
                half3 lightAmount : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);

                // 获取顶点法线输入，它包含了世界空间中的法线
                VertexNormalInputs positions = GetVertexNormalInputs(IN.positionOS);

                // 获取主光源的属性
                Light light = GetMainLight();

                // 计算顶点接收到的光照量
                OUT.lightAmount = LightingLambert(light.color, light.direction, positions.normalWS.xyz);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 设置片段的颜色为插值后的光照量
                return float4(IN.lightAmount, 1);
            }
            ENDHLSL
        }
    }
}
```

## 额外资源

- [编写自定义着色器](writing-custom-shaders-urp.md)
- [将自定义着色器升级为 URP 兼容](urp-shaders/birp-urp-custom-shader-upgrade-guide.md)
- [Unity 中的 HLSL](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-ShaderPrograms.html)
- [漫反射](https://docs.unity.cn/cn/tuanjiemanual/Manual/shader-NormalDiffuse.html)
- [高光反射](https://docs.unity.cn/cn/tuanjiemanual/Manual/shader-NormalSpecular.html)



