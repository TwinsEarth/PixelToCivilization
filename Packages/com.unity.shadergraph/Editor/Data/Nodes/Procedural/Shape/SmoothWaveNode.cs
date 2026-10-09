using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{

    [Title("Procedural", "Shape", "SmoothWave")]
    class SmoothWaveNode : CodeFunctionNode
    {
        public SmoothWaveNode()
        {
            name = "SmoothWave";
            synonyms = new string[] { "sin","cos" };
        }

        [SerializeField]
        private AntiAliasType m_AntiAliasType = AntiAliasType.Fastest;

        [EnumControl("Anti Aliasing")]
        public AntiAliasType clampType
        {
            get { return m_AntiAliasType; }
            set
            {
                if (m_AntiAliasType == value)
                    return;

                m_AntiAliasType = value;
                Dirty(ModificationScope.Graph); 
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (clampType)
            {
                case AntiAliasType.Smooth:
                    return GetType().GetMethod("Unity_SmoothWave_Smooth", BindingFlags.Static | BindingFlags.NonPublic);
                case AntiAliasType.Derivative:
                    return GetType().GetMethod("Unity_SmoothWave_Derivative", BindingFlags.Static | BindingFlags.NonPublic);
                case AntiAliasType.Fastest:
                default:
                    return GetType().GetMethod("Unity_SmoothWave_Fastest", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_SmoothWave_Fastest(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 2f, 0, 0, 0)] Vector1 Wavelength,
            [Slot(3, Binding.None, 0.25f, 0, 0, 0)] Vector1 Amplitude,
            [Slot(4, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(5, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
    // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Calculate the X component of the sine wave
    $precision sineComponent = sin(scaledUV.x * Wavelength * 6.28318530) * 0.5 + 0.5;
 
    // Calculate Y component
    $precision yComponent = scaledUV.y / Amplitude;

    // Calculate waveform difference
    $precision waveDifference = abs(yComponent - sineComponent - round(yComponent - sineComponent)) * 2;

    // Calculate smooth waveform
    $precision smoothWave = step(Width, waveDifference);

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(smoothWave * 1e7);
#else
    Out = saturate(smoothWave);
#endif
}";
        }

        static string Unity_SmoothWave_Smooth(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Vector1 Wavelength,
            [Slot(3, Binding.None, 0.5f, 0, 0, 0)] Vector1 Amplitude,
            [Slot(4, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(5, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
    // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Calculate the X component of the sine wave
    $precision waveX = sin(scaledUV.x * Wavelength * 6.28318530) * 0.5 + 0.5;

    // Calculate Y component
    $precision waveY = scaledUV.y / Amplitude;

    // Calculate smooth waveform
    $precision waveDifference = abs(waveY - waveX - round(waveY - waveX)) * 2;
    $precision smoothWave = smoothstep(Width, Width + 0.1, waveDifference);

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(smoothWave * 1e7);
#else
    Out = saturate(smoothWave);
#endif

}";
        }

        static string Unity_SmoothWave_Derivative(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Vector1 Wavelength,
            [Slot(3, Binding.None, 0.5f, 0, 0, 0)] Vector1 Amplitude,
            [Slot(4, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(5, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
    // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Calculate the X component of the sine wave
    $precision waveU = sin(scaledUV.x * Wavelength * 6.28318530) * 0.5 + 0.5;

    // Calculate Y component
    $precision waveV = scaledUV.y / Amplitude;

    // Calculate smooth waveform
    $precision smoothWave = abs(waveV - waveU - round(waveV - waveU)) * 2 - Width;
    smoothWave = saturate(smoothWave / fwidth(smoothWave));

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(smoothWave * 1e7);
#else
    Out = saturate(smoothWave);
#endif

}";
        }
    }
}
