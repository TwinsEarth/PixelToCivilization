using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Taiji")]
    class TaijiNode : CodeFunctionNode
    {
        public TaijiNode()
        {
            name = "Taiji";
            synonyms = new string[] { "taiji" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Taiji", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Taiji(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0.0f, 0, 0)] Vector1 Rotate,
            [Slot(2, Binding.None, 0.1f, 0.0f, 0, 0)] Vector1 EyeSize,
            [Slot(3, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out,
            [Slot(4, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Alpha)

        {
            return
@"
{

    $precision2 TransformedUV = UV * 2.0 - 1.0;
    $precision RotationAngle=radians(Rotate);
    $precision SinRotation = sin(RotationAngle);
    $precision CosRotation = cos(RotationAngle);
    //center rotation matrix
    $precision2x2 RotationMatrix = $precision2x2(CosRotation, -SinRotation, SinRotation, CosRotation);
    RotationMatrix *= 0.5;
    RotationMatrix += 0.5;
    RotationMatrix = RotationMatrix*2 - 1;
    //multiply the UVs by the rotation matrix
    TransformedUV.xy = mul(TransformedUV.xy, RotationMatrix);
    $precision DistanceFromOrigin = length(TransformedUV);
    $precision UpperHalfCircleDistance = length(TransformedUV - $precision2(0.0, 0.5)) - 0.5;  
    $precision LowerHalfCircleDistance = length(TransformedUV - $precision2(0.0, -0.5)) - 0.5; 
    $precision UpperEye = smoothstep(0,0.02,length(TransformedUV - float2(0.0, 0.5)) - EyeSize); 
    $precision LowerEye = smoothstep(0,0.02,length(TransformedUV - float2(0.0, -0.5)) - EyeSize); 
    $precision sCurve = (TransformedUV.xy > 0.0) ? smoothstep(0.0,0.02, UpperHalfCircleDistance) : smoothstep(0.0,0.02, -LowerHalfCircleDistance);
    $precision CircularMask = 1-smoothstep(0.99, 1.0,DistanceFromOrigin);  // 限制在半径1的圆内
    Out= (sCurve+(1-UpperEye))*LowerEye*CircularMask; 
#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate((saturate(Out * 1e7)));
    Alpha= saturate((saturate(CircularMask * 1e7)));
#else
    Out = saturate(Out);
    Alpha = saturate(CircularMask);
#endif

}";
        }
    }
}
