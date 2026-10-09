#ifndef UNITY_GRAPHFUNCTIONS_LW_INCLUDED
#define UNITY_GRAPHFUNCTIONS_LW_INCLUDED

#define SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv) shadergraph_LWSampleSceneDepth(uv)
#define SHADERGRAPH_SAMPLE_SCENE_COLOR(uv) shadergraph_LWSampleSceneColor(uv)
#define SHADERGRAPH_BAKED_GI(positionWS, normalWS, positionSS, uvStaticLightmap, uvDynamicLightmap, applyScaling) shadergraph_LWBakedGI(positionWS, normalWS, positionSS, uvStaticLightmap, uvDynamicLightmap, applyScaling)
#define SHADERGRAPH_REFLECTION_PROBE(viewDir, normalOS, lod) shadergraph_LWReflectionProbe(viewDir, normalOS, lod)
#define SHADERGRAPH_FOG(position, color, density) shadergraph_LWFog(position, color, density)
#define SHADERGRAPH_AMBIENT_SKY unity_AmbientSky
#define SHADERGRAPH_AMBIENT_EQUATOR unity_AmbientEquator
#define SHADERGRAPH_AMBIENT_GROUND unity_AmbientGround
#define SHADERGRAPH_MAIN_LIGHT_DIRECTION shadergraph_MainLightDirection
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
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/DeclareDepthTexture.hlsl"
#endif

#if defined(REQUIRE_OPAQUE_TEXTURE)
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/DeclareOpaqueTexture.hlsl"
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

float3 shadergraph_LWBakedGI(float3 positionWS, float3 normalWS, uint2 positionSS, float2 uvStaticLightmap, float2 uvDynamicLightmap, bool applyScaling)
{
#ifdef LIGHTMAP_ON
    if (applyScaling)
        uvStaticLightmap = uvStaticLightmap * unity_LightmapST.xy + unity_LightmapST.zw;

    return SampleLightmap(uvStaticLightmap, normalWS);
#else
    return SampleSH(normalWS);
#endif
}

float3 shadergraph_LWReflectionProbe(float3 viewDir, float3 normalOS, float lod)
{
    float3 reflectVec = reflect(-viewDir, normalOS);
    return DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVec, lod), unity_SpecCube0_HDR);
}

void shadergraph_LWFog(float3 position, out float4 color, out float density)
{
    color = unity_FogColor;
    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    // ComputeFogFactor returns the fog density (0 for no fog and 1 for full fog).
    density = ComputeFogFactor(TransformObjectToHClip(position).z);
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

float3 shadergraph_MainLightDirection()
{
    return -_WorldSpaceLightPos0.xyz * (1.0 - _WorldSpaceLightPos0.w);
}

float3 shadergraph_RendererBoundsWS_Min()
{
    return 0.0f;
}

float3 shadergraph_RendererBoundsWS_Max()
{
    return 0.0f;
}
void shadergraph_URPAmbientOcclusion(float2 screenSpaceUV, float occlusion, out float indirectAO, out float directAO)
{
    indirectAO = 1;
    directAO = 1;
}

float3 shadergraph_URPApproxGGXTerm(float3 normalWS, float roughness, float3 lightDirWS, float3 viewDirWS, float3 specular)
{
    return 0.0f;
}
float3 shadergraph_URPBlinnPhongTerm(float3 normalWS, float roughness, float3 lightDirWS, float3 viewDirWS, float3 specular)
{
    return 0.0f;
}
float3 shadergraph_URPCottonWoolTerm(float3 normalWS, float roughness, float3 lightDirWS, float3 viewDirWS, float3 specular)
{
    return 0.0f;
}
float3 shadergraph_URPSilkTerm(float3 normalWS, float3 tangentWS, float3 bitangentWS, float roughness, float3 lightDirWS, float3 viewDirWS, float3 specular, float anisotropy)
{
    return 0.0f;
}
float3 shadergraph_URPAnitrosopyTerm(float3 normalWS, float3 tangentWS, float3 bitangentWS, float roughness, float3 lightDirWS, float3 viewDirWS, float3 specular, float anisotropy)
{
    return 0.0f;
}
float3 shadergraph_URPLambertianTerm(float3 diffuse)
{
    return 0.0f;
}
float3 shadergraph_URPFabricLambertTerm(float3 diffuse, float roughness)
{
    return 0.0f;
}
float3 shadergraph_URPDisneyDiffuseTerm(float3 diffuse, float3 normalWS, float3 lightDirWS, float3 viewDirWS, float perceptualRoughness)
{
    return 0.0f;
}

// Always include Shader Graph version
// Always include last to avoid double macros
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"

#endif // UNITY_GRAPHFUNCTIONS_LW_INCLUDED
