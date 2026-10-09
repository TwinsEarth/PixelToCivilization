using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "ShadowMask")]
    class ShadowMaskNode : CodeFunctionNode
    {
        public ShadowMaskNode()
        {
            name = "ShadowMask";
            synonyms = new string[] { "atten","light" };
        }

        public override bool hasPreview { get { return false; } }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("ShadowMask", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string ShadowMask(
            [Slot(0, Binding.MeshUV1)]  Vector2 LightmapUV,
            [Slot(1, Binding.None)] out Vector4 Out
            )
        {

            Out = Vector4.one;
            return
@"
{
    Out = SHADERGRAPH_SHADOWMASK(LightmapUV);

}
";
        }
    }
}
