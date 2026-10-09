using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    enum TextureType
    {
        Default,
        Normal
    };

    [FormerName("UnityEditor.ShaderGraph.Texture2DNode")]
    [Title("Input", "Texture", "Sample Texture 2D")]
    class SampleTexture2DNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV, IMayRequireVG, IMayRequireInputVGTiling
    {
        public const int OutputSlotRGBAId = 0;
        public const int OutputSlotRId = 4;
        public const int OutputSlotGId = 5;
        public const int OutputSlotBId = 6;
        public const int OutputSlotAId = 7;
        public const int TextureInputId = 1;
        public const int UVInput = 2;
        public const int SamplerInput = 3;
        public const int MipBiasInput = 8;
        public const int LodInput = 9;
        public const int DdxInput = 10;
        public const int DdyInput = 11;

        const string kTextureInputName = "Texture";
        const string kUVInputName = "UV";
        const string kSamplerInputName = "Sampler";
        RGBANodeOutput m_RGBAPins = RGBANodeOutput.NewDefault();
        Mip2DSamplingInputs m_Mip2DSamplingInputs = Mip2DSamplingInputs.NewDefault();
        // For gaining runtime ShaderStageCapability.
        ShaderStageCapability m_StageCapability = ShaderStageCapability.None;

        public override bool hasPreview { get { return true; } }

        public SampleTexture2DNode()
        {
            name = "Sample Texture 2D";
            synonyms = new string[] { "tex2d" };
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private TextureType m_TextureType = TextureType.Default;

        [EnumControl("Type")]
        public TextureType textureType
        {
            get { return m_TextureType; }
            set
            {
                if (m_TextureType == value)
                    return;

                m_TextureType = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }

        [SerializeField]
        private NormalMapSpace m_NormalMapSpace = NormalMapSpace.Tangent;

        [EnumControl("Space")]
        public NormalMapSpace normalMapSpace
        {
            get { return m_NormalMapSpace; }
            set
            {
                if (m_NormalMapSpace == value)
                    return;

                m_NormalMapSpace = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        private bool m_EnableGlobalMipBias = true;
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
            AddSlot(new Texture2DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            AddSlot(new UVMaterialSlot(UVInput, kUVInputName, kUVInputName, UVChannel.UV0));
            AddSlot(new SamplerStateMaterialSlot(SamplerInput, kSamplerInputName, kSamplerInputName, SlotType.Input));
            UpdateMipSamplingModeInputs();
            RemoveSlotsNameNotMatching(new[] { OutputSlotRGBAId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId, TextureInputId, UVInput, SamplerInput, MipBiasInput, LodInput, DdxInput, DdyInput });
        }

        public override void Setup()
        {
            base.Setup();
            var textureSlot = FindInputSlot<Texture2DInputMaterialSlot>(TextureInputId);
            textureSlot.defaultType = (textureType == TextureType.Normal ? Texture2DShaderProperty.DefaultType.NormalMap : Texture2DShaderProperty.DefaultType.White);
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var uvName = GetSlotValue(UVInput, generationMode);
            var uvSlot = FindSlot<UVMaterialSlot>(UVInput);
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
                // VBuffer AlphaClip Pass
                sb.AppendLine("#if SHADERPASS == SHADERPASS_ALPHA_MESH_SELECTION || SHADERPASS == SHADERPASS_VBUFFER_ALPHA_CLIP");

                var alphaClipResult = string.Format("$precision4 {0} = {1}({2}.tex, {3}.samplerstate, {2}.GetTransformedUV({4}) {5});"
                    , GetVariableNameForSlot(OutputSlotRGBAId)
                    , MipSamplingModesUtils.Get2DTextureSamplingMacro(m_MipSamplingMode, usePlatformMacros: !m_EnableGlobalMipBias, isArray: false)
                    , id
                    , edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id
                    , uvName
                    , MipSamplingModesUtils.GetSamplerMipArgs(this, m_MipSamplingMode, m_Mip2DSamplingInputs, generationMode));

                sb.AppendLine(alphaClipResult);

                sb.AppendLine("#else");

                string lodVariables;
                if (uvSlot.isConnected)
                    lodVariables = string.Format("float4 grad{0} = {1}.xyxy * IN.ddxddy{2};", GetVariableNameForSlot(OutputSlotRGBAId), GetVGTilingVariableName(), vgTiling);
                else
                    lodVariables = string.Format("float4 grad{0} = {1}.scaleTranslate.xyxy *  IN.ddxddy{2};", GetVariableNameForSlot(OutputSlotRGBAId), id, vgTiling);

                sb.AppendLine(lodVariables);


                var lodResult = string.Format("$precision4 {0} = {1}({2}.tex, {3}.samplerstate, {2}.GetTransformedUV({4}), grad{0}.xy, grad{0}.zw);"
                    , GetVariableNameForSlot(OutputSlotRGBAId)
                    , MipSamplingModesUtils.Get2DTextureSamplingMacro(m_MipSamplingMode, usePlatformMacros: !m_EnableGlobalMipBias, isArray: false, isRequireVG: true)
                    , id
                    , edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id
                    , uvName);

                sb.AppendLine(lodResult);

                sb.AppendLine("#endif"); //#endif !SHADERPASS == SHADERPASS_ALPHA_MESH_SELECTION && !SHADERPASS == SHADERPASS_VBUFFER_ALPHA_CLIP

                sb.AppendLine("#else");
            }

            var result = string.Format("$precision4 {0} = {1}({2}.tex, {3}.samplerstate, {2}.GetTransformedUV({4}) {5});"
                , GetVariableNameForSlot(OutputSlotRGBAId)
                , MipSamplingModesUtils.Get2DTextureSamplingMacro(m_MipSamplingMode, usePlatformMacros: !m_EnableGlobalMipBias, isArray: false)
                , id
                , edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id
                , uvName
                , MipSamplingModesUtils.GetSamplerMipArgs(this, m_MipSamplingMode, m_Mip2DSamplingInputs, generationMode));

            sb.AppendLine(result);

            if (isRequireVG)
                sb.AppendLine("#endif");

            if (textureType == TextureType.Normal)
            {
                if (normalMapSpace == NormalMapSpace.Tangent)
                {
                    sb.AppendLine(string.Format("{0}.rgb = UnpackNormal({0});", GetVariableNameForSlot(OutputSlotRGBAId)));
                }
                else
                {
                    sb.AppendLine(string.Format("{0}.rgb = UnpackNormalRGB({0});", GetVariableNameForSlot(OutputSlotRGBAId)));
                }
            }

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
            // This if for itself to determine runtime ShaderStageCapability to
            // generate correct codes.
            m_StageCapability = stageCapability;
            // This is for SubGraphNode to determine whether SubGraphNode includes
            // special nodes for VG.
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
