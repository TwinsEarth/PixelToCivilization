using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Custom Specular Term")]
    class CustomSpecularNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireNormal, IMayRequireViewDirection, IMayRequireTangent, IMayRequireBitangent
    {
        public enum SpecularTermMode
        {
            [InspectorName("Approx GGX")]
            ApproxGGX,
            [InspectorName("Blinn Phong")]
            BlinnPhong,
            [InspectorName("Anisotropy")]
            Aniso,
            [InspectorName("CottonWool")]
            CottonWool,
            [InspectorName("Silk")]
            Silk,
        };
        [SerializeField]
        private SpecularTermMode m_SpecularTermMode = SpecularTermMode.ApproxGGX;

        [EnumControl("Specular Term Mode")]
        public SpecularTermMode specularTermMode
        {
            get { return m_SpecularTermMode; }
            set
            {
                if (m_SpecularTermMode == value)
                    return;

                m_SpecularTermMode = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Node);
            }
        }

        public CustomSpecularNode()
        {
            name = "Custom Specular Term";
            synonyms = new string[] { "ggx","specular", "term", "blinnphong", "anisotropy", "cotton", "wool", "silk" };
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }
        const int OutputSlotId = 0;
        const string kOutputSlotName = "Specular Term";

        const int SpecularSlotID = 1;
        const string kSpecularInputSlotName = "Specular";
        const int RoughnessSlotID = 2;
        const string kRoughnessInputSlotName = "Roughness";
        const string kPerceptualRoughnessInputSlotName = "PerceptualRoughness";

        const int LightDirWSSlotID = 3;
        const string kLightDirWSInputSlotName = "Light Direction WS";
        const int ViewDirWSSlotID = 4;
        const string kViewDirWSInputSlotName = "View Direction WS";

        const int NormalWSSlotID = 5;
        const string kNormalWSInputSlotName = "Normal WS";
        const int TangentSlotID = 6;
        const string kTangentInputSlotName = "Tangent WS";
        const int BitangentSlotID = 7;
        const string kBitangentInputSlotName = "Bitangent WS";
        const int AnisotropySlotID = 8;
        const string kAnisotropyInputSlotName = "Anisotropy";

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string FunctionName = "";
            if (specularTermMode == SpecularTermMode.ApproxGGX)
                FunctionName = "SHADERGRAPH_APPROX_GGX_TERM";
            else if(specularTermMode == SpecularTermMode.BlinnPhong)
                FunctionName = "SHADERGRAPH_BLINN_PHONG_TERM";
            else if(specularTermMode == SpecularTermMode.CottonWool)
                FunctionName = "SHADERGRAPH_COTTON_WOOL_TERM";
            else if(specularTermMode == SpecularTermMode.Silk)
                FunctionName = "SHADERGRAPH_SILK_TERM";
            else
                FunctionName = "SHADERGRAPH_ANISOTROPY_TERM";

            if (specularTermMode == SpecularTermMode.Aniso || specularTermMode == SpecularTermMode.Silk)
            {
                sb.AppendLine("$precision3 {0} = {9}({1},{2},{3},{4},{5},{6},{7},{8});", 
                GetVariableNameForSlot(OutputSlotId), 
                GetSlotValue(NormalWSSlotID, generationMode),
                GetSlotValue(TangentSlotID, generationMode),
                GetSlotValue(BitangentSlotID, generationMode),
                GetSlotValue(RoughnessSlotID, generationMode),
                GetSlotValue(LightDirWSSlotID, generationMode),
                GetSlotValue(ViewDirWSSlotID, generationMode),
                GetSlotValue(SpecularSlotID, generationMode),
                GetSlotValue(AnisotropySlotID, generationMode),
                FunctionName);
            }
            else
            {
                sb.AppendLine("$precision3 {0} = {6}({1},{2},{3},{4},{5});", 
                GetVariableNameForSlot(OutputSlotId), 
                GetSlotValue(NormalWSSlotID, generationMode),
                GetSlotValue(RoughnessSlotID, generationMode),
                GetSlotValue(LightDirWSSlotID, generationMode),
                GetSlotValue(ViewDirWSSlotID, generationMode),
                GetSlotValue(SpecularSlotID, generationMode),
                FunctionName);
            }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            List<int> Slots = new List<int>();

            
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            Slots.Add(OutputSlotId);

            AddSlot(new Vector3MaterialSlot(SpecularSlotID, kSpecularInputSlotName, kSpecularInputSlotName, SlotType.Input, new Vector3(0.04f, 0.04f, 0.04f)));
            Slots.Add(SpecularSlotID);
            AddSlot(new Vector1MaterialSlot(RoughnessSlotID, specularTermMode == SpecularTermMode.BlinnPhong ? kPerceptualRoughnessInputSlotName : kRoughnessInputSlotName, specularTermMode == SpecularTermMode.BlinnPhong ? kPerceptualRoughnessInputSlotName : kRoughnessInputSlotName, SlotType.Input, 0.5f));
            Slots.Add(RoughnessSlotID);
            AddSlot(new Vector3MaterialSlot(LightDirWSSlotID, kLightDirWSInputSlotName, kLightDirWSInputSlotName, SlotType.Input, Vector3.zero));
            Slots.Add(LightDirWSSlotID);
            AddSlot(new ViewDirectionMaterialSlot(ViewDirWSSlotID, kViewDirWSInputSlotName, kViewDirWSInputSlotName, CoordinateSpace.World));
            Slots.Add(ViewDirWSSlotID);
            AddSlot(new NormalMaterialSlot(NormalWSSlotID, kNormalWSInputSlotName, kNormalWSInputSlotName, CoordinateSpace.World));
            Slots.Add(NormalWSSlotID);
            
            if (specularTermMode == SpecularTermMode.Aniso || specularTermMode == SpecularTermMode.Silk)
            {
                AddSlot(new TangentMaterialSlot(TangentSlotID, kTangentInputSlotName, kTangentInputSlotName, CoordinateSpace.World));
                Slots.Add(TangentSlotID);
                
                AddSlot(new BitangentMaterialSlot(BitangentSlotID, kBitangentInputSlotName, kBitangentInputSlotName, CoordinateSpace.World));
                Slots.Add(BitangentSlotID);

                AddSlot(new Vector1MaterialSlot(AnisotropySlotID, kAnisotropyInputSlotName, kAnisotropyInputSlotName, SlotType.Input, 0.5f));
                Slots.Add(AnisotropySlotID);
            }
            RemoveSlotsNameNotMatching(Slots, true);
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return FindSlot<NormalMaterialSlot>(NormalWSSlotID).RequiresNormal();
        }
        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return FindSlot<TangentMaterialSlot>(TangentSlotID).RequiresTangent();
        }
        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return FindSlot<BitangentMaterialSlot>(BitangentSlotID).RequiresBitangent();
        }
        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return FindSlot<ViewDirectionMaterialSlot>(ViewDirWSSlotID).RequiresViewDirection();
        }
    }
}
