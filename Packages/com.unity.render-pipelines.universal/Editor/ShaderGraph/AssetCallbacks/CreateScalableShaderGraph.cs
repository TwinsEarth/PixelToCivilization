using System;
using UnityEditor.ShaderGraph;
using UnityEditor.Rendering.Universal.Analytics;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class CreateScalableShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/URP/Scalable Lit Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateScalableLitGraph()
        {
            var target = (UniversalTarget)Activator.CreateInstance(typeof(UniversalTarget));
            target.TrySetActiveSubTarget(typeof(UniversalScalableLitSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Occlusion,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
            ShaderGraphGUIAnalytics.SendShaderGraphCreate("URPScalableLitMenuCreation", target.displayName);
        }
    }
}
