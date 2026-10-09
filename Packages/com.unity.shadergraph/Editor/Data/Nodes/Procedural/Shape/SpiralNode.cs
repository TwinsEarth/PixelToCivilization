using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{

    [Title("Procedural", "Shape", "Spiral")]
    class SpiralNode : CodeFunctionNode
    {
        public SpiralNode()
        {
            name = "Spiral";
            synonyms = new string[] { "swirl" };
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
                    return GetType().GetMethod("Unity_Spiral_Smooth", BindingFlags.Static | BindingFlags.NonPublic);
                case AntiAliasType.Derivative:
                    return GetType().GetMethod("Unity_Spiral_Derivative", BindingFlags.Static | BindingFlags.NonPublic);
                case AntiAliasType.Fastest:
                default:
                    return GetType().GetMethod("Unity_Spiral_Fastest", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_Spiral_Fastest(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.5f, 0.5f, 0, 0)] Vector2 Position,
            [Slot(3, Binding.None, 5f, 0, 0, 0)] Vector1 Number,
            [Slot(4, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(5, Binding.None, 0.5f, 0, 0, 0)] Vector1 Separation,
            [Slot(6, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
    // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Calculate offset relative to position
    $precision2 positionDelta = scaledUV - Position;

    // Calculate radius
    $precision radialDistance = length(positionDelta) * 2 ;

    // Calculate angle
    $precision angularPosition = atan2(positionDelta.x, positionDelta.y) * (1.0 / 6.28) ;

    //Construct polar coordinate UV
    $precision2 polarUV = $precision2(radialDistance, angularPosition);

    // Calculate radius times π
    $precision radialPi = 3.1415926 * polarUV.x;

    // Calculate the separation angle
    $precision separationAngle = 6.28318530718 * Separation;

    // Calculate the spiral angle
    $precision spiralAngle = Number * separationAngle * polarUV.y;

    // Calculate the spiral pattern
    $precision spiralValue = (radialPi - spiralAngle) / separationAngle;
    spiralValue = abs((spiralValue - round(spiralValue)) * separationAngle) * Width;
    spiralValue = step(0.5, spiralValue);

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(spiralValue * 1e7);
#else
    Out = saturate(spiralValue);
#endif
}";
        }

        static string Unity_Spiral_Smooth(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.5f, 0.5f, 0, 0)] Vector2 Position,
            [Slot(3, Binding.None, 5f, 0, 0, 0)] Vector1 Number,
            [Slot(4, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(5, Binding.None, 0.5f, 0, 0, 0)] Vector1 Separation,
            [Slot(6, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
     // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

     // Calculate offset relative to position
    $precision2 positionDelta = scaledUV - Position;

    // Calculate radius
    $precision radialDistance = length(positionDelta) * 2 ;

     // Calculate angle
    $precision angularPosition = atan2(positionDelta.x, positionDelta.y) * (1.0 / 6.28) ;

    //Construct polar coordinate UV
    $precision2 polarUV = $precision2(radialDistance, angularPosition);

      // Calculate radius times π
    $precision radialPi = 3.1415926 * polarUV.x;

    // Calculate the separation angle
    $precision separationAngle = 6.28318530718 * Separation;

    // Calculate the spiral angle
    $precision spiralAngle = Number * separationAngle * polarUV.y;

    // Calculate the spiral pattern
    $precision spiralValue = (radialPi - spiralAngle) / separationAngle;
    spiralValue = abs((spiralValue - round(spiralValue)) * separationAngle) * Width;
    spiralValue = smoothstep(0.45, 0.55, spiralValue);

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(spiralValue * 1e7);
#else
    Out = saturate(spiralValue);
#endif

}";
        }

        static string Unity_Spiral_Derivative(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.5f, 0.5f, 0, 0)] Vector2 Position,
            [Slot(3, Binding.None, 5f, 0, 0, 0)] Vector1 Number,
            [Slot(4, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(5, Binding.None, 0.5f, 0, 0, 0)] Vector1 Separation,
            [Slot(6, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
     // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

     // Calculate offset relative to position
    $precision2 positionDelta = scaledUV - Position;

    // Calculate radius
    $precision radialDistance = length(positionDelta) * 2 ;

     // Calculate angle
    $precision angularPosition = atan2(positionDelta.x, positionDelta.y) * (1.0 / 6.28) ;

    //Construct polar coordinate UV
    $precision2 polarUV = $precision2(radialDistance, angularPosition);

      // Calculate radius times π
    $precision radialPi = 3.1415926 * polarUV.x;

    // Calculate the separation angle
    $precision separationAngle = 6.28318530718 * Separation;

    // Calculate the spiral angle
    $precision spiralAngle = Number * separationAngle * polarUV.y;

    // Calculate the spiral pattern
    $precision spiralValue = (radialPi - spiralAngle) / separationAngle;
    spiralValue = abs((spiralValue - round(spiralValue)) * separationAngle) * Width;
    spiralValue = spiralValue - 0.5;
    spiralValue = saturate(spiralValue / fwidth(spiralValue));

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(spiralValue * 1e7);
#else
    Out = saturate(spiralValue);
#endif

}";
        }
    }
}
