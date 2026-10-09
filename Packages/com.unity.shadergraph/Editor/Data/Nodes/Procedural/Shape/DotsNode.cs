using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Dots")]
    class DotsNode : CodeFunctionNode
    {
        public DotsNode()
        {
            name = "Dots";
            synonyms = new string[] { "circle" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Dots", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Dots(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 10f, 10f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.5f, 0.0f, 0, 0)] Vector1 OffsetX,
            [Slot(3, Binding.None, 0.5f, 0.0f, 0, 0)] Vector1 OffsetY,
            [Slot(4, Binding.None, 1f, 0.0f, 0, 0)] Vector1 Size,
            [Slot(5, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)

        {
            return
@"
{
    // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Calculate the adjusted X coordinate based on the y component of scaledUV and OffsetX
    $precision adjustedX = step(1, fmod(scaledUV.y, 2)) * OffsetX + scaledUV.x;

    // Calculate the adjusted Y coordinate based on the x component of scaledUV and OffsetY
    $precision adjustedY = step(1, fmod(scaledUV.x, 2)) * OffsetY + scaledUV.y;

    // Create a new UV coordinate using the adjusted X and Y values
    $precision2 adjustedUV = $precision2(adjustedX, adjustedY);

    // Get the fractional part of the adjusted UV coordinates
    $precision2 fractionalUV = frac(adjustedUV);

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate((1.0 - length((fractionalUV * 2 - 1) / $precision2(Size, Size))) * 1e7);
#else
    // Calculate the distance from the center of the UV coordinates
    $precision distanceFromCenter = length((fractionalUV * 2 - 1) / $precision2(Size, Size));
    Out = saturate((1 - distanceFromCenter) / fwidth(distanceFromCenter));
#endif

}";
        }
    }
}
