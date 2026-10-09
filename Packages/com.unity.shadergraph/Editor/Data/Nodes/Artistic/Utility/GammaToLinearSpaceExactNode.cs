using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Utility", "GammaToLinearSpaceExact")]
    class GammaToLinearSpaceExactNode : CodeFunctionNode
    {
        public GammaToLinearSpaceExactNode()
        {
            name = "GammaToLinearSpaceExact";
            synonyms = new string[] { "gamma", "linear", "conversion" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_GammaToLinearSpaceExact", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_GammaToLinearSpaceExact(
            [Slot(0, Binding.None)] Vector1 In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
           // Out = 0f;
            return
@"
{
    if (In <= 0.04045)
        Out= In / 12.92;
    else if (In < 1.0)
         Out= pow((In + 0.055)/1.055, 2.4);
    else
         Out= pow(In, 2.2);
       
}
";
        }
    }
}
