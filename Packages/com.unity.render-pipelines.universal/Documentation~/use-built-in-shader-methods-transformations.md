# 在自定义 URP 着色器中转换坐标

要在自定义 Universal Render Pipeline (URP) 着色器中转换坐标，请按照以下步骤操作：

1. 在着色器文件的 `HLSLPROGRAM` 内部添加 `#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"`。`Core.hlsl` 文件会自动导入 `ShaderVariablesFunction.hlsl` 文件。
2. 使用 `ShaderVariablesFunction.hlsl` 文件中的以下方法之一。

| **方法** | **语法** | **描述** |
|-|-|-|
| `GetNormalizedScreenSpaceUV` | `float2 GetNormalizedScreenSpaceUV(float2 positionInClipSpace)` | 将裁剪空间中的位置转换为屏幕空间。 |
| `GetObjectSpaceNormalizeViewDir` | `half3 GetObjectSpaceNormalizeViewDir(float3 positionInObjectSpace)` | 将物体空间中的位置转换为朝向观察者的标准化方向。 |
| `GetVertexNormalInputs` | `VertexNormalInputs GetVertexNormalInputs(float3 normalInObjectSpace)` | 将物体空间中的法线转换为世界空间中的切线、双切线和法线。你还可以同时输入法线和 `float4` 形式的切线。 |
| `GetVertexPositionInputs` | `VertexPositionInputs GetVertexPositionInputs(float3 positionInObjectSpace)` | 将物体空间中的顶点位置转换为世界空间、视图空间、裁剪空间和标准化设备坐标（NDC）。 |
| `GetWorldSpaceNormalizeViewDir` | `half3 GetWorldSpaceNormalizeViewDir(float3 positionInWorldSpace)` | 计算世界空间中的位置到观察者的方向，并进行标准化。 |
| `GetWorldSpaceViewDir` | `float3 GetWorldSpaceViewDir(float3 positionInWorldSpace)` | 计算世界空间中的位置到观察者的方向。 |

## 结构体

### VertexPositionInputs

使用 `GetVertexPositionInputs` 方法可以获取此结构体。

| **字段** | **描述** |
|-------|-------------|
| `float3 positionWS` | 世界空间中的位置。 |
| `float3 positionVS` | 视图空间中的位置。 |
| `float4 positionCS` | 裁剪空间中的位置。 |
| `float4 positionNDC` | 归一化设备坐标（NDC）中的位置。 |

### VertexNormalInputs

使用 `GetVertexNormalInputs` 方法可以获取此结构体。

| **字段** | **描述** |
|---------|-------------|
| `real3 tangentWS` | 世界空间中的切线。 |
| `real3 bitangentWS` | 世界空间中的双切线。 |
| `float3 normalWS` | 世界空间中的法线。 |

## 示例

以下 URP 着色器使用屏幕空间中的位置绘制物体表面颜色。

```hlsl
Shader "Custom/ScreenSpacePosition"
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
                float3 positionWS : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);

                // 获取顶点在不同空间中的位置
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS);

                // 将 positionWS 设置为顶点的屏幕空间位置
                OUT.positionWS = positions.positionWS.xyz;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 将片段颜色设置为屏幕空间位置向量
                return float4(IN.positionWS.xy, 0, 1);
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
- [Shader 语义](https://docs.unity.cn/cn/tuanjiemanual/Manual/SL-ShaderSemantics.html)