using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Grid")]
    class GridNode : CodeFunctionNode
    {
        public GridNode()
        {
            name = "Grid";
            synonyms = new string[] { "lattice" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Grid", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Grid(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 6.0f, 0, 0, 0)] Vector1 Frequency,
            [Slot(2, Binding.None, 0.0f, 0, 0, 0)] Vector1 Offset,
            [Slot(3, Binding.None, 0.8f, 0, 0, 0)] Vector1 WidthX,
            [Slot(4, Binding.None, 0.8f, 0, 0, 0)] Vector1 WidthY,
            [Slot(5, Binding.None, 45.0f, 0, 0, 0)] Vector1 Rotation,
            [Slot(6, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)
        {
            return
@"
{
    $precision2 Center = $precision2(0.5,0.5);
    // Set UV coordinates to center
    $precision2 centeredUV = UV - Center;

    // Calculate the radian value of the rotation angle
    $precision rotationRadians = radians(Rotation);
    $precision sineValue = sin(rotationRadians);
    $precision cosineValue = cos(rotationRadians);

    //Construct rotation matrix
    $precision2x2 rotationMatrix = $precision2x2(cosineValue, -sineValue, sineValue, cosineValue);

    //Apply rotation matrix
    $precision2 rotatedUV = mul(centeredUV, rotationMatrix);

    // Restore UV coordinates to original position
    $precision2 finalUV = rotatedUV + Center;

    // Calculate pattern
    $precision2 fractionalPattern = abs(frac(finalUV * Frequency + Offset) * 2 - 1) - $precision2(WidthX,WidthY);
    fractionalPattern = 1 - fractionalPattern / fwidth(fractionalPattern);
    $precision patternResult = saturate(min(fractionalPattern.x, fractionalPattern.y));

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(patternResult * 1e7);
#else
    Out = saturate(patternResult);
#endif
}";
        }
    }
}
