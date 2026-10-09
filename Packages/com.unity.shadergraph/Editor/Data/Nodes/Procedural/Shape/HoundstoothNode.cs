using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Houndstooth")]
    class HoundstoothNode : CodeFunctionNode
    {
        public HoundstoothNode()
        {
            name = "Houndstooth";
            synonyms = new string[] { "houndstooth" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Houndstooth", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Houndstooth(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 10f, 10f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 1f, 0.0f, 0, 0)] Vector1 Teeth,
            [Slot(3, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)

        {
            return
@"
{

    // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Calculate Z component
    $precision zComponent = (scaledUV.x + scaledUV.y) * Teeth;

    // Calculate the XYZ components after the frac operation
    $precision3 fractionalXYZ = frac($precision3(scaledUV.x, scaledUV.y, zComponent));

    // Calculate smooth step value
    $precision3 stepXY = smoothstep(0.5, 0.55, fractionalXYZ);
    $precision3 stepZW = smoothstep(0.95, 1, fractionalXYZ);

    // Calculate houndstooth pattern
    $precision3 houndstoothPattern = stepXY - stepZW;

    // Calculate final output
    Out = lerp(houndstoothPattern.x, houndstoothPattern.y, houndstoothPattern.z);

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(Out * 1e7);
#else
    Out = saturate(Out);
#endif

}";
        }
    }
}
