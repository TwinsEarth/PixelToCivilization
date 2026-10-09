using System;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class UVMaterialSlot : Vector2MaterialSlot, IMayRequireMeshUV, IMayRequireInputVGTiling
    {
        [SerializeField]
        UVChannel m_Channel = UVChannel.UV0;

        public UVChannel channel
        {
            get { return m_Channel; }
            set { m_Channel = value; }
        }

        public override bool isDefaultValue => channel == UVChannel.UV0;

        public UVMaterialSlot()
        { }

        public UVMaterialSlot(int slotId, string displayName, string shaderOutputName, UVChannel channel,
                              ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector2.zero, stageCapability, hidden: hidden)
        {
            this.channel = channel;
        }

        public override VisualElement InstantiateControl()
        {
            return new UVSlotControlView(this);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return string.Format("IN.{0}.xy", channel.GetUVName());
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            if (isConnected)
                return false;

            return m_Channel == channel;
        }

        public VGTiling RequiresInputVGTiling(ShaderStageCapability stageCapability)
        {
            if ((stageCapability & ShaderStageCapability.Fragment) == 0)
                return VGTiling.UV_INVALID;
            var edges = owner.owner.GetEdges(slotReference);
            if (edges.Any() == false)
                return (VGTiling)m_Channel;
            var slot = edges.First().outputSlot;
            var node = slot.node as IMayRequireOutputVGTiling;
            return node?.RequiresOutputVGTiling(slot.slotId) ?? VGTiling.UV_INVALID;
        }

        public string GetVGTilingVariableName()
        {
            var edges = owner.owner.GetEdges(slotReference);
            if (edges.Any() == false)
                return null;
            var slot = edges.First().outputSlot;
            var node = slot.node as IMayRequireOutputVGTiling;
            return node?.GetVGTilingVariableName(slot.slotId);
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as UVMaterialSlot;
            if (slot != null)
                channel = slot.channel;
        }
    }
}
