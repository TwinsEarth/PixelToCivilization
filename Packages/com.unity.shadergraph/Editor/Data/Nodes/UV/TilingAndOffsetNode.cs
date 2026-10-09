using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Tiling And Offset")]
    class TilingAndOffsetNode : CodeFunctionNode, IMayRequireOutputVGTiling
    {
        public TilingAndOffsetNode()
        {
            name = "Tiling And Offset";
            synonyms = new string[] { "pan", "scale" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_TilingAndOffset", BindingFlags.Static | BindingFlags.NonPublic);
        }

        public VGTiling RequiresOutputVGTiling(int slotId)
        {
            // When generate code, we confirm whether we need Tiling or not.
            return FindSlot<MaterialSlot>(0).RequiresOutputVGTiling();
        }

        public string GetVGTilingVariableName(int slotId)
        {
            return GetVariableNameForSlot(4);
        }

        static string Unity_TilingAndOffset(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 1f, 1f)] Vector2 Tiling,
            [Slot(2, Binding.None, 0f, 0f, 0f, 0f)] Vector2 Offset,
            [Slot(3, Binding.None)] out Vector2 Out,
            [Slot(4, Binding.None, true)] out Vector2 VGTiling)
        {
            Out = Vector2.zero;
            VGTiling = Vector2.zero;
            return
@"
{
    Out = UV * Tiling + Offset;
    VGTiling = Tiling;
}
";
        }
    }
}
