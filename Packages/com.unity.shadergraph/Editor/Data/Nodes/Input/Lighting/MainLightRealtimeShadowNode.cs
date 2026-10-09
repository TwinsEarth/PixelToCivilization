using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Main Light Realtime Shadow")]
    class MainLightRealtimeShadowNode : CodeFunctionNode
    {
        public MainLightRealtimeShadowNode()
        {
            name = "Main Light Realtime Shadow";
            synonyms = new string[] { "atten","sun" };
        }

        public override bool hasPreview { get { return false; } }

        protected override MethodInfo GetFunctionToConvert()
        {

            return GetType().GetMethod("MainLightShadowAttenuation", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string MainLightShadowAttenuation(
            [Slot(0, Binding.WorldSpacePosition)] Vector3 Position,
            [Slot(1, Binding.None)] out Vector1 Out

            )
        {
        

            return
@"
{

	#ifdef SHADERGRAPH_PREVIEW
		Out = 1;
	#else
        Out=SHADERGRAPH_MAINLIGHTREALTIMESHADOW(Position);
	#endif
	

}
";
        }
    }
}
