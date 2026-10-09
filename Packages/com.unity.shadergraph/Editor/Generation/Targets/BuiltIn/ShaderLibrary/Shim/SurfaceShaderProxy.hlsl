#ifndef UNITY_SURFACE_SHADER_PROXY_INCLUDED
#define UNITY_SURFACE_SHADER_PROXY_INCLUDED

// We use #undef to avoid redefinition when using instancing.
#undef UNITY_MATRIX_I_M
#define UNITY_MATRIX_I_M   unity_WorldToObject

#endif // UNITY_SURFACE_SHADER_PROXY_INCLUDED
