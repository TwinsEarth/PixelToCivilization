#ifndef UNIVERSAL_SURFACE_DATA_INCLUDED
#define UNIVERSAL_SURFACE_DATA_INCLUDED

// Must match Universal ShaderGraph master node
struct SurfaceData
{
    half3 albedo;
    half3 specular;
    half  metallic;
    half  smoothness;
    half3 normalTS;
    half3 emission;
    half  occlusion;
    half  alpha;
    half  clearCoatMask;
    half  clearCoatSmoothness;

#if defined(_CLEARCOAT) && defined(_SCALABLE_LIT)
    half3 clearCoatNormal;
#endif

#if _THIN_FILM
    half thinFilmThickness;
    half thinFilmMask;
#endif

#if _DIFFRACTION_GRATINGS
    half slitsDistance;
    half slitsMask;
    half3 slitsDirection;
#endif

#if _SPECULAR_MODEL_ANISO
    half anisotropy;
    half3 tangentTS;
#endif
#if _CUSTOM_INDIRECT_SPECULAR
    half indirectSpecularMask;
    half3 indirectSpecular;
#endif
#if _CUSTOM_INDIRECT_DIFFUSE
    half indirectDiffuseMask;
    half3 indirectDiffuse;
#endif

#if _USE_PREINTEGRATED_FDG
    half3 customSpecularFDG;
    half customEnergyCompensation;
#endif
};

#endif
