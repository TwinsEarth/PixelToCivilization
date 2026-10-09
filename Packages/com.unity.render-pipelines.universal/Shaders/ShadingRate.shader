Shader "Hidden/Universal Render Pipeline/ShadingRate"
{
    SubShader
    {
        Pass
        {
            Name "Shading Rate"

            Tags{ "LightMode" = "ShadingRate" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _RendererColor;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _RendererColor;
                UNITY_OPAQUE_ALPHA(col.a);
                return col;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
