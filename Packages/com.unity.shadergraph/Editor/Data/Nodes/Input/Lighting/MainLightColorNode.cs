using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Main Light Color")]
    class MainLightColorNode : CodeFunctionNode
    {
        public MainLightColorNode()
        {
            name = "Main Light Color";
            synonyms = new string[] { "sun" };
        }

        public override bool hasPreview { get { return false; } }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("MainLightColor", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string MainLightColor(
            [Slot(0, Binding.None)] out Vector3 Out
                  
            )
        {
            Out = Vector3.one;

            return
@"
{

    #if SHADERGRAPH_PREVIEW
    Out = half3(1, 1, 1);
    #else
    Out = SHADERGRAPH_MAIN_LIGHT_COLOR();
    #endif
    
  
}
";
        }
    }
}
