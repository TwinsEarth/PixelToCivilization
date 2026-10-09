using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{

    enum LineType
    {
        Smooth,
        Sharp
    };

    [Title("Procedural", "Shape", "Tile")]
    class TileNode : CodeFunctionNode
    {
        public TileNode()
        {
            name = "Tile";
            synonyms = new string[] { "tiling" };
        }

        [SerializeField]
        private LineType m_LineType = LineType.Smooth;

        [EnumControl("Type")]
        public LineType lineType
        {
            get { return m_LineType; }
            set
            {
                if (m_LineType == value)
                    return;

                m_LineType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (lineType)
            {
                case LineType.Smooth:
                    return GetType().GetMethod("Unity_Tile_Smooth", BindingFlags.Static | BindingFlags.NonPublic);
                case LineType.Sharp:
                default:
                    return GetType().GetMethod("Unity_Tile_Sharp", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_Tile_Sharp(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 0, 0, 0)] Vector1 Width,
            [Slot(2, Binding.None, 0.1f, 0, 0, 0)] Vector1 Height,
            [Slot(3, Binding.None, 3f, 0, 0, 0)] Vector1 Factor,
            [Slot(4, Binding.None, 0.33f, 0, 0, 0)] Vector1 Alignment,
            [Slot(5, Binding.None, 0.005f, 0, 0, 0)] Vector1 Stroke,
            [Slot(6, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
    // Calculate row index
    $precision rowIdx = floor(UV.y / Height);

    // Compute normalized row position
    $precision normalizedRowPos = frac(UV.y / Height);

    // Calculate normalized column positions (taking into account alignment and factors)
    $precision normalizedColPos = frac(UV.x-0.5 / Width + Alignment * fmod(rowIdx, Factor));

    // Calculate aspect ratio
    $precision aspectRatio = Width / Height;

    // Calculate gap width
    $precision strokeGap = 1 - Stroke;

    // Compute horizontal hard edges
    $precision horizontalHardEdge = step(0.5 - strokeGap / 2.0, abs(normalizedColPos - 0.5));

    // Calculate vertical hard edges
    $precision verticalHardEdge = 1 - step(0.5 - aspectRatio * (1 - strokeGap) / 2.0, abs(normalizedRowPos - 0.5));

    // Calculate the final result
    $precision tile = horizontalHardEdge * verticalHardEdge;

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(tile * 1e7);
#else
    Out = saturate(tile);
#endif

}";
        }

        static string Unity_Tile_Smooth(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 0, 0, 0)] Vector1 Width,
            [Slot(2, Binding.None, 0.1f, 0, 0, 0)] Vector1 Height,
            [Slot(3, Binding.None, 3f, 0, 0, 0)] Vector1 Factor,
            [Slot(4, Binding.None, 0.33f, 0, 0, 0)] Vector1 Alignment,
            [Slot(5, Binding.None, 0.005f, 0, 0, 0)] Vector1 Stroke,
            [Slot(6, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
    // Calculate row index
    $precision rowIdx = floor(UV.y / Height);

    // Compute normalized row position
    $precision normalizedRowPos = frac(UV.y / Height);

    // Calculate normalized column positions (taking into account alignment and factors)
    $precision normalizedColPos = frac(UV.x / Width + Alignment * fmod(rowIdx, Factor));

    // Calculate aspect ratio
    $precision aspectRatio = Width / Height;

    // Calculate the final result
    $precision tile = smoothstep(0.5, 0.5 - Stroke, abs(normalizedColPos - 0.5)) * smoothstep(0.5, 0.5 - aspectRatio * Stroke, abs(normalizedRowPos - 0.5));

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate(tile * 1e7);
#else
    Out = saturate(tile);
#endif

}";
        }
    }
}
