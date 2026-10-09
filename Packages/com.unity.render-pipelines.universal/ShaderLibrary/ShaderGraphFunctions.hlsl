#ifndef UNITY_GRAPHFUNCTIONS_LW_INCLUDED
#define UNITY_GRAPHFUNCTIONS_LW_INCLUDED

#define SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv) shadergraph_LWSampleSceneDepth(uv)
#define SHADERGRAPH_SAMPLE_SCENE_COLOR(uv) shadergraph_LWSampleSceneColor(uv)
#define SHADERGRAPH_SAMPLE_SCENE_NORMAL(uv) shadergraph_LWSampleSceneNormals(uv)
#ifdef UNITY_GPU_DRIVEN_PIPELINE
#define SHADERGRAPH_BAKED_GI(positionWS, normalWS, positionSS, uvStaticLightmap, uvDynamicLightmap, applyScaling, instance, materialOffset, ddx, ddy) shadergraph_LWBakedGI(positionWS, normalWS, positionSS, uvStaticLightmap, uvDynamicLightmap, applyScaling)
#else
#define SHADERGRAPH_BAKED_GI(positionWS, normalWS, positionSS, uvStaticLightmap, uvDynamicLightmap, applyScaling) shadergraph_LWBakedGI(positionWS, normalWS, positionSS, uvStaticLightmap, uvDynamicLightmap, applyScaling)
#endif

#define SHADERGRAPH_REFLECTION_PROBE(viewDir, normalOS, lod) shadergraph_LWReflectionProbe(viewDir, normalOS, lod)
#define SHADERGRAPH_FOG(position, color, density) shadergraph_LWFog(position, color, density)
#define SHADERGRAPH_AMBIENT_SKY unity_AmbientSky
#define SHADERGRAPH_AMBIENT_EQUATOR unity_AmbientEquator
#define SHADERGRAPH_AMBIENT_GROUND unity_AmbientGround
#define SHADERGRAPH_MAIN_LIGHT_DIRECTION shadergraph_URPMainLightDirection
#define SHADERGRAPH_MAIN_LIGHT_COLOR shadergraph_URPMainLightColor
#define SHADERGRAPH_SHADOWMASK(LightmapUV) shadergraph_LWShadowMask(LightmapUV)
#define SHADERGRAPH_MAINLIGHTREALTIMESHADOW(positionWS) shadergraph_LWMainLightRealtimeShadow(positionWS)
#define SHADERGRAPH_MAINLIGHTSHADOW(positionWS,LightmapUV) shadergraph_LWMainLightShadow(positionWS,LightmapUV)
#define SHADERGRAPH_RENDERER_BOUNDS_MIN shadergraph_RendererBoundsWS_Min()
#define SHADERGRAPH_RENDERER_BOUNDS_MAX shadergraph_RendererBoundsWS_Max()
#define SHADERGRAPH_AMBIENT_OCCLUSION(screenSpaceUV, occlusion, indirectAO, directAO) shadergraph_URPAmbientOcclusion(screenSpaceUV, occlusion, indirectAO, directAO)
#define SHADERGRAPH_APPROX_GGX_TERM(normalWS, roughness, lightDirWS, viewDirWS, specular) shadergraph_URPApproxGGXTerm(normalWS, roughness, lightDirWS, viewDirWS, specular)
#define SHADERGRAPH_BLINN_PHONG_TERM(normalWS, roughness, lightDirWS, viewDirWS, specular) shadergraph_URPBlinnPhongTerm(normalWS, roughness, lightDirWS, viewDirWS, specular)
#define SHADERGRAPH_COTTON_WOOL_TERM(normalWS, roughness, lightDirWS, viewDirWS, specular) shadergraph_URPCottonWoolTerm(normalWS, roughness, lightDirWS, viewDirWS, specular)
#define SHADERGRAPH_SILK_TERM(normalWS, tangentWS, bitangentWS, roughness, lightDirWS, viewDirWS, specular, anisotropy) shadergraph_URPSilkTerm(normalWS, tangentWS, bitangentWS, roughness, lightDirWS, viewDirWS, specular, anisotropy)
#define SHADERGRAPH_ANISOTROPY_TERM(normalWS, tangentWS, bitangentWS, roughness, lightDirWS, viewDirWS, specular, anisotropy) shadergraph_URPAnitrosopyTerm(normalWS, tangentWS, bitangentWS, roughness, lightDirWS, viewDirWS, specular, anisotropy)
#define SHADERGRAPH_LAMBERTIAN_TERM(diffuse) shadergraph_URPLambertianTerm(diffuse)
#define SHADERGRAPH_FABRIC_LAMBERT_TERM(diffuse, roughness) shadergraph_URPFabricLambertTerm(diffuse, roughness)
#define SHADERGRAPH_DISNEY_DIFFUSE_TERM(diffuse, normalWS, lightDirWS, viewDirWS, roughness) shadergraph_URPDisneyDiffuseTerm(diffuse, normalWS, lightDirWS, viewDirWS, roughness)


#if defined(REQUIRE_DEPTH_TEXTURE)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#endif

#if defined(REQUIRE_OPAQUE_TEXTURE)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#endif

#if defined(REQUIRE_NORMAL_TEXTURE)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#endif

float shadergraph_LWSampleSceneDepth(float2 uv)
{
#if defined(REQUIRE_DEPTH_TEXTURE)
    return SampleSceneDepth(uv);
#else
    return 0;
#endif
}

float3 shadergraph_LWSampleSceneColor(float2 uv)
{
#if defined(REQUIRE_OPAQUE_TEXTURE)
    return SampleSceneColor(uv);
#else
    return 0;
#endif
}

float3 shadergraph_LWSampleSceneNormals(float2 uv)
{
#if defined(REQUIRE_NORMAL_TEXTURE)
    return SampleSceneNormals(uv);
#else
    return 0;
#endif
}

float3 shadergraph_LWBakedGI(float3 positionWS, float3 normalWS, uint2 positionSS, float2 uvStaticLightmap, float2 uvDynamicLightmap, bool applyScaling)
{
#ifdef LIGHTMAP_ON
    if (applyScaling)
    {
        uvStaticLightmap = uvStaticLightmap * unity_LightmapST.xy + unity_LightmapST.zw;
        uvDynamicLightmap = uvDynamicLightmap * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
    }
#if defined(DYNAMICLIGHTMAP_ON)
    return SampleLightmap(uvStaticLightmap, uvDynamicLightmap, normalWS);
#else
    return SampleLightmap(uvStaticLightmap, normalWS);
#endif
#else
    return SampleSH(normalWS);
#endif
}

float3 shadergraph_LWReflectionProbe(float3 viewDir, float3 normalOS, float lod)
{
    float3 reflectVec = reflect(-viewDir, normalOS);
#if USE_FORWARD_PLUS
    return SAMPLE_TEXTURECUBE_LOD(_GlossyEnvironmentCubeMap, sampler_GlossyEnvironmentCubeMap, reflectVec, lod).rgb;
#else
    return DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVec, lod), unity_SpecCube0_HDR);
#endif
}

void shadergraph_LWFog(float3 positionOS, out float4 color, out float density)
{
    color = unity_FogColor;
    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    float viewZ = -TransformWorldToView(TransformObjectToWorld(positionOS)).z;
    float nearZ0ToFarZ = max(viewZ - _ProjectionParams.y, 0);
    // ComputeFogFactorZ0ToFar returns the fog "occlusion" (0 for full fog and 1 for no fog) so this has to be inverted for density.
    density = 1.0f - ComputeFogIntensity(ComputeFogFactorZ0ToFar(nearZ0ToFarZ));
    #else
    density = 0.0f;
    #endif
}
// This function assumes the bitangent flip is encoded in tangentWS.w
float3x3 BuildTangentToWorld(float4 tangentWS, float3 normalWS)
{
    // tangentWS must not be normalized (mikkts requirement)

    // Normalize normalWS vector but keep the renormFactor to apply it to bitangent and tangent
    float3 unnormalizedNormalWS = normalWS;
    float renormFactor = 1.0 / length(unnormalizedNormalWS);

    // bitangent on the fly option in xnormal to reduce vertex shader outputs.
    // this is the mikktspace transformation (must use unnormalized attributes)
    float3x3 tangentToWorld = CreateTangentToWorld(unnormalizedNormalWS, tangentWS.xyz, tangentWS.w > 0.0 ? 1.0 : -1.0);

    // surface gradient based formulation requires a unit length initial normal. We can maintain compliance with mikkts
    // by uniformly scaling all 3 vectors since normalization of the perturbed normal will cancel it.
    tangentToWorld[0] = tangentToWorld[0] * renormFactor;
    tangentToWorld[1] = tangentToWorld[1] * renormFactor;
    tangentToWorld[2] = tangentToWorld[2] * renormFactor;       // normalizes the interpolated vertex normal

    return tangentToWorld;
}

float3 shadergraph_URPMainLightDirection()
{
    return GetMainLight().direction;
}

//URP MainLightColor
float3 shadergraph_URPMainLightColor()
{
    return GetMainLight().color;
}

float4 shadergraph_LWShadowMask(float2 LightmapUV)
{
    // To ensure backward compatibility we have to avoid using shadowMask input, as it is not present in older shaders
    #if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
    OUTPUT_LIGHTMAP_UV(LightmapUV, unity_LightmapST, LightmapUV);
    half4 shadowMask = SAMPLE_SHADOWMASK(LightmapUV);
    #elif !defined (LIGHTMAP_ON)
    half4 shadowMask = unity_ProbesOcclusion;
    #else
    half4 shadowMask = half4(1, 1, 1, 1);
    #endif
    return shadowMask;

}

float shadergraph_LWMainLightRealtimeShadow(float3 positionWS)
{
float4 shadowCoord;
#if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
    shadowCoord = ComputeScreenPos(TransformWorldToHClip(positionWS));
#else
    shadowCoord = TransformWorldToShadowCoord(positionWS);
#endif
   
    return  MainLightRealtimeShadow(shadowCoord);

}

float shadergraph_LWMainLightShadow(float3 positionWS,float2 LightmapUV)
{
    float4 shadowCoord;
#if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
    shadowCoord = ComputeScreenPos(TransformWorldToHClip(positionWS));
#else
    shadowCoord = TransformWorldToShadowCoord(positionWS);
#endif
    float ShadowAtten = MainLightRealtimeShadow(shadowCoord);
    OUTPUT_LIGHTMAP_UV(LightmapUV, unity_LightmapST, LightmapUV);
    float4 shadowMask = SAMPLE_SHADOWMASK(LightmapUV);
#ifdef CALCULATE_BAKED_SHADOWS
    half bakedShadow = BakedShadow(shadowMask, _MainLightOcclusionProbes);
#else
    half bakedShadow = half(1.0);
#endif
#ifdef MAIN_LIGHT_CALCULATE_SHADOWS
    half shadowFade = GetMainLightShadowFade(positionWS);
#else
    half shadowFade = half(1.0);
#endif

    return MixRealtimeAndBakedShadows(ShadowAtten, bakedShadow, shadowFade);

}

float3 shadergraph_RendererBoundsWS_Min()
{
    return GetCameraRelativePositionWS(unity_RendererBounds_Min.xyz);
}

float3 shadergraph_RendererBoundsWS_Max()
{
    return GetCameraRelativePositionWS(unity_RendererBounds_Max.xyz);
}

void shadergraph_URPAmbientOcclusion(float4 screenSpaceUV, float occlusion, out float indirectAO, out float directAO)
{
    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(screenSpaceUV.xy);

    aoFactor.indirectAmbientOcclusion = min(aoFactor.indirectAmbientOcclusion, occlusion);
    indirectAO = aoFactor.indirectAmbientOcclusion;
    directAO = aoFactor.directAmbientOcclusion;
}

float3 shadergraph_URPApproxGGXTerm(float3 normalWS, float roughness, float3 lightDirWS, float3 viewDirWS, float3 specular)
{
    float3 halfDir = SafeNormalize(lightDirWS + viewDirWS);

    float NoH = saturate(dot(normalWS, halfDir));
    half LoH = half(saturate(dot(lightDirWS, halfDir)));

    float Roughness2 = max(roughness * roughness, HALF_MIN);
    float NormalizationTerm = roughness * 4.0 + 2.0;
    float d = NoH * NoH * (Roughness2 - 1.0) + 1.00001f;
    half LoH2 = LoH * LoH;
    half specularTerm = Roughness2 / ((d * d) * max(0.1h, LoH2) * NormalizationTerm);

    #if REAL_IS_HALF
        specularTerm = specularTerm - HALF_MIN;
        specularTerm = clamp(specularTerm, 0.0, 1000.0); // Prevent FP16 overflow on mobiles
    #endif

    return specularTerm * specular;
}

float3 shadergraph_URPBlinnPhongTerm(float3 normalWS, float roughness, float3 lightDirWS, float3 viewDirWS, float3 specular)
{
    float3 halfDir = SafeNormalize(lightDirWS + viewDirWS);

    float NoH = saturate(dot(normalWS, halfDir));
    float smoothness = exp2(10 * (1 - roughness) + 1);
    half normalize = (smoothness + 7) * 0.039789; // bling-phong energy conerv coeff
    return specular * max(pow(NoH, smoothness) * normalize, 0.001);
}
float3 shadergraph_URPCottonWoolTerm(float3 normalWS, float roughness, float3 lightDirWS, float3 viewDirWS, float3 specular)
{
    float3 H = SafeNormalize(lightDirWS + viewDirWS);
    float NdotL = saturate(dot(normalWS, lightDirWS));
    float NdotH = saturate(dot(normalWS, H));
    float D = D_Charlie(NdotH, roughness);
    float Vis = saturate(V_Ashikhmin(NdotL, saturate(dot(viewDirWS, normalWS))));
    float3 F = specular;
    return F * Vis * D;
}
float3 shadergraph_URPSilkTerm(float3 normalWS, float3 tangentWS, float3 bitangentWS, float roughness, float3 lightDirWS, float3 viewDirWS, float3 specular, float anisotropy)
{
    float3 H = SafeNormalize(lightDirWS + viewDirWS);
    float NdotL = saturate(dot(normalWS, lightDirWS));
    float NdotH = saturate(dot(normalWS, H));
    float NdotV = abs(dot(viewDirWS, normalWS));

    half TdotH = dot(tangentWS, H);
    half BdotH = dot(bitangentWS, H);
    half TdotL = dot(tangentWS, lightDirWS);
    half BdotL = dot(bitangentWS, lightDirWS);
    half TdotV = dot(tangentWS, viewDirWS);
    half BdotV = dot(bitangentWS, viewDirWS);
    half VdotH = saturate(dot(viewDirWS, H));

    float roughnessT, roughnessB;
    ConvertAnisotropyToClampRoughness(roughness, anisotropy, roughnessT, roughnessB);
    float DV = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH, NdotV, 
        TdotL, BdotL, NdotL,
        roughnessT, roughnessB, GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV, roughnessT, roughnessB)
        );
    float3 F = F_Schlick(specular, VdotH);
    return max(0, DV) * F;
}
float3 shadergraph_URPAnitrosopyTerm(float3 normalWS, float3 tangentWS, float3 bitangentWS, float roughness, float3 lightDirWS, float3 viewDirWS, float3 specular, float anisotropy)
{
    float3 H = SafeNormalize(lightDirWS + viewDirWS);
    float NdotL = saturate(dot(normalWS, lightDirWS));
    float NdotH = saturate(dot(normalWS, H));
    float NdotV = abs(dot(viewDirWS, normalWS));

    half TdotH = dot(tangentWS, H);
    half BdotH = dot(bitangentWS, H);
    half TdotL = dot(tangentWS, lightDirWS);
    half BdotL = dot(bitangentWS, lightDirWS);
    half TdotV = dot(tangentWS, viewDirWS);
    half BdotV = dot(bitangentWS, viewDirWS);
    half VdotH = saturate(dot(viewDirWS, H));

    float roughnessT, roughnessB;
    GetAnisotropicRoughness(roughness * roughness, anisotropy, roughnessT, roughnessB);
    float DV = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH,
        TdotV, BdotV, NdotV,
        TdotL, BdotL, NdotL,
        roughnessT, roughnessB
        );

    float3 F = F_Schlick(specular, VdotH);
    return max(0, DV) * F;
}

float3 shadergraph_URPLambertianTerm(float3 diffuse)
{
    // multiply by PI in URP to keep consistency
    return diffuse * Lambert() * PI;
}
float3 shadergraph_URPFabricLambertTerm(float3 diffuse, float roughness)
{
    // multiply by PI in URP to keep consistency
    return diffuse * FabricLambert(roughness) * PI;
}
float3 shadergraph_URPDisneyDiffuseTerm(float3 diffuse, float3 normalWS, float3 lightDirWS, float3 viewDirWS, float perceptualRoughness)
{

    float3 H = SafeNormalize(lightDirWS + viewDirWS);
    float LdotH = saturate(dot(lightDirWS, H));
    float NdotL = saturate(dot(normalWS, lightDirWS));
    float NdotV = abs(dot(viewDirWS, normalWS));
    // multiply by PI in URP to keep consistency
    return diffuse * DisneyDiffuse(NdotV, NdotL, LdotH, perceptualRoughness)* PI;  
}
// Always include Shader Graph version
// Always include last to avoid double macros
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"

#endif // UNITY_GRAPHFUNCTIONS_LW_INCLUDED
