using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Noise", "Simple Wood")]
    class WoodNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireMeshUV
    {
        // 0 original version
        // 1 add deterministic noise option
        public override int latestVersion => 1;
        public override IEnumerable<int> allowedNodeVersions => new int[] { 1 };

        public const int UVSlotId = 0;
        public const int FactorSlotId = 1;
        public const int DistortSlotId = 2;
        public const int OutSlotId = 3;

        const string kUVSlotName = "UV";
        const string kFactorSlotName = "Factor";
        const string kDistortSlotName = "Distort";
        const string kOutSlotName = "Out";

        public WoodNode()
        {
            name = "Simple Wood";
            synonyms = new string[] { "value wood" };
            UpdateNodeAfterDeserialization();
        }


        public override bool hasPreview => true;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(UVSlotId, kUVSlotName, kUVSlotName, UVChannel.UV0));
            AddSlot(new Vector4MaterialSlot(FactorSlotId, kFactorSlotName, kFactorSlotName, SlotType.Input,new Vector4(1.0f,1.2f,-1.2f,5.0f)));
            AddSlot(new Vector1MaterialSlot(DistortSlotId, kDistortSlotName, kDistortSlotName, SlotType.Input, 3.0f));
            AddSlot(new Vector1MaterialSlot(OutSlotId, kOutSlotName, kOutSlotName, SlotType.Output, 0.0f));
            RemoveSlotsNameNotMatching(new[] { UVSlotId, FactorSlotId, DistortSlotId, OutSlotId });
        }

        [SerializeField]


        void IGeneratesFunction.GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl");


            registry.ProvideFunction("Unity_grad_$precision", s =>
            {
                s.AppendLine("$precision2 Unity_grad_$precision($precision2 uv)");
                using (s.BlockScope())
                {
                    s.AppendLine("int n = uv.x + uv.y * 11111;");
                    s.AppendLine(" n = (n << 13) ^ n;");
                    s.AppendLine(" n = (n * (n * n * 15731 + 789221) + 1376312589) >> 16;");
                    s.AppendLine("#if 0");
                    s.AppendLine("return $precision2(cos($precision(n)), sin($precision(n)));");
                    s.AppendLine("#else");
                    s.AppendLine("n &= 7;");
                    s.AppendLine(" $precision2 gr = $precision2(n & 1, n >> 1) * 2.0 - 1.0;");
                    s.AppendLine(" return (n >= 6) ? $precision2(0.0, gr.x) :(n >= 4) ? $precision2(gr.x, 0.0) :gr;");
                    s.AppendLine(" #endif");
                }
            });




            registry.ProvideFunction("Unity_SimpleWood_ValueNoise_$precision", s =>
            {
                s.AppendLine("$precision Unity_SimpleWood_ValueNoise_$precision ($precision2 uv)");
                using (s.BlockScope())
                {

                    s.AppendLine(" $precision2 i = $precision2(floor(uv));");
                    s.AppendLine(" $precision2 f = frac(uv);");
                    s.AppendLine("  $precision2 u = f * f * (3.0 - 2.0 * f);");
                    s.AppendLine("$precision dGradA = dot(Unity_grad_$precision(i + $precision2(0, 0)), f - $precision2(0.0, 0.0));");
                    s.AppendLine("$precision dGradB = dot(Unity_grad_$precision(i + $precision2(1, 0)), f - $precision2(1.0, 0.0));");
                    s.AppendLine("$precision dGradC = dot(Unity_grad_$precision(i + $precision2(0, 1)), f - $precision2(0.0, 1.0));");
                    s.AppendLine("$precision dGradD = dot(Unity_grad_$precision(i + $precision2(1, 1)), f - $precision2(1.0, 1.0));");
                    s.AppendLine("$precision dGrad = lerp(lerp(dGradA, dGradB, u.x), lerp(dGradC, dGradD, u.x), u.y);");
                    s.AppendLine(" return dGrad;");

                }
            });

            registry.ProvideFunction("Unity_SimpleWood" + "_$precision", s =>
            {
                s.AppendLine("void Unity_SimpleWood_$precision($precision2 UV, $precision4 Factor, $precision Distort, out $precision Out)");
                using (s.BlockScope())
                {

                    s.AppendLine("UV.x = UV.x ;");
                    s.AppendLine("UV.y = UV.y * sin(2);");
                    s.AppendLine("$precision2 uv_nse = 0.2 * UV;");
                    s.AppendLine("$precision f = 0.0;");
                    s.AppendLine("$precision2x2 m = $precision2x2(Factor.x,Factor.y,Factor.z,Factor.w);");
                    s.AppendLine("f = 0.5000 * Unity_SimpleWood_ValueNoise_$precision(uv_nse);");
                    s.AppendLine("uv_nse = mul(m,uv_nse);");
                    s.AppendLine("f += 0.2500 * Unity_SimpleWood_ValueNoise_$precision(uv_nse);");
                    s.AppendLine("uv_nse =mul(m,uv_nse);");
                    s.AppendLine("f += 0.1250 * Unity_SimpleWood_ValueNoise_$precision(uv_nse);");
                    s.AppendLine("uv_nse =mul(m,uv_nse);");
                    s.AppendLine("f += 0.0625 * Unity_SimpleWood_ValueNoise_$precision(uv_nse);");
                    s.AppendLine("uv_nse = mul(m,uv_nse);");
                    s.AppendLine("f += 0.025* Unity_SimpleWood_ValueNoise_$precision(uv_nse);");
                    s.AppendLine("UV.y = UV.y + f * Distort;");
                    s.AppendLine("Out = saturate(Unity_SimpleWood_ValueNoise_$precision(UV * $precision2(0.1, 15.5)));");
                    
                }
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {

            string uv = GetSlotValue(UVSlotId, generationMode);
            string factor = GetSlotValue(FactorSlotId, generationMode);
            string distort = GetSlotValue(DistortSlotId, generationMode);
            string output = GetVariableNameForSlot(OutSlotId);
            var outSlot = FindSlot<MaterialSlot>(OutSlotId);

            sb.AppendLine($"{outSlot.concreteValueType.ToShaderString(PrecisionUtil.Token)} {output};");
            sb.AppendLine($"Unity_SimpleWood_$precision({uv}, {factor}, {distort},{output});");
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                var result = false;
                foreach (var slot in tempSlots)
                {
                    if (slot.RequiresMeshUV(channel))
                    {
                        result = true;
                        break;
                    }
                }

                tempSlots.Clear();
                return result;
            }
        }

        public override void OnAfterMultiDeserialize(string json)
        {
            if (sgVersion < 1)
            {

                ChangeVersion(1);
            }
        }
    }
}
