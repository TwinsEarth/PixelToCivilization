using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Bacteria")]
    class BacteriaNode : CodeFunctionNode
    {
        public BacteriaNode()
        {
            name = "Bacteria";
            synonyms = new string[] { "germ", "microbe" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Bacteria", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Bacteria(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 10f, 10f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.5f, 0.0f, 0, 0)] Vector1 Seed,
            [Slot(3, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)

        {
            return
@"
{
    // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Get the fractional part of UV
    $precision2 fractionalPosition = frac(scaledUV);

    // Get the integer part of UV
    $precision2 integerPosition = floor(scaledUV);

    // Calculate the integer part after scaling by seed
    $precision2 seedIntegerPosition = floor(integerPosition * Seed);

    // Calculate the fractional part after scaling by seed
    $precision2 seedFractionalPosition = frac(integerPosition * Seed);

    // Calculate the smooth interpolation factor
    $precision2 smoothInterpolation = seedFractionalPosition * seedFractionalPosition * (3.0 - 2.0 * seedFractionalPosition);

    // Generate noise value A
    $precision3 noiseValueA = frac($precision3(seedIntegerPosition.xyx + $precision2(0.0, 0.0).xyx) * 0.1031);
    noiseValueA += dot(noiseValueA, noiseValueA.yzx + 33.33);
    $precision noiseA = frac((noiseValueA.x + noiseValueA.y) * noiseValueA.z);

    // Generate noise value B
    $precision3 noiseValueB = frac($precision3(seedIntegerPosition.xyx + $precision2(1.0, 0.0).xyx) * 0.1031);
    noiseValueB += dot(noiseValueB, noiseValueB.yzx + 33.33);
    float noiseB = frac((noiseValueB.x + noiseValueB.y) * noiseValueB.z);

    // Generate noise value C
    $precision3 noiseValueC = frac($precision3(seedIntegerPosition.xyx + $precision2(0.0, 1.0).xyx) * 0.1031);
    noiseValueC += dot(noiseValueC, noiseValueC.yzx + 33.33);
    $precision noiseC = frac((noiseValueC.x + noiseValueC.y) * noiseValueC.z);

    // Generate noise value D
    $precision3 noiseValueD = frac($precision3(seedIntegerPosition.xyx + $precision2(1.0, 1.0).xyx) * 0.1031);
    noiseValueD += dot(noiseValueD, noiseValueD.yzx + 33.33);
    float noiseD = frac((noiseValueD.x + noiseValueD.y) * noiseValueD.z);

    // Calculate interpolation coefficients
    $precision k0 = noiseA;
    $precision k1 = noiseB - noiseA;
    $precision k2 = noiseC - noiseA;
    $precision k4 = noiseA - noiseB - noiseC + noiseD;

    // Calculate the final noise value
    $precision noiseValue = k0 + k1 * smoothInterpolation.x + k2 * smoothInterpolation.y + k4 * smoothInterpolation.x * smoothInterpolation.y;
    noiseValue = frac(noiseValue);

    // Calculate step thresholds
    $precision step1 = step(0.75, noiseValue);
    $precision step2 = step(0.5, noiseValue);
    $precision step3 = step(0.25, noiseValue);

    // Calculate mixing steps
    $precision2 mixStep1 = (1 - fractionalPosition) * step1;
    $precision2 mixStep2 = (step2 - step1) * (1 - $precision2(1 - fractionalPosition.x, fractionalPosition.y));
    $precision2 mixStep3 = (step3 - step2) * $precision2(1 - fractionalPosition.x, fractionalPosition.y);
    $precision2 mixStep4 = (1 - step3) * fractionalPosition;

    // Calculate the mixed result
    $precision2 mixedSteps = mixStep1 + mixStep2 + mixStep3 + mixStep4;

    // Calculate the length of the mixed result
    $precision lengthMixedSteps = length(mixedSteps);

    // Calculate the body part
    $precision bodyPart = (smoothstep(0.45, 0.4, lengthMixedSteps) - smoothstep(0.35, 0.3, lengthMixedSteps)) + (smoothstep(0.7, 0.65, lengthMixedSteps) - smoothstep(0.6, 0.55, lengthMixedSteps));

    // Calculate the corrected lengths
    $precision lengthMixedStepsFix1 = length(mixedSteps - $precision2(0.5, 1));
    $precision lengthMixedStepsFix2 = length(mixedSteps - $precision2(1, 0.5));

    // Calculate the head and tail parts
    $precision headTailPart = (smoothstep(0.2, 0.15, lengthMixedStepsFix1) - smoothstep(0.1, 0.05, lengthMixedStepsFix1)) + (smoothstep(0.2, 0.15, lengthMixedStepsFix2) - smoothstep(0.1, 0.05, lengthMixedStepsFix2));

    // Calculate the final output
    $precision bacteria = bodyPart + headTailPart;

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(bacteria * 1e7);
#else
    Out = saturate(bacteria);
#endif

}";
        }
    }
}
