using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Utility", "RGB To Grayscale")]
    class RgbToGrayscaleNode : CodeFunctionNode
    {
        public RgbToGrayscaleNode()
        {
            name = "RGB To Grayscale";
            synonyms = new string[] { "Grayscale", "rgb", "conversion" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_RgbToGrayscale", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_RgbToGrayscale(
            [Slot(0, Binding.None)] ColorRGB In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
@"
{
    Out= (In.r+In.g+In.b)/3;
       
}
";
        }
    }
}
