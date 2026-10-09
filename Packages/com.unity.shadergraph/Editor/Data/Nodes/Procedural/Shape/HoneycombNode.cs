using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Honeycomb")]
    class HoneycombNode : CodeFunctionNode
    {
        public HoneycombNode()
        {
            name = "Honeycomb";
            synonyms = new string[] { "beehive" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Honeycomb", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Honeycomb(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 10f, 10f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 1f, 0.0f, 0, 0)] Vector1 Scale,
            [Slot(3, Binding.None, 0.2f, 0.0f, 0, 0)] Vector1 EdgeWidth,
            [Slot(4, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)

        {
            return
@"
{
    // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;
    // Adjust Y coordinate
    scaledUV.y= (floor(scaledUV.x*1.5)%2)*0.5+scaledUV.y;
    // Adjust X coordinate
    scaledUV.x*=1.5;
    // Calculate offset from center
    $precision2 uvOffsetFromCenter= abs((scaledUV%$precision2(1,1))-$precision2(0.5,0.5));
    // Calculate honeycomb pattern
    $precision honeycomb = smoothstep(0,EdgeWidth,abs(max(uvOffsetFromCenter.x*1.5+uvOffsetFromCenter.y,uvOffsetFromCenter.y*2)-Scale)*2);
#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(honeycomb * 1e7);
#else
    Out = saturate(honeycomb);
#endif

}";
        }
    }
}
