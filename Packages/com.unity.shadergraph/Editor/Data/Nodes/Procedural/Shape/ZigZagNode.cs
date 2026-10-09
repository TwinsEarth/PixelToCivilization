using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{


    [Title("Procedural", "Shape", "ZigZag")]
    class ZigZagNode : CodeFunctionNode
    {
        public ZigZagNode()
        {
            name = "ZigZag";
            synonyms = new string[] { "zigzag" };
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
                    return GetType().GetMethod("Unity_ZigZag_Smooth", BindingFlags.Static | BindingFlags.NonPublic);
                case AntiAliasType.Derivative:
                    return GetType().GetMethod("Unity_ZigZag_Derivative", BindingFlags.Static | BindingFlags.NonPublic);
                case AntiAliasType.Fastest:
                default:
                    return GetType().GetMethod("Unity_ZigZag_Fastest", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_ZigZag_Fastest(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.5f, 0f, 0, 0)] Vector1 Width,
            [Slot(3, Binding.None, 0.5f, 0, 0, 0)] Vector1 Wavelength,
            [Slot(4, Binding.None, 0.5f, 0, 0, 0)] Vector1 Amplitude,
            [Slot(5, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
    // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Calculate the scaling value in the Y direction (relative to the amplitude)
    $precision scaledY = scaledUV.y / Amplitude;

    // Calculate the scaling value in the X direction (relative to the wavelength)
    $precision scaledX = scaledUV.x / Wavelength;

    // Calculate periodic changes in the X direction
    $precision periodicX = abs((scaledX - floor(scaledX + 0.5)) * 2) * 2 - 1;

    // Calculate waveform value
    $precision waveValue = scaledY - periodicX;

    //Apply step function and width limit
    $precision steppedWave = step(clamp(Width, 0.0001, 0.9999), abs(waveValue - round(waveValue)) * 2);

#if defined(SHADER_STAGE_RAY_TRACING)

    Out= saturate(steppedWave * 1e7);
#else
    Out = saturate(steppedWave);
#endif
}";
        }

        static string Unity_ZigZag_Smooth(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.5f, 0f, 0, 0)] Vector1 Width,
            [Slot(3, Binding.None, 0.5f, 0, 0, 0)] Vector1 Wavelength,
            [Slot(4, Binding.None, 0.5f, 0, 0, 0)] Vector1 Amplitude,
            [Slot(5, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
   // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Calculate the scaling value in the Y direction (relative to the amplitude)
    $precision scaledY = scaledUV.y / Amplitude;

    // Calculate the scaling value in the X direction (relative to the wavelength)
    $precision scaledX = scaledUV.x / Wavelength;

    // Calculate periodic changes in the X direction
    $precision periodicX = abs((scaledX - floor(scaledX + 0.5)) * 2) * 2 - 1;

    // Calculate waveform value
    $precision waveValue = scaledY - periodicX;

   //Apply smooth step function and width limit
    $precision smoothedWave = smoothstep(
       clamp(Width - 0.05, 0.0001, 2.5),
       clamp(Width + 0.05, 0.0001, 2.5),
       abs(waveValue - round(waveValue)) * 2);

#if defined(SHADER_STAGE_RAY_TRACING)

    Out = saturate(smoothedWave * 1e7);
#else

    Out = saturate(smoothedWave);
#endif

}";
        }

        static string Unity_ZigZag_Derivative(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.5f, 0f, 0, 0)] Vector1 Width,
            [Slot(3, Binding.None, 0.5f, 0, 0, 0)] Vector1 Wavelength,
            [Slot(4, Binding.None, 0.5f, 0, 0, 0)] Vector1 Amplitude,
            [Slot(5, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
   // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Calculate the scaling value in the Y direction (relative to the amplitude)
    $precision scaledY = scaledUV.y / Amplitude;

    // Calculate the scaling value in the X direction (relative to the wavelength)
    $precision scaledX = scaledUV.x / Wavelength;

   // Calculate periodic changes in the X direction
    $precision periodicX = abs((scaledX - floor(scaledX + 0.5)) * 2) * 2 - 1;

    // Calculate waveform value
    $precision waveValue = scaledY - periodicX;

    //Adjust waveform values ​​and apply width
    waveValue = (abs(waveValue - round(waveValue)) * 2 - Width);

    //Apply edge smoothing
    waveValue = saturate(waveValue / fwidth(waveValue));

#if defined(SHADER_STAGE_RAY_TRACING)

    Out = saturate(waveValue * 1e7);
#else

    Out = saturate(waveValue);
#endif

}";
        }
    }
}
