using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Pixelate UV")]
    class PixelateUVNode : CodeFunctionNode
    {
        public PixelateUVNode()
        {
            name = "Pixelate UV";
            synonyms = new string[] { "Pixelate", "mosaic","uv" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_PixelateUV", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_PixelateUV(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 10f, 1f, 1f, 1f)] Vector1 PixelsX,
            [Slot(2, Binding.None, 10f, 1f, 1f, 1f)] Vector1 PixelsY,
            [Slot(3, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;
            return
@"
{

		$precision2 pixelStep = $precision2( PixelsX, PixelsY );
		Out = floor( UV * pixelStep ) / max(pixelStep,0.00001);
}
";
        }
    }
}
