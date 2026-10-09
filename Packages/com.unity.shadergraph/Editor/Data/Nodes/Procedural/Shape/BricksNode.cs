using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    enum BricksType
    {
        Shelf,
        Luminance,

    };

    [Title("Procedural", "Shape", "Bricks")]
    class BricksNode : CodeFunctionNode
    {
        public BricksNode()
        {
            name = "Bricks";
            synonyms = new string[] { "block", "wall"};
        }


        protected override MethodInfo GetFunctionToConvert()
        {
  
             return GetType().GetMethod("Unity_Bricks_Luminance", BindingFlags.Static | BindingFlags.NonPublic);
              
        }


        static string Unity_Bricks_Luminance(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 3f, 10f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.37f, 0f, 0, 0)] Vector1 Size,
            [Slot(3, Binding.None, 0.5f, 0f, 0, 0)] Vector1 JointX,
            [Slot(4, Binding.None, 0.51f, 0f, 0, 0)] Vector1 JointY,
            [Slot(5, Binding.None, 0.5f, 0, 0, 0)] Vector1 Offset,
            [Slot(6, Binding.None, 0.05f, 0.25f, 0, 0)] Vector2 Luminance,
            [Slot(7, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out,
            [Slot(8, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Shelf)
        {
            return
@"
{
    // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Calculate the adjusted X coordinate
    $precision adjustedX = step(1, fmod(scaledUV.y, 2)) * Offset + scaledUV.x;

    // Build the adjusted coordinate
    $precision2 adjustedCoordinate = $precision2(adjustedX, scaledUV.y);

    // Calculate the random range value
    $precision randomRange = lerp(Luminance.x, Luminance.y, frac(sin(dot(floor(adjustedCoordinate), $precision2(13, 78))) * 5000));

    // Calculate the rectangle width
    $precision rectangleWidth = 1 / Tiling.y * max(sign(Tiling.y - Tiling.x), 0) + JointX + Size;

    // Calculate the rectangle height
    $precision rectangleHeight = -1 / Tiling.x * min(sign(Tiling.y - Tiling.x), 0) + JointY + Size;

    // Calculate the rectangle coordinates
    $precision2 rectangleCoordinates = abs(frac(adjustedCoordinate) * 2 - 1) - $precision2(rectangleWidth, rectangleHeight);
    rectangleCoordinates = 1 - rectangleCoordinates / fwidth(rectangleCoordinates);

    // Calculate the rectangle value
    $precision rectangleValue = saturate(min(rectangleCoordinates.x, rectangleCoordinates.y));

    // Calculate the weighted color
    $precision3 weightedColor = rectangleValue * randomRange;

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(weightedColor * 1e7);
    Shelf = saturate(rectangleValue * 1e7);
#else
    Out = saturate(weightedColor);
    Shelf = saturate(rectangleValue);
#endif
}";
        }

    }
}
