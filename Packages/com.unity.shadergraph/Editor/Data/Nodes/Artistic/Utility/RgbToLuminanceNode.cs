using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Utility", "RGB To Luminance")]
    class RgbToLuminanceNode : CodeFunctionNode
    {
        public RgbToLuminanceNode()
        {
            name = "RGB To Luminance";
            synonyms = new string[] { "Luminance", "RGB", "conversion" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_RgbToLuminance", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_RgbToLuminance(
            [Slot(0, Binding.None)] ColorRGB In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
@"
{
    Out= Luminance(In);
       
}
";
        }
    }
}
