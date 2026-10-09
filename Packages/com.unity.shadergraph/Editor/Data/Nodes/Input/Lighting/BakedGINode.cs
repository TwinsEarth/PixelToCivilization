using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEditor.ShaderGraph.BakedGAbstractMaterialNode")]
    [FormerName("UnityEditor.ShaderGraph.LightProbeNode")]
    [Title("Input", "Lighting", "Baked GI")]
    class BakedGINode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequirePixelPosition, IMayRequirePosition, IMayRequireNormal, IMayRequireMeshUV, IMayRequireVG, IMayRequireInputVGTiling
    {
        public override bool hasPreview { get { return false; } }

        public BakedGINode()
        {
            name = "Baked GI";
            synonyms = new string[] { "global illumination" };
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private bool m_ApplyScaling = true;

        [ToggleControl("Apply Lightmap Scaling")]
        public ToggleData applyScaling
        {
            get { return new ToggleData(m_ApplyScaling); }
            set
            {
                if (m_ApplyScaling == value.isOn)
                    return;
                m_ApplyScaling = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        const int kNormalWSInputSlotId = 0;
        const string kNormalWSInputSlotName = "NormalWS";

        const int kOutputSlotId = 1;
        const string kOutputSlotName = "Out";

        const int kPositionWSInputSlotId = 2;
        const string kPositionWSInputSlotName = "PositionWS";

        const int kStaticUVInputSlotId = 3;
        const string kStaticUVInputSlotName = "StaticUV";

        const int kDynamicUVInputSlotId = 4;
        const string kDynamicUVInputSlotName = "DynamicUV";

        ShaderStageCapability m_StageCapability = ShaderStageCapability.None;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new NormalMaterialSlot(kNormalWSInputSlotId, kNormalWSInputSlotName, kNormalWSInputSlotName, CoordinateSpace.World));
            AddSlot(new PositionMaterialSlot(kPositionWSInputSlotId, kPositionWSInputSlotName, kPositionWSInputSlotName, CoordinateSpace.World));
            AddSlot(new UVMaterialSlot(kStaticUVInputSlotId, kStaticUVInputSlotName, kStaticUVInputSlotName, UVChannel.UV1));
            AddSlot(new UVMaterialSlot(kDynamicUVInputSlotId, kDynamicUVInputSlotName, kDynamicUVInputSlotName, UVChannel.UV2));

            // Output
            AddSlot(new Vector3MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kNormalWSInputSlotId,
                kPositionWSInputSlotId,
                kStaticUVInputSlotId,
                kDynamicUVInputSlotId,

                // Output
                kOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals || generationMode == GenerationMode.VFX)
            {
                // If this node may be used in frag stage, we should generate code about VG.
                bool requireVG = (m_StageCapability & ShaderStageCapability.Fragment) != 0;
                if (requireVG)
                {
                    var staticUVSlot = FindSlot<UVMaterialSlot>(kStaticUVInputSlotId);
                    VGTiling vgTiling = RequiresInputVGTiling(m_StageCapability);
                    string staticUVName = GetVariableNameForSlot(kStaticUVInputSlotId);
                    sb.AppendLine("#ifdef UNITY_GPU_DRIVEN_PIPELINE");
                    sb.AppendLine("#ifdef SHADER_STAGE_FRAGMENT");

                    if (staticUVSlot.isConnected)
                        sb.AppendLine("float4 grad{0} = IN.ddxddy{1} * {2}.xyxy;", staticUVName, GetVGTilingVariableName(), vgTiling);
                    else
                        sb.AppendLine("float4 grad{0} = IN.ddxddy{1};", staticUVName, vgTiling);
                    sb.AppendLine("$precision3 {7} = SHADERGRAPH_BAKED_GI({0}, {1}, IN.{2}.xy, {3}, {4}, {5}, instance, materialOffset, grad{6}.xy, grad{6}.zw);",
                        GetSlotValue(kPositionWSInputSlotId, generationMode),
                        GetSlotValue(kNormalWSInputSlotId, generationMode),
                        ShaderGeneratorNames.PixelPosition,
                        GetSlotValue(kStaticUVInputSlotId, generationMode),
                        GetSlotValue(kDynamicUVInputSlotId, generationMode),
                        applyScaling.isOn ? "true" : "false",
                        staticUVName,
                        GetVariableNameForSlot(kOutputSlotId));

                    sb.AppendLine("#else");

                    // If in SubShaderGraph used in vert stage, we should give some default values
                    // to avoid compile bug.
                    sb.AppendLine("$precision3 {6} = SHADERGRAPH_BAKED_GI({0}, {1}, IN.{2}.xy, {3}, {4}, {5}, (InstanceSubset)0, materialOffset, float2(0, 0), float2(0, 0));",
                        GetSlotValue(kPositionWSInputSlotId, generationMode),
                        GetSlotValue(kNormalWSInputSlotId, generationMode),
                        ShaderGeneratorNames.PixelPosition,
                        GetSlotValue(kStaticUVInputSlotId, generationMode),
                        GetSlotValue(kDynamicUVInputSlotId, generationMode),
                        applyScaling.isOn ? "true" : "false",
                        GetVariableNameForSlot(kOutputSlotId));

                    sb.AppendLine("#endif");
                    sb.AppendLine("#else");
                }

                sb.AppendLine("$precision3 {6} = SHADERGRAPH_BAKED_GI({0}, {1}, IN.{2}.xy, {3}, {4}, {5});",
                    GetSlotValue(kPositionWSInputSlotId, generationMode),
                    GetSlotValue(kNormalWSInputSlotId, generationMode),
                    ShaderGeneratorNames.PixelPosition,
                    GetSlotValue(kStaticUVInputSlotId, generationMode),
                    GetSlotValue(kDynamicUVInputSlotId, generationMode),
                    applyScaling.isOn ? "true" : "false",
                    GetVariableNameForSlot(kOutputSlotId));

                if (requireVG)
                    sb.AppendLine("#endif");
            }
            else
            {
                // Output zeros
                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kOutputSlotId));
            }
        }

        public bool RequiresPixelPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return true; // needed for APV sampling noise when TAA is used
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return FindSlot<PositionMaterialSlot>(kPositionWSInputSlotId).RequiresPosition();
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return FindSlot<NormalMaterialSlot>(kNormalWSInputSlotId).RequiresNormal();
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return FindSlot<UVMaterialSlot>(kStaticUVInputSlotId).RequiresMeshUV(channel) ||
                   FindSlot<UVMaterialSlot>(kDynamicUVInputSlotId).RequiresMeshUV(channel);
        }

        public bool RequiresVG(ShaderStageCapability stageCapability)
        {
            // This if for itself to determine runtime ShaderStageCapability to
            // generate correct codes.
            m_StageCapability = stageCapability;
            // This is for SubGraphNode to determine whether SubGraphNode includes
            // special nodes for VG.
            return (stageCapability & ShaderStageCapability.Fragment) != 0;
        }

        public VGTiling RequiresInputVGTiling(ShaderStageCapability stageCapability)
        {
            return FindSlot<MaterialSlot>(kStaticUVInputSlotId).RequiresInputVGTiling(stageCapability);
        }

        public string GetVGTilingVariableName()
        {
            return FindSlot<MaterialSlot>(kStaticUVInputSlotId).GetVGTilingVariableName();
        }
    }
}
