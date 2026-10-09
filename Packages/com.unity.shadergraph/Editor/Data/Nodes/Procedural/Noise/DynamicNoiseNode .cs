using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Noise", "Dynamic Noise")]
    class DynamicNoiseNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireMeshUV
    {
        // 0 original version
        // 1 add deterministic noise option
        public override int latestVersion => 1;
        public override IEnumerable<int> allowedNodeVersions => new int[] { 1 };

        public const int UVSlotId = 0;
        public const int ScaleSlotId = 1;
        public const int TimeSlotId = 2;
        public const int OutSlotId = 3;

        const string kUVSlotName = "UV";
        const string kScaleSlotName = "Scale";
        const string kTimeSlotName = "Time";
        const string kOutSlotName = "Out";

        public DynamicNoiseNode()
        {
            name = "Dynamic Noise";
            synonyms = new string[] { "value noise" };
            UpdateNodeAfterDeserialization();
        }

        public enum HashType
        {
            Deterministic,
            LegacySine,
        };
        static readonly string[] kHashFunctionPrefix =
        {
            "Hash_Tchou_3_3_",
            "Hash_LegacyMod_3_3_",
        };

        public override bool hasPreview => true;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(UVSlotId, kUVSlotName, kUVSlotName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(ScaleSlotId, kScaleSlotName, kScaleSlotName, SlotType.Input, 500.0f));
            AddSlot(new Vector1MaterialSlot(TimeSlotId, kTimeSlotName, kTimeSlotName, SlotType.Input, 0.0f));
            AddSlot(new Vector1MaterialSlot(OutSlotId, kOutSlotName, kOutSlotName, SlotType.Output, 0.0f));
            RemoveSlotsNameNotMatching(new[] { UVSlotId, ScaleSlotId, TimeSlotId, OutSlotId });
        }

        [SerializeField]
        private HashType m_HashType = HashType.Deterministic;
        readonly bool m_HumanReadable;
        [EnumControl("Hash Type")]
        public HashType hashType
        {
            get
            {
                if (((int)m_HashType < 0) || ((int)m_HashType >= kHashFunctionPrefix.Length))
                    return (HashType)0;
                return m_HashType;
            }
            set
            {
                if (m_HashType == value)
                    return;

                m_HashType = value;
                Dirty(ModificationScope.Graph);
            }
        }





        public static PassDescriptor BuildPass()
        {
            PassDescriptor pass = new PassDescriptor()
            {
                
                pragmas = new PragmaCollection {  { Pragma.Fragment("_ CUSTOM_FEATURE") } },

            };
            return pass;
        }

        void IGeneratesFunction.GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl");
    

            var hashType = this.hashType;
            var hashTypeString = hashType.ToString();
            var HashFunction = kHashFunctionPrefix[(int)hashType];

            registry.ProvideFunction($"random3_{hashTypeString}_$precision", s =>
            {
                s.AppendLine($"$precision3 random3_{hashTypeString}_$precision ($precision3 x)");
                using (s.BlockScope())
                {
                    s.AppendLine("$precision j = 4096.0 * sin(dot(x, $precision3(17.0, 59.4, 15.0)));");
                    s.AppendLine("$precision3 r;");
                    s.AppendLine("r.z = frac(512.0 * j);");
                    s.AppendLine("j *= .125;");
                    s.AppendLine("r.x = frac(512.0 * j);");
                    s.AppendLine("j *= .125;");
                    s.AppendLine("r.y = frac(512.0 * j);");
                    s.AppendLine("return r - 0.5;");
                }
            });


            registry.ProvideFunction($"simplex3d{hashTypeString}_$precision", s =>
            {
                s.AppendLine($"$precision simplex3d{hashTypeString}_$precision ($precision3 p)");
                using (s.BlockScope())
                {

                    s.AppendLine(" $precision3 f =$precision3(0.3333333,0.3333333,0.3333333);");
                    s.AppendLine(" $precision3 g =$precision3(0.1666667,0.1666667,0.1666667);");
                    s.AppendLine("$precision3 s = floor(p + dot(p, f));");
                    s.AppendLine("$precision3 x = p - s + dot(s, g);");
                    s.AppendLine("$precision3 e = step($precision3(0.0, 0.0, 0.0), x - x.yzx);");
                    s.AppendLine("$precision3 i1 = e * (1.0 - e.zxy);");
                    s.AppendLine("$precision3 i2 = 1.0 - e.zxy * (1.0 - e);");
                    s.AppendLine("$precision3 x1 = x - i1 + g.x;");
                    s.AppendLine("$precision3 x2 = x - i2 + 2.0 * g.x;");
                    s.AppendLine("$precision3 x3 = x - 1.0 + 3.0 * g.x;");
                    s.AppendLine(" $precision4 w, d;");
                    s.AppendLine(" w.x = dot(x, x);");
                    s.AppendLine(" w.y = dot(x1, x1);");
                    s.AppendLine(" w.z = dot(x2, x2);");
                    s.AppendLine(" w.w = dot(x3, x3);");
                    s.AppendLine(" w = max(0.6 - w, 0.0);");
                    s.AppendLine($"$precision3 s0; {HashFunction}$precision(s, s0);");
                    s.AppendLine($"$precision3 s1; {HashFunction}$precision(s + i1, s1);");
                    s.AppendLine($"$precision3 s2; {HashFunction}$precision(s + i2, s2);");
                    s.AppendLine($"$precision3 s3; {HashFunction}$precision(s + 1.0, s3);");
                    s.AppendLine($"d.x = dot(random3_{hashTypeString}_$precision(s0), x);");
                    s.AppendLine($"d.y = dot(random3_{hashTypeString}_$precision(s1), x1);");
                    s.AppendLine($"d.z = dot(random3_{hashTypeString}_$precision(s2), x2);");
                    s.AppendLine($"d.w = dot(random3_{hashTypeString}_$precision(s3), x3);");
                    s.AppendLine("w = pow(w,3);");
                    s.AppendLine("d *= w;");
                    s.AppendLine("return dot(d, $precision4(52.0, 54.0, 54.0, 54.0));");



                }
            });

            registry.ProvideFunction($"fbm{hashTypeString}_$precision", s =>
            {
                s.AppendLine($"$precision fbm{hashTypeString}_$precision ($precision2 xy,$precision z,int octs)");
                using (s.BlockScope())
                {

                    s.AppendLine("$precision f = 1.0;");
                    s.AppendLine(" $precision a = 1.0;");
                    s.AppendLine(" $precision t = 0.0;");
                    s.AppendLine(" $precision a_bound = 0.0;");
                    s.AppendLine("for (int i = 0; i < octs; i++)");
                    s.AppendLine("  {");
                    s.AppendLine($"t += a * simplex3d{hashTypeString}_$precision ($precision3(xy * f, z * f));");
                    s.AppendLine(" f *= 2.0;");
                    s.AppendLine("a_bound += a;");
                    s.AppendLine("a *= 0.5;");
                    s.AppendLine("}");
                    s.AppendLine(" return t / a_bound;");

                }
            });

            registry.ProvideFunction($"noise_final_comp{hashTypeString}_$precision", s =>
            {
                s.AppendLine($"$precision noise_final_comp{hashTypeString}_$precision ($precision2 xy,$precision z)");
                using (s.BlockScope())
                {

                    s.AppendLine($"$precision value = fbm{hashTypeString}_$precision($precision2(xy.x / 200.0 + 513.0, xy.y / 200.0 + 124.0), z, 3);");
                    s.AppendLine("value = 1.0 - abs(value);");
                    s.AppendLine("value = value * value;");
                    s.AppendLine("return value * 2.0 - 1.0;");


                }
            });


            registry.ProvideFunction($"noise_final_{hashTypeString}_$precision", s =>
            {
                s.AppendLine($"$precision noise_final_{hashTypeString}_$precision ($precision2 xy,$precision z)");
                using (s.BlockScope())
                {

                    s.AppendLine($" $precision value = fbm{hashTypeString}_$precision($precision2((noise_final_comp{hashTypeString}_$precision(xy, z) * 15.0 + xy.x) / 100.0, (noise_final_comp{hashTypeString}_$precision(xy + 300.0, z) * 15.0 + xy.y) / 100.0), z * 1.5, 5);");
                    s.AppendLine(" return max(0.0, min(1.0, (value * 0.5 + 0.5) * 1.3));");

                }
            });



            registry.ProvideFunction($"Unity_DynamicNoise_" + hashTypeString + "_$precision", s =>
            {
                s.AppendLine($"void Unity_DynamicNoise_{hashTypeString}_$precision($precision2 UV, $precision Scale, $precision Time,out $precision Out)");
                using (s.BlockScope())
                {
                    s.AppendLine("UV = UV * Scale;");
                    s.AppendLine($" Out= noise_final_{hashTypeString}_$precision(UV, Time * 0.025 + 0.05 * sin(Time * 0.2 + (UV.x * 0.3 * (sin(Time / 30.0) - 0.3) + UV.y) / 265.0));");
   
                }
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var hashType = this.hashType;
            var hashTypeString = hashType.ToString();
            string uv = GetSlotValue(UVSlotId, generationMode);
            string scale = GetSlotValue(ScaleSlotId, generationMode);
            string time = GetSlotValue(TimeSlotId, generationMode);
            string output = GetVariableNameForSlot(OutSlotId);
            var outSlot = FindSlot<MaterialSlot>(OutSlotId);

            sb.AppendLine($"{outSlot.concreteValueType.ToShaderString(PrecisionUtil.Token)} {output};");
            sb.AppendLine($"Unity_DynamicNoise_{hashTypeString}_$precision({uv}, {scale},{time},{output});");
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
                // old nodes should select "LegacySine" to replicate old behavior
                hashType = HashType.LegacySine;
                ChangeVersion(1);
            }
        }
    }
}
