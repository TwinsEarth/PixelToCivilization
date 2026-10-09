using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Stripes")]
    class StripesNode : CodeFunctionNode
    {
        public StripesNode()
        {
            name = "Stripes";
            synonyms = new string[] { "streak", "striae" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Stripes", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Stripes(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 6.0f, 0, 0, 0)] Vector1 Frequency,
            [Slot(2, Binding.None, 0.0f, 0, 0, 0)] Vector1 Offset,
            [Slot(3, Binding.None, 0.5f, 0, 0, 0)] Vector1 Thickness,
            [Slot(4, Binding.None, 45.0f, 0, 0, 0)] Vector1 Rotation,
            [Slot(5, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)
        {
            return
@"
{
    //Define center point
    $precision2 centerPoint = $precision2(0.5, 0.5);

    //  Offset UV coordinates relative to the center point
    UV -= centerPoint;

    //  Calculate the radian value of the rotation angle
    $precision rotationRadians = radians(Rotation);

    // Calculate sine and cosine values
    $precision sineValue = sin(rotationRadians);
    $precision cosineValue = cos(rotationRadians);

    // Build rotation matrix
    $precision2x2 rotationMatrix = $precision2x2(cosineValue, -sineValue, sineValue, cosineValue);

    // Apply rotation matrix to UV coordinates
    UV.xy = mul(UV.xy, rotationMatrix);

    // Restore UV coordinates to original position
    UV += centerPoint;

    // Define height
    $precision heightValue = 0.5;

    // Calculate fractal patterns
    $precision2 fractalPattern = abs(frac(UV * $precision2(Frequency, 1) + $precision2(Offset, 0)) * 2 - 1) - $precision2(Thickness, heightValue);
    fractalPattern = 1 - fractalPattern / fwidth(fractalPattern);

    // Calculate the final result
    $precision stripes = saturate(min(fractalPattern.x, fractalPattern.y));

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(stripes * 1e7);
#else
    Out = saturate(stripes);
#endif


}";
        }
    }
}
