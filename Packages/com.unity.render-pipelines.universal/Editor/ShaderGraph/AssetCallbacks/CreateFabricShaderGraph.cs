using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.Rendering.Universal.Analytics;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class CreateFabricShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/URP/Fabric Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateFabricGraph()
        {
            var target = (UniversalTarget)Activator.CreateInstance(typeof(UniversalTarget));
            target.TrySetActiveSubTarget(typeof(UniversalFabricSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.Specular,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Occlusion,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
            ShaderGraphGUIAnalytics.SendShaderGraphCreate("URPFabricMenuCreation", target.displayName);
        }
    }
}
