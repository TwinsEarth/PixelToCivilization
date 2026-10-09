// using System.Reflection;
// using UnityEngine;

// namespace UnityEditor.ShaderGraph
// {
//     [Title("Input", "Lighting", "Global Illumination")]
//     class GlobaIlluminationNode : CodeFunctionNode
//     {
//         public GlobaIlluminationNode()
//         {
//             name = "Global Illumination";
//             synonyms = new string[] { "GI", "environment","light" };
//         }

//         public override bool hasPreview { get { return false; } }

//         protected override MethodInfo GetFunctionToConvert()
//         {
//             return GetType().GetMethod("GlobaIllumination", BindingFlags.Static | BindingFlags.NonPublic);
//         }

//         static string GlobaIllumination(

//             [Slot(0, Binding.None)]  Vector3 Albedo,
//             [Slot(1, Binding.None)] Vector1 Metallic,
//             [Slot(2, Binding.None)] Vector3 Specular,
//             [Slot(3, Binding.None)] Vector1 Smoothness,
//             [Slot(4, Binding.None)] Vector1 ClearCoatMask,
//             [Slot(5, Binding.None)] Vector1 ClearCoatSmoothness,
//             [Slot(6, Binding.None)] Vector4 ShadowMask,
//             [Slot(7, Binding.None)] Vector3 BakeGI,
//             [Slot(8, Binding.None)] Vector1 Occlusion,
//             [Slot(9, Binding.WorldSpacePosition)] Vector3 PositionWS,
//             [Slot(10, Binding.WorldSpaceNormal)] Vector3 NormalWS,
//             [Slot(11, Binding.WorldSpaceViewDirection)] Vector3 ViewDirectionWS,
//             [Slot(12, Binding.None)] out Vector3 Out
                  
//             )
//         {
//             Out = Vector3.one;

//             return
// @"
// {
//  #ifdef SHADERGRAPH_PREVIEW
//         half roughness=1- Smoothness;
//         half roughness2= roughness*roughness;
//         half3 NormalOS= TransformWorldToObject(NormalWS);
//         half3 reflectVec = reflect(-ViewDirectionWS, NormalOS);
//         half3 indirectDiffuse = BakeGI;
//     #if !defined(_ENVIRONMENTREFLECTIONS_OFF)
//         float mip = sqrt(roughness) * (1.7 - 0.7 * sqrt(roughness));
//         mip *=6;
//         half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVec, mip);
//         half3 irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
//     #endif // GLOSSY_REFLECTIONS
//         Specular=lerp(half3(0.04,0.04,0.04), Albedo, Metallic);
//         half3 indirectSpecular =irradiance * Occlusion; // GlossyEnvironmentReflection
//         half NoV = saturate(dot(NormalWS, ViewDirectionWS));
//         half fresnelTerm = Pow4(1.0 - NoV);
//         half surfaceReduction = 1.0 / (roughness2 + 1.0);
//         half grazingTerm=saturate(Specular.r + Smoothness);
//         surfaceReduction =surfaceReduction* lerp(Specular, grazingTerm, fresnelTerm);
//         Out = indirectDiffuse * Albedo;
//         Out += indirectSpecular * surfaceReduction*Occlusion;
//     #else
//         Out = SHADERGRAPH_GLOBALILLUMINATION(Albedo, Metallic, Specular, Smoothness, ClearCoatMask, ClearCoatSmoothness,ShadowMask,BakeGI,Occlusion,PositionWS,NormalWS,ViewDirectionWS);
//     #endif
// }
// ";
//         }
//     }
// }
