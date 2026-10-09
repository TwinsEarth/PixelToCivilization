using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Sample Texture 2D Array")]
    class SampleTexture2DArrayNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV, IMayRequireVG, IMayRequireInputVGTiling
    {
        public const int OutputSlotRGBAId = 0;
        public const int OutputSlotRId = 4;
        public const int OutputSlotGId = 5;
        public const int OutputSlotBId = 6;
        public const int OutputSlotAId = 7;
        public const int TextureInputId = 1;
        public const int UVInput = 2;
        public const int SamplerInput = 3;
        public const int IndexInputId = 8;
        public const int MipBiasInput = 9;
        public const int LodInput = 10;
        public const int DdxInput = 11;
        public const int DdyInput = 12;

        const string kTextureInputName = "Texture Array";
        const string kUVInputName = "UV";
        const string kSamplerInputName = "Sampler";
        const string kIndexInputName = "Index";

        RGBANodeOutput m_RGBAPins = RGBANodeOutput.NewDefault();
        Mip2DSamplingInputs m_Mip2DSamplingInputs = Mip2DSamplingInputs.NewDefault();
        ShaderStageCapability m_StageCapability = ShaderStageCapability.None;

        public override bool hasPreview { get { return true; } }

        public SampleTexture2DArrayNode()
        {
            name = "Sample Texture 2D Array";
            synonyms = new string[] { "stack", "pile", "tex2darray" };
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private bool m_EnableGlobalMipBias = false;
        internal bool enableGlobalMipBias
        {
            set { m_EnableGlobalMipBias = value; }
            get { return m_EnableGlobalMipBias; }
        }

        [SerializeField]
        private Texture2DMipSamplingMode m_MipSamplingMode = Texture2DMipSamplingMode.Standard;
        internal Texture2DMipSamplingMode mipSamplingMode
        {
            set { m_MipSamplingMode = value; UpdateMipSamplingModeInputs(); }
            get { return m_MipSamplingMode; }
        }

        private void UpdateMipSamplingModeInputs()
        {
            var capabilities = ShaderStageCapability.Fragment;
            if (m_MipSamplingMode == Texture2DMipSamplingMode.LOD || m_MipSamplingMode == Texture2DMipSamplingMode.Gradient)
                capabilities |= ShaderStageCapability.Vertex;

            m_RGBAPins.SetCapabilities(capabilities);

            m_Mip2DSamplingInputs = MipSamplingModesUtils.CreateMip2DSamplingInputs(
                this, m_MipSamplingMode, m_Mip2DSamplingInputs, MipBiasInput, LodInput, DdxInput, DdyInput);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            m_RGBAPins.CreateNodes(this, ShaderStageCapability.None, OutputSlotRGBAId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId);
            AddSlot(new Texture2DArrayInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            AddSlot(new Vector1MaterialSlot(IndexInputId, kIndexInputName, kIndexInputName, SlotType.Input, 0));
            AddSlot(new UVMaterialSlot(UVInput, kUVInputName, kUVInputName, UVChannel.UV0));
            AddSlot(new SamplerStateMaterialSlot(SamplerInput, kSamplerInputName, kSamplerInputName, SlotType.Input));
            UpdateMipSamplingModeInputs();
            RemoveSlotsNameNotMatching(new[] { OutputSlotRGBAId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId, TextureInputId, IndexInputId, UVInput, SamplerInput, MipBiasInput, LodInput, DdxInput, DdyInput });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var uvName = GetSlotValue(UVInput, generationMode);
            var indexName = GetSlotValue(IndexInputId, generationMode);
            var uvSlot = FindSlot<MaterialSlot>(UVInput);
            VGTiling vgTiling = RequiresInputVGTiling(m_StageCapability);

            //Sampler input slot
            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInput);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);

            var id = GetSlotValue(TextureInputId, generationMode);

            // We just generate VG version for standard mode.
            bool isRequireVG = m_MipSamplingMode == Texture2DMipSamplingMode.Standard && vgTiling != VGTiling.UV_INVALID;
            if (isRequireVG)
            {
                sb.AppendLine("#ifdef UNITY_GPU_DRIVEN_PIPELINE");

                string lodVariables;
                if (uvSlot.isConnected)
                    lodVariables = string.Format("float4 grad{0} = {1}.xyxy * IN.ddxddy{2};", GetVariableNameForSlot(OutputSlotRGBAId), GetVGTilingVariableName(), vgTiling);
                else
                    lodVariables = string.Format("float4 grad{0} = float4(1, 1, 1, 1) *  IN.ddxddy{1};", GetVariableNameForSlot(OutputSlotRGBAId), vgTiling);

                sb.AppendLine(lodVariables);

                var lodResult = string.Format("$precision4 {0} = {1}({2}.tex, {3}.samplerstate, {4}, {5}, grad{0}.xy, grad{0}.zw);"
                    , GetVariableNameForSlot(OutputSlotRGBAId)
                    , MipSamplingModesUtils.Get2DTextureSamplingMacro(m_MipSamplingMode, usePlatformMacros: !m_EnableGlobalMipBias, isArray: true, isRequireVG: true)
                    , id
                    , edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id
                    , uvName
                    , indexName);

                sb.AppendLine(lodResult);

                sb.AppendLine("#else");
            }

            var result = string.Format("$precision4 {0} = {1}({2}.tex, {3}.samplerstate, {4}, {5} {6});"
                , GetVariableNameForSlot(OutputSlotRGBAId)
                , MipSamplingModesUtils.Get2DTextureSamplingMacro(m_MipSamplingMode, usePlatformMacros: !m_EnableGlobalMipBias, isArray: true)
                , id
                , edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id
                , uvName
                , indexName
                , MipSamplingModesUtils.GetSamplerMipArgs(this, m_MipSamplingMode, m_Mip2DSamplingInputs, generationMode));

            sb.AppendLine(result);

            if (isRequireVG)
                sb.AppendLine("#endif");

            sb.AppendLine(string.Format("$precision {0} = {1}.r;", GetVariableNameForSlot(OutputSlotRId), GetVariableNameForSlot(OutputSlotRGBAId)));
            sb.AppendLine(string.Format("$precision {0} = {1}.g;", GetVariableNameForSlot(OutputSlotGId), GetVariableNameForSlot(OutputSlotRGBAId)));
            sb.AppendLine(string.Format("$precision {0} = {1}.b;", GetVariableNameForSlot(OutputSlotBId), GetVariableNameForSlot(OutputSlotRGBAId)));
            sb.AppendLine(string.Format("$precision {0} = {1}.a;", GetVariableNameForSlot(OutputSlotAId), GetVariableNameForSlot(OutputSlotRGBAId)));
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

        public bool RequiresVG(ShaderStageCapability stageCapability)
        {
            m_StageCapability = stageCapability;
            return (stageCapability & ShaderStageCapability.Fragment) != 0;
        }

        public VGTiling RequiresInputVGTiling(ShaderStageCapability stageCapability)
        {
            return FindSlot<MaterialSlot>(UVInput).RequiresInputVGTiling(stageCapability);
        }

        public string GetVGTilingVariableName()
        {
            return FindSlot<MaterialSlot>(UVInput).GetVGTilingVariableName();
        }
    }
}
