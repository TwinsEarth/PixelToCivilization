using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Main Light Shadow")]
    class MainLightShadowNode : CodeFunctionNode
    {
        public MainLightShadowNode()
        {
            name = "MainLightShadow";
            synonyms = new string[] { "atten","sun" };
        }

        public override bool hasPreview { get { return false; } }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("MainLightShadow", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string MainLightShadow(
            [Slot(0, Binding.MeshUV1)]  Vector2 LightmapUV,
            [Slot(1, Binding.WorldSpacePosition)] Vector3 PositionWS,
            [Slot(2, Binding.None)] out Vector1 Out
            )
        {

            return
@"
{
	#ifdef SHADERGRAPH_PREVIEW
		Out =1;
	#else
        Out =SHADERGRAPH_MAINLIGHTSHADOW(PositionWS,LightmapUV);
    #endif


}
";
        }
    }
}
