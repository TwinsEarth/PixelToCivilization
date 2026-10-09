using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Truchet")]
    class TruchetNode : CodeFunctionNode
    {
        public TruchetNode()
        {
            name = "Truchet";
            synonyms = new string[] { "truchet" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Truchet", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Truchet(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 6.0f, 6.0f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 5f, 0, 0, 0)] Vector1 Repetition,
            [Slot(3, Binding.None, 0.8f, 0, 0, 0)] Vector1 LineWidth,
            [Slot(4, Binding.None, 0.15f, 0, 0, 0)] Vector1 LineEdge,
            [Slot(5, Binding.None, 10f, 0, 0, 0)] Vector1 Seed,
            [Slot(6, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)
        {
            return
@"
{
    // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Calculate the X component of the pattern based on the seed
    $precision patternX = sign(cos(length(ceil(scaledUV)) * Seed)) * scaledUV.x;

    // Calculate fractional UV pattern Y component
    $precision2 fractionalPatternY = frac($precision2(patternX, scaledUV.y));

    // Calculate smooth repeating pattern
    $precision smoothRepeat = frac(min(length(fractionalPatternY), length(1 - fractionalPatternY)) * Repetition);

    // Calculate the minimum and maximum values ​​​​of the line
    $precision lineMin = LineWidth + LineEdge;
    $precision lineMax = LineWidth - LineEdge;
    $precision oneMinusLineMin = (1 - LineWidth) + LineEdge;
    $precision oneMinusLineMax = (1 - LineWidth) - LineEdge;

    // Calculate the output X and Y components
    $precision outX = smoothstep(lineMin, lineMax, smoothRepeat);
    $precision outY = smoothstep(oneMinusLineMin, oneMinusLineMax, smoothRepeat);

    // Calculate final output
    Out = outX - outY;

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(Out * 1e7);
#else
    Out = saturate(Out);
#endif

}";
        }
    }
}
