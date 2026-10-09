using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Herringbone")]
    class HerringboneNode : CodeFunctionNode
    {
        public HerringboneNode()
        {
            name = "Herringbone";
            synonyms = new string[] { "chevron" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Herringbone", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Herringbone(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 6.0f, 6.0f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.2f, 0, 0, 0)] Vector1 Width,
            [Slot(3, Binding.None, 3.0f, 0, 0, 0)] Vector1 Cells,
            [Slot(4, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)
        {
            return
@"
{
    //Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Calculate integer and decimal parts
    $precision integerPartX = round(scaledUV.x);
    $precision integerPartY = round(scaledUV.y);
    $precision fractionalPartX = scaledUV.x - integerPartX;
    $precision fractionalPartY = scaledUV.y - integerPartY;
    $precision absoluteFractionalX = abs(fractionalPartX);
    $precision absoluteFractionalY = abs(fractionalPartY);

    // Calculate offset
    $precision offsetX = integerPartX - floor(scaledUV.y) - 1;
    $precision offsetY = integerPartY - floor(scaledUV.x);

    // Handle periodicity
    $precision cellsDoubled = Cells * 2;
    offsetX = fmod(offsetX, cellsDoubled);
    offsetY = fmod(offsetY, cellsDoubled);
    offsetX += cellsDoubled;
    offsetY += cellsDoubled;
    offsetX = fmod(offsetX, cellsDoubled);
    offsetY = fmod(offsetY, cellsDoubled);

    // adjust width
    Width *= 0.5;

    // Calculate smoothed absolute value
    $precision smoothedAbsoluteX = smoothstep(Width + 0.05, Width, absoluteFractionalX);
    $precision smoothedAbsoluteY = smoothstep(Width + 0.05, Width, absoluteFractionalY);

    // Calculate cell boundaries
    $precision roundedCells = round(Cells) + 0.5;
    offsetX = step(roundedCells, offsetX);
    offsetY = step(roundedCells, offsetY);

    // Calculate final output
    $precision outputX = smoothedAbsoluteX - offsetX;
    $precision outputY = smoothedAbsoluteY - offsetY;
    Out = 1 - max(outputX, outputY);

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(saturate(Out * 1e7));
#else
    Out = saturate(Out);
#endif
}";
        }
    }
}
