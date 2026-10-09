using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Custom Diffuse Term")]
    class CustomDiffuseNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireNormal, IMayRequireViewDirection
    {
        public enum DiffuseTermMode
        {
            [InspectorName("Lambertian")]
            Lambertian,
            [InspectorName("Disney Diffuse")]
            Disney,
            [InspectorName("Fabric")]
            Fabric,
        };
        [SerializeField]
        private DiffuseTermMode m_DiffuseTermMode = DiffuseTermMode.Lambertian;

        [EnumControl("Diffuse Term Mode")]
        public DiffuseTermMode diffuseTermMode
        {
            get { return m_DiffuseTermMode; }
            set
            {
                if (m_DiffuseTermMode == value)
                    return;

                m_DiffuseTermMode = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Node);
            }
        }

        public CustomDiffuseNode()
        {
            name = "Custom Diffuse Term";
            synonyms = new string[] { "lambert","term", "diffuse", "disney", "fabric" };
            UpdateNodeAfterDeserialization();
        }
        public override bool hasPreview { get { return false; } }
        const int OutputSlotId = 0;
        const string kOutputSlotName = "Diffuse Term";
        const int DiffuseSlotID = 1;
        const string kDiffuseInputSlotName = "Diffuse";
        const int RoughnessSlotID = 2;
        const string kRoughnessInputSlotName = "Roughness";
        const int LightDirWSSlotID = 3;
        const string kLightDirWSInputSlotName = "Light Direction WS";
        const int ViewDirWSSlotID = 4;
        const string kViewDirWSInputSlotName = "View Direction WS";

        const int NormalWSSlotID = 5;
        const string kNormalWSInputSlotName = "Normal WS";
        const int PerceptualRoughnessSlotID = 6;
        const string kPerceptualRoughnessInputSlotName = "Perceptual Roughness";

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string FunctionName = "";
            if (diffuseTermMode == DiffuseTermMode.Lambertian)
                FunctionName = "SHADERGRAPH_LAMBERTIAN_TERM";
            else if(diffuseTermMode == DiffuseTermMode.Disney)
                FunctionName = "SHADERGRAPH_DISNEY_DIFFUSE_TERM";
            else
                FunctionName = "SHADERGRAPH_FABRIC_LAMBERT_TERM";

            if (diffuseTermMode == DiffuseTermMode.Lambertian)
            {
                sb.AppendLine("$precision3 {0} = {2}({1});", 
                GetVariableNameForSlot(OutputSlotId), 
                GetSlotValue(DiffuseSlotID, generationMode),
                FunctionName);
            }
            else if (diffuseTermMode == DiffuseTermMode.Disney)
            {
                sb.AppendLine("$precision3 {0} = {6}({1},{2},{3},{4},{5});", 
                GetVariableNameForSlot(OutputSlotId), 
                GetSlotValue(DiffuseSlotID, generationMode),
                GetSlotValue(NormalWSSlotID, generationMode),
                GetSlotValue(LightDirWSSlotID, generationMode),
                GetSlotValue(ViewDirWSSlotID, generationMode),
                GetSlotValue(PerceptualRoughnessSlotID, generationMode),
                FunctionName);
            }
            else
            {   
                sb.AppendLine("$precision3 {0} = {3}({1},{2});", 
                GetVariableNameForSlot(OutputSlotId), 
                GetSlotValue(DiffuseSlotID, generationMode),
                GetSlotValue(RoughnessSlotID, generationMode),
                FunctionName);
            }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            List<int> Slots = new List<int>();

            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            Slots.Add(OutputSlotId);


            AddSlot(new Vector3MaterialSlot(DiffuseSlotID, kDiffuseInputSlotName, kDiffuseInputSlotName, SlotType.Input, new Vector3(0.5f, 0.5f, 0.5f)));
            Slots.Add(DiffuseSlotID);
            if (diffuseTermMode == DiffuseTermMode.Disney)
            {
                AddSlot(new Vector3MaterialSlot(LightDirWSSlotID, kLightDirWSInputSlotName, kLightDirWSInputSlotName, SlotType.Input, Vector3.zero));
                Slots.Add(LightDirWSSlotID);
                AddSlot(new ViewDirectionMaterialSlot(ViewDirWSSlotID, kViewDirWSInputSlotName, kViewDirWSInputSlotName, CoordinateSpace.World));
                Slots.Add(ViewDirWSSlotID);
                AddSlot(new NormalMaterialSlot(NormalWSSlotID, kNormalWSInputSlotName, kNormalWSInputSlotName, CoordinateSpace.World));
                Slots.Add(NormalWSSlotID);
            }
            if (diffuseTermMode == DiffuseTermMode.Disney || diffuseTermMode == DiffuseTermMode.Fabric)
            {
                AddSlot(new Vector1MaterialSlot(diffuseTermMode == DiffuseTermMode.Disney? PerceptualRoughnessSlotID : RoughnessSlotID, diffuseTermMode == DiffuseTermMode.Disney? kPerceptualRoughnessInputSlotName : kRoughnessInputSlotName, diffuseTermMode == DiffuseTermMode.Disney? kPerceptualRoughnessInputSlotName : kRoughnessInputSlotName, SlotType.Input, 0.5f));
                Slots.Add(diffuseTermMode == DiffuseTermMode.Disney? PerceptualRoughnessSlotID : RoughnessSlotID);
            }

            RemoveSlotsNameNotMatching(Slots, true);
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return FindSlot<NormalMaterialSlot>(NormalWSSlotID).RequiresNormal();
        }
        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return FindSlot<ViewDirectionMaterialSlot>(ViewDirWSSlotID).RequiresViewDirection();
        }
    }
}
