// This structure abstract uv mapping inside one struct.
// It represent a mapping of any uv (with its associated tangent space for derivative if SurfaceGradient mode) - UVSet0 to 4, planar, triplanar

#ifndef __SAMPLEUVMAPPING_HLSL__
#define __SAMPLEUVMAPPING_HLSL__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"

#define UV_MAPPING_UVSET 0
#define UV_MAPPING_PLANAR 1
#define UV_MAPPING_TRIPLANAR 2

struct UVMapping
{
    int mappingType;
    float2 uv;  // Current uv or planar uv
    float4 ddxddy;

    // Triplanar specific
    float2 uvZY;
    float2 uvXZ;
    float2 uvXY;
    float4 ddxddy_zy;
    float4 ddxddy_xy;

    float3 normalWS; // vertex normal
    float3 triplanarWeights;

#ifdef SURFACE_GRADIENT
    // tangent basis to use when mappingType is UV_MAPPING_UVSET
    // these are vertex level in world space
    float3 tangentWS;
    float3 bitangentWS;
    // TODO: store also object normal map for object triplanar
#endif
};

#ifdef UNITY_GPU_DRIVEN_PIPELINE
void CalculateUVMappingDdxddy(FragInputs input,
                             float4 uvMappingMask, inout UVMapping uvMapping, 
                             // scale + global tiling factor (for layered lit only)
                             float2 texScale, float additionalTiling,
                             // parameter for planar/triplanar
                             float worldScale, bool objectSpaceMapping)
{
    float4 scale = (texScale * additionalTiling).xyxy;
    
#if defined(_MAPPING_PLANAR) || defined(_MAPPING_TRIPLANAR) || defined(_EMISSIVE_MAPPING_PLANAR) || defined(_EMISSIVE_MAPPING_TRIPLANAR)
    if (uvMapping.mappingType == UV_MAPPING_PLANAR || uvMapping.mappingType == UV_MAPPING_TRIPLANAR)
    {
        float3 planarDdx = input.planarWorld_ddx;
        float3 planarDdy = input.planarWorld_ddy;
        if (objectSpaceMapping)
        {
            planarDdx = input.planarObject_ddx;
            planarDdy = input.planarObject_ddy;
        }
        uvMapping.ddxddy = float4(planarDdx.xz, planarDdy.xz) * scale * worldScale;
        if (uvMapping.mappingType == UV_MAPPING_TRIPLANAR)
        {
            uvMapping.ddxddy_zy = float4(planarDdx.zy, planarDdy.zy) * scale * worldScale;
            uvMapping.ddxddy_xy = float4(planarDdx.xy, planarDdy.xy) * scale * worldScale;
        }
    }
    else
#endif
    {
        float4 ddxddy = uvMappingMask.x * float4(input.texcoordDDX0.xy, input.texcoordDDY0.xy) +
            uvMappingMask.y * float4(input.texcoordDDX1.xy, input.texcoordDDY1.xy) +
            uvMappingMask.z * float4(input.texcoordDDX2.xy, input.texcoordDDY2.xy) +
            uvMappingMask.w * float4(input.texcoordDDX3.xy, input.texcoordDDY3.xy);
        uvMapping.ddxddy = ddxddy * scale;
    }
}
#endif

// Multiple includes of the file to handle all variations of textures sampling for regular, lod and bias

// Regular sampling functions
#define ADD_FUNC_SUFFIX(Name) Name
#define EXTRA_TYPE_PARAMS
#define SAMPLE_TEXTURE_FUNC_UV(textureName, samplerName) SAMPLE_TEXTURE2D(textureName, samplerName, uvMapping.uv)
#define SAMPLE_TEXTURE_FUNC_UVZY(textureName, samplerName) SAMPLE_TEXTURE2D(textureName, samplerName, uvMapping.uvZY)
#define SAMPLE_TEXTURE_FUNC_UVXZ(textureName, samplerName) SAMPLE_TEXTURE2D(textureName, samplerName, uvMapping.uvXZ)
#define SAMPLE_TEXTURE_FUNC_UVXY(textureName, samplerName) SAMPLE_TEXTURE2D(textureName, samplerName, uvMapping.uvXY)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMappingInternal.hlsl"
#undef ADD_FUNC_SUFFIX
#undef EXTRA_TYPE_PARAMS
#undef SAMPLE_TEXTURE_FUNC_UV
#undef SAMPLE_TEXTURE_FUNC_UVZY
#undef SAMPLE_TEXTURE_FUNC_UVXZ
#undef SAMPLE_TEXTURE_FUNC_UVXY

// Lod sampling functions
#define ADD_FUNC_SUFFIX(Name) MERGE_NAME(Name, Lod)
#define EXTRA_TYPE_PARAMS , real lod
#define SAMPLE_TEXTURE_FUNC_UV(textureName, samplerName) SAMPLE_TEXTURE2D_LOD(textureName, samplerName, uvMapping.uv, lod)
#define SAMPLE_TEXTURE_FUNC_UVZY(textureName, samplerName) SAMPLE_TEXTURE2D_LOD(textureName, samplerName, uvMapping.uvZY, lod)
#define SAMPLE_TEXTURE_FUNC_UVXZ(textureName, samplerName) SAMPLE_TEXTURE2D_LOD(textureName, samplerName, uvMapping.uvXZ, lod)
#define SAMPLE_TEXTURE_FUNC_UVXY(textureName, samplerName) SAMPLE_TEXTURE2D_LOD(textureName, samplerName, uvMapping.uvXY, lod)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMappingInternal.hlsl"
#undef ADD_FUNC_SUFFIX
#undef EXTRA_TYPE_PARAMS
#undef SAMPLE_TEXTURE_FUNC_UV
#undef SAMPLE_TEXTURE_FUNC_UVZY
#undef SAMPLE_TEXTURE_FUNC_UVXZ
#undef SAMPLE_TEXTURE_FUNC_UVXY

// Bias sampling functions
#define ADD_FUNC_SUFFIX(Name) MERGE_NAME(Name, Bias)
#define EXTRA_TYPE_PARAMS , real bias
#define SAMPLE_TEXTURE_FUNC_UV(textureName, samplerName) SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, uvMapping.uv, bias)
#define SAMPLE_TEXTURE_FUNC_UVZY(textureName, samplerName) SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, uvMapping.uvZY, bias)
#define SAMPLE_TEXTURE_FUNC_UVXZ(textureName, samplerName) SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, uvMapping.uvXZ, bias)
#define SAMPLE_TEXTURE_FUNC_UVXY(textureName, samplerName) SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, uvMapping.uvXY, bias)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMappingInternal.hlsl"
#undef ADD_FUNC_SUFFIX
#undef EXTRA_TYPE_PARAMS
#undef SAMPLE_TEXTURE_FUNC_UV
#undef SAMPLE_TEXTURE_FUNC_UVZY
#undef SAMPLE_TEXTURE_FUNC_UVXZ
#undef SAMPLE_TEXTURE_FUNC_UVXY

// Grad sampling functions
#define ADD_FUNC_SUFFIX(Name) MERGE_NAME(Name, Grad)
#define EXTRA_TYPE_PARAMS
#define SAMPLE_TEXTURE_FUNC_UV(textureName, samplerName) SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, uvMapping.uv, uvMapping.ddxddy.xy, uvMapping.ddxddy.zw)
#define SAMPLE_TEXTURE_FUNC_UVZY(textureName, samplerName) SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, uvMapping.uvZY, uvMapping.ddxddy_zy.xy, uvMapping.ddxddy_zy.zw)
#define SAMPLE_TEXTURE_FUNC_UVXZ(textureName, samplerName) SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, uvMapping.uvXZ, uvMapping.ddxddy.xy, uvMapping.ddxddy.zw)
#define SAMPLE_TEXTURE_FUNC_UVXY(textureName, samplerName) SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, uvMapping.uvXY, uvMapping.ddxddy_xy.xy, uvMapping.ddxddy_xy.zw)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMappingInternal.hlsl"
#undef ADD_FUNC_SUFFIX
#undef EXTRA_TYPE_PARAMS
#undef SAMPLE_TEXTURE_FUNC_UV
#undef SAMPLE_TEXTURE_FUNC_UVZY
#undef SAMPLE_TEXTURE_FUNC_UVXZ
#undef SAMPLE_TEXTURE_FUNC_UVXY

// Macro to improve readibility of surface data
#define SAMPLE_UVMAPPING_TEXTURE2D(textureName, samplerName, uvMapping)             SampleUVMapping(TEXTURE2D_ARGS(textureName, samplerName), uvMapping) 
#define SAMPLE_UVMAPPING_TEXTURE2D_LOD(textureName, samplerName, uvMapping, lod)    SampleUVMappingLod(TEXTURE2D_ARGS(textureName, samplerName), uvMapping, lod)
#define SAMPLE_UVMAPPING_TEXTURE2D_BIAS(textureName, samplerName, uvMapping, bias)  SampleUVMappingBias(TEXTURE2D_ARGS(textureName, samplerName), uvMapping, bias)
#define SAMPLE_UVMAPPING_TEXTURE2D_GRAD(textureName, samplerName, uvMapping)     SampleUVMappingGrad(TEXTURE2D_ARGS(textureName, samplerName), uvMapping)

#define SAMPLE_UVMAPPING_NORMALMAP(textureName, samplerName, uvMapping, scale)              SampleUVMappingNormal(TEXTURE2D_ARGS(textureName, samplerName), uvMapping, scale)
#define SAMPLE_UVMAPPING_NORMALMAP_LOD(textureName, samplerName, uvMapping, scale, lod)     SampleUVMappingNormalLod(TEXTURE2D_ARGS(textureName, samplerName), uvMapping, scale, lod)
#define SAMPLE_UVMAPPING_NORMALMAP_BIAS(textureName, samplerName, uvMapping, scale, bias)   SampleUVMappingNormalBias(TEXTURE2D_ARGS(textureName, samplerName), uvMapping, scale, bias)
#define SAMPLE_UVMAPPING_NORMALMAP_GRAD(textureName, samplerName, uvMapping, scale)     SampleUVMappingNormalGrad(TEXTURE2D_ARGS(textureName, samplerName), uvMapping, scale)

#define SAMPLE_UVMAPPING_NORMALMAP_AG(textureName, samplerName, uvMapping, scale)              SampleUVMappingNormalAG(TEXTURE2D_ARGS(textureName, samplerName), uvMapping, scale)
#define SAMPLE_UVMAPPING_NORMALMAP_AG_LOD(textureName, samplerName, uvMapping, scale, lod)     SampleUVMappingNormalAGLod(TEXTURE2D_ARGS(textureName, samplerName), uvMapping, scale, lod)
#define SAMPLE_UVMAPPING_NORMALMAP_AG_BIAS(textureName, samplerName, uvMapping, scale, bias)   SampleUVMappingNormalAGBias(TEXTURE2D_ARGS(textureName, samplerName), uvMapping, scale, bias)
#define SAMPLE_UVMAPPING_NORMALMAP_AG_GRAD(textureName, samplerName, uvMapping, scale)     SampleUVMappingNormalAGGrad(TEXTURE2D_ARGS(textureName, samplerName), uvMapping, scale)

#define SAMPLE_UVMAPPING_NORMALMAP_RGB(textureName, samplerName, uvMapping, scale)              SampleUVMappingNormalRGB(TEXTURE2D_ARGS(textureName, samplerName), uvMapping, scale)
#define SAMPLE_UVMAPPING_NORMALMAP_RGB_LOD(textureName, samplerName, uvMapping, scale, lod)     SampleUVMappingNormalRGBLod(TEXTURE2D_ARGS(textureName, samplerName), uvMapping, scale, lod)
#define SAMPLE_UVMAPPING_NORMALMAP_RGB_BIAS(textureName, samplerName, uvMapping, scale, bias)   SampleUVMappingNormalRGBBias(TEXTURE2D_ARGS(textureName, samplerName), uvMapping, scale, bias)
#define SAMPLE_UVMAPPING_NORMALMAP_RGB_GRAD(textureName, samplerName, uvMapping, scale)     SampleUVMappingNormalRGBGrad(TEXTURE2D_ARGS(textureName, samplerName), uvMapping, scale)

#endif //__SAMPLEUVMAPPING_HLSL__
