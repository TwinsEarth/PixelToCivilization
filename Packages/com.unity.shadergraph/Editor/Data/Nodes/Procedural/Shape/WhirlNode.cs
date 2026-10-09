using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{

    [Title("Procedural", "Shape", "Whirl")]
    class WhirlNode : CodeFunctionNode
    {
        public WhirlNode()
        {
            name = "Whirl";
            synonyms = new string[] { "twirl" };
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
                    return GetType().GetMethod("Unity_Whirl_Smooth", BindingFlags.Static | BindingFlags.NonPublic);
                case AntiAliasType.Derivative:
                    return GetType().GetMethod("Unity_Whirl_Derivative", BindingFlags.Static | BindingFlags.NonPublic);
                case AntiAliasType.Fastest:
                default:
                    return GetType().GetMethod("Unity_Whirl_Fastest", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_Whirl_Fastest(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.5f, 0.5f, 0, 0)] Vector2 Position,
            [Slot(3, Binding.None, 8f, 0f, 0, 0)] Vector1 Count,
            [Slot(4, Binding.None, 5f, 0, 0, 0)] Vector1 Width,
            [Slot(5, Binding.None, 0.5f, 0, 0, 0)] Vector1 Rotation,
            [Slot(6, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
    // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    //Calculate the offset relative to the center point
    $precision2 uvOffset = scaledUV - Position;

    // Calculate radius (distance from center point)
    $precision distanceFromCenter = length(uvOffset) * 2;

    // Calculate the angle (relative to the center point)
    $precision angleInRadians = atan2(uvOffset.x, uvOffset.y) * (1.0 / 6.28);

    //Construct polar coordinate UV
    $precision2 polarUV = $precision2(distanceFromCenter, angleInRadians);

    // Calculate the rotated polar coordinate UV
    $precision rotatedAngle = (polarUV.y - (Rotation / 6.28318530718) * polarUV.x) * Count;
    $precision adjustedRotatedAngle = abs(rotatedAngle - round(rotatedAngle)) * Width;

    // Calculate final output
    $precision whirl = step(0.5, adjustedRotatedAngle);

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(whirl * 1e7);
#else
    Out = saturate(whirl);
#endif
}";
        }

        static string Unity_Whirl_Smooth(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.5f, 0.5f, 0, 0)] Vector2 Position,
            [Slot(3, Binding.None, 8f, 0f, 0, 0)] Vector1 Count,
            [Slot(4, Binding.None, 5f, 0, 0, 0)] Vector1 Width,
            [Slot(5, Binding.None, 0.5f, 0, 0, 0)] Vector1 Rotation,
            [Slot(6, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            
            return
@"
{
    // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Calculate the offset relative to the center point
    $precision2 uvOffset = scaledUV - Position;

    // Calculate radius (distance from center point)
    $precision distanceFromCenter = length(uvOffset) * 2;

    //  Calculate the angle (relative to the center point)
    $precision angleInRadians = atan2(uvOffset.x, uvOffset.y) * (1.0 / 6.28);

    // Construct polar coordinate UV
    $precision2 polarUV = $precision2(distanceFromCenter, angleInRadians);

    // Calculate the rotated polar coordinate UV
    $precision rotatedAngle = (polarUV.y - (Rotation / 6.28318530718) * polarUV.x) * Count;
    $precision adjustedRotatedAngle = abs(rotatedAngle - round(rotatedAngle)) * Width;

    // Calculate final output
    $precision whirl = smoothstep(0.45, 0.55, adjustedRotatedAngle);

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(whirl * 1e7);
#else
    Out = saturate(whirl);
#endif

}";
        }

        static string Unity_Whirl_Derivative(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 0, 0)] Vector2 Tiling,
            [Slot(2, Binding.None, 0.5f, 0.5f, 0, 0)] Vector2 Position,
            [Slot(3, Binding.None, 8f, 0f, 0, 0)] Vector1 Count,
            [Slot(4, Binding.None, 5f, 0, 0, 0)] Vector1 Width,
            [Slot(5, Binding.None, 0.5f, 0, 0, 0)] Vector1 Rotation,
            [Slot(6, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
                
            return
@"
{


    // Scale UV coordinates
    $precision2 scaledUV = UV * Tiling;

    // Calculate the offset relative to the center point
    $precision2 uvOffset = scaledUV - Position;

    // Calculate radius (distance from center point)
    $precision distanceFromCenter = length(uvOffset) * 2;

    //  Calculate the angle (relative to the center point)
    $precision angleInRadians = atan2(uvOffset.x, uvOffset.y) * (1.0 / 6.28) ;

    // Construct polar coordinate UV
    $precision2 polarUV = $precision2(distanceFromCenter, angleInRadians);

    // Calculate the rotated polar coordinate UV
    $precision rotatedAngle = (polarUV.y - (Rotation / 6.28318530718) * polarUV.x) * Count;
    $precision adjustedRotatedAngle = abs(rotatedAngle - round(rotatedAngle)) * Width;

    // Calculate final output
    $precision whirl=adjustedRotatedAngle-0.5;
               whirl=saturate(whirl/fwidth(whirl));

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(whirl * 1e7);
#else
    Out = saturate(whirl);
#endif
    
}";
        }
    }
}
