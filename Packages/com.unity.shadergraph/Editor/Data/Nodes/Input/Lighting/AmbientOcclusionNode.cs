using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Ambient Occlusion")]
    class AmbientOcclusionNode : CodeFunctionNode
    {
        public AmbientOcclusionNode()
        {
            name = "Ambient Occlusion";
            synonyms = new string[] { "occlusion","ambient" };
        }

        public override bool hasPreview { get { return false; } }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("AmbientOcclusion", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string AmbientOcclusion(
            [Slot(0, Binding.ScreenPosition)]  Vector4 ScreenSpaceUV,
            [Slot(1, Binding.None, 1, 1, 1, 1)]  Vector1 Occlusion,
            [Slot(2, Binding.None)] out Vector1 IndirectAmbientOcclusion,
            [Slot(3, Binding.None)] out Vector1 DirectAmbientOcclusion
            )
        {
            return
@"
{
    SHADERGRAPH_AMBIENT_OCCLUSION(ScreenSpaceUV, Occlusion, IndirectAmbientOcclusion, DirectAmbientOcclusion);

}
";
        }
    }
}
