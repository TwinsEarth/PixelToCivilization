// using System.Reflection;
// using UnityEngine;

// namespace UnityEditor.ShaderGraph
// {
//     [Title("Input", "Lighting", "Main Lighting")]
//     class MainLightingNode : CodeFunctionNode
//     {
//         public MainLightingNode()
//         {
//             name = "Main Lighting";
//             synonyms = new string[] { "sun" };
//         }

//         public override bool hasPreview { get { return false; } }

//         protected override MethodInfo GetFunctionToConvert()
//         {
//             return GetType().GetMethod("MainLighting", BindingFlags.Static | BindingFlags.NonPublic);
//         }

//         static string MainLighting(

//             [Slot(0, Binding.None)] Vector3 Albedo,
//             [Slot(1, Binding.None)] Vector1 Metallic,
//             [Slot(2, Binding.None)] Vector3 Specular,
//             [Slot(3, Binding.None)] Vector1 Smoothness,
//             [Slot(4, Binding.None)] Vector1 Occlusion,
//             [Slot(5, Binding.None)] Vector1 Alpha,
//             [Slot(6, Binding.None)] Vector4 ShadowMask,
//             [Slot(7, Binding.WorldSpaceNormal)]  Vector3 NormalWS,
//             [Slot(8, Binding.WorldSpacePosition)] Vector3 PositionWS,
//             [Slot(9, Binding.WorldSpaceViewDirection)] Vector3 ViewDirectionWS,
//             [Slot(10, Binding.None)] Vector1 ClearCoatMask,
//             [Slot(11, Binding.None)] Vector1 ClearCoatSmoothness,
//             [Slot(12, Binding.None)] Boolean SpecularHighlightsOff,
//             [Slot(13, Binding.None)] out Vector3 Out
//             )
//         {

//             Out = Vector3.one;
//             return
// @"
// {
// 	#ifdef SHADERGRAPH_PREVIEW
//     half3 lightDirectionWS= -half3(-0.5, -0.5, 0.8);
//     half3 halfDir = normalize(lightDirectionWS + ViewDirectionWS);
//     half roughness=1- Smoothness;
//          roughness=roughness*roughness;
//     half NoH = saturate(dot(NormalWS, halfDir));
//     half LoH = half(saturate(dot(lightDirectionWS, halfDir)));
//     half d = NoH * NoH * (roughness * roughness - 1) + 1.00001f;
//     half LoH2 = LoH * LoH;
//     half specularTerm = (roughness * roughness) / ((d * d) * max(0.1h, LoH2) * (roughness * 4 + 2));
//     half Ds=float3(0.04,0.04,0.04);
// 		 half NdotL = saturate(dot(NormalWS,lightDirectionWS));
//          Out=  NdotL*(specularTerm*lerp(Ds,Albedo,Metallic)+(1-Metallic)*Albedo);
// 	#else
//          Out = SHADERGRAPH_MAINLIGHTING(Albedo,Metallic,Specular,Smoothness,Occlusion,Alpha,ShadowMask,NormalWS,PositionWS,ViewDirectionWS,ClearCoatMask,ClearCoatSmoothness,SpecularHighlightsOff);
//     #endif

// }
// ";
//         }
//     }
// }
