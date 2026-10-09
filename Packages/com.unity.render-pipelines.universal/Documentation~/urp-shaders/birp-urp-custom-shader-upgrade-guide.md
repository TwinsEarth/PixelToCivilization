# 升级自定义 Shader 以兼容 URP

针对内置渲染管线（Built-In Render Pipeline）编写的自定义 Shader 与通用渲染管线（Universal Render Pipeline, URP）不兼容，无法通过渲染管线转换器（Render Pipeline Converter）自动升级。因此，您需要手动重写不兼容的 Shader 代码，使其适用于 URP。

您也可以使用 Shader Graph 重新创建自定义 Shader。有关详细信息，请参阅 [Shader Graph 文档](https://docs.unity.cn/cn/Packages-cn/com.unity.shadergraph@latest)。

> **注意**：在升级到 URP 时，使用自定义 Shader 的材质将在场景中变为洋红色（亮粉色），以指示错误。

本指南演示如何将 Built-In Render Pipeline 的自定义无光照 Shader 升级为完全兼容 URP 的 Shader，涵盖以下部分：

- [升级自定义 Shader 以兼容 URP](#升级自定义-shader-以兼容-urp)
  - [Built-In Render Pipeline 的自定义 Shader 示例](#built-in-render-pipeline-的自定义-shader-示例)
  - [使自定义 Shader 兼容 URP](#使自定义-shader-兼容-urp)
  - [启用 Shader 的平铺（Tiling）和偏移（Offset）](#启用-shader-的平铺tiling和偏移offset)
  - [完整的 Shader 代码](#完整的-shader-代码)

## Built-In Render Pipeline 的自定义 Shader 示例

以下是一个简单的无光照（Unlit） Shader，适用于 Built-In Render Pipeline。本指南将演示如何升级此 Shader 以使其兼容 URP。

```c++
Shader "Custom/UnlitShader"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Main Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv: TEXCOORD0;
            };

            float4 _Color;
            sampler2D _MainTex;

            v2f vert(appdata_base v)
            {
                v2f o;

                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texel = tex2D(_MainTex, i.uv);
                return texel * _Color;
            }
            ENDCG
        }
    }
}
```

## 使自定义 Shader 兼容 URP

Built-In Render Pipeline 的 Shader 存在以下两个问题，可在 **Inspector** 窗口中看到：

* **Material property is found in another cbuffer**（材质属性存在于另一个 cbuffer）。
* **SRP Batcher** 属性显示 **not compatible**（不兼容）。

按照以下步骤解决这些问题，并使 Shader 兼容 URP 及 SRP Batcher：

1. 将 `CGPROGRAM` 和 `ENDCG` 替换为 `HLSLPROGRAM` 和 `ENDHLSL`。
2. 更新 `#include` 语句，引用 `Core.hlsl` 文件：

    ```c++
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    ```

    > **注意**：`Core.hlsl` 包含核心 SRP 库、URP Shader 变量、矩阵定义和变换，但不包含光照函数或默认结构体。

3. 在 Shader 标签中添加 `"RenderPipeline" = "UniversalPipeline"`：

    ```c++
    Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
    ```

4. 使用 `Varyings` 结构体替换 `struct v2f`，并更新 Shader 变量：

    ```c++
    struct Varyings
    {
        float4 positionHCS  : SV_POSITION;
        float2 uv : TEXCOORD0;
    };
    ```

5. 在 `#include` 语句下方定义 `Attributes` 结构体：

    ```c++
    struct Attributes
    {
        float4 positionOS   : POSITION;
        float2 uv : TEXCOORD0;
    };
    ```

6. 更新 `vert` 函数，使其使用 `Attributes` 作为输入，并使用 `TransformObjectToHClip` 进行变换：

    ```c++
    Varyings vert(Attributes IN)
    {
        Varyings OUT;

        OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
        OUT.uv = IN.uv;

        return OUT;
    }
    ```

7. 在 `CBUFFER` 代码块中声明材质属性，使 Shader 兼容 SRP Batcher：

    ```c++
    CBUFFER_START(UnityPerMaterial)
    float4 _Color;
    sampler2D _MainTex;
    CBUFFER_END
    ```

8. 更新 `frag` 函数，使其使用 `Varyings` 作为输入，并将 `fixed4` 替换为 `half4`：

    ```c++
    half4 frag(Varyings IN) : SV_Target
    {
        half4 texel = tex2D(_MainTex, IN.uv);
        return texel * _Color;
    }
    ```

升级后，Shader 现已兼容 SRP Batcher，并可在 **Inspector** 窗口中验证：

* **Material property is found in another cbuffer** 的警告不再出现。
* **SRP Batcher** 属性显示 **compatible**（兼容）。

## 启用 Shader 的平铺（Tiling）和偏移（Offset）

要使 **Tiling** 和 **Offset** 属性生效，请执行以下更改：

1. 将 `_MainTex` 重命名为 `_BaseMap`，并删除 `[NoScaleOffset]`。
2. 在 `_BaseMap` 属性上添加 `[MainTexture]`，并在 `_Color` 属性上添加 `[MainColor]`：

    ```c++
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _Color("Color", Color) = (1,1,1,1)
    }
    ```

3. 添加 `TEXTURE2D(_BaseMap);` 和 `SAMPLER(sampler_BaseMap);` 宏：

    ```c++
    TEXTURE2D(_BaseMap);
    SAMPLER(sampler_BaseMap);
    ```

4. 在 `CBUFFER` 代码块中使用 `_BaseMap_ST` 存储 Tiling 和 Offset：

    ```c++
    CBUFFER_START(UnityPerMaterial)
    float4 _Color;
    float4 _BaseMap_ST;
    CBUFFER_END
    ```

5. 使用 `SAMPLE_TEXTURE2D` 宏替换 `tex2D`：

    ```c++
    half4 frag(Varyings IN) : SV_Target
    {
        half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
        return texel * _Color;
    }
    ```

6. 在 `vert` 函数中使用 `TRANSFORM_TEX` 处理 Tiling 和 Offset：

    ```c++
    Varyings vert(Attributes IN)
    {
        Varyings OUT;

        OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
        OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

        return OUT;
    }
    ```

## 完整的 Shader 代码

```c++
Shader "Custom/UnlitShader"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _Color("Color", Color) = (1,1,1,1)
    }

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
                float4 positionOS   : POSITION;
                float2 uv: TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv: TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _Color;
            }
            ENDHLSL
        }
    }
}
```