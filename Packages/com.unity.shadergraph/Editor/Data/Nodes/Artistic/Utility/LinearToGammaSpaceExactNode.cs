using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Utility", "LinearToGammaSpaceExact")]
    class LinearToGammaSpaceExactNode : CodeFunctionNode
    {
        public LinearToGammaSpaceExactNode()
        {
            name = "LinearToGammaSpaceExact";
            synonyms = new string[] { "gamma", "linear", "conversion" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_LinearToGammaSpaceExact", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_LinearToGammaSpaceExact(
            [Slot(0, Binding.None)] Vector1 In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
           // Out = 0f;
            return
@"
{
    if (In <= 0.0)
        Out= 0.0;
    else if (In <= 0.0031308)
        Out= 12.92 * In;
    else if (In < 1.0)
        Out= 1.055 * pow(In, 0.4166667) - 0.055;
    else
        Out= pow(In, 0.45454545);
       
}
";
        }
    }
}
