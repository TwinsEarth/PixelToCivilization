using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    public enum VGTiling
    {
        UV_INVALID = -1,
        UV0 = 0,
        UV1 = 1,
        UV2 = 2,
        UV3 = 3,
    }

    interface IMayRequireInputVGTiling
    {
        VGTiling RequiresInputVGTiling(ShaderStageCapability stageCapability = ShaderStageCapability.All);
        // It is recommended to use null for UV_INVALID.
        string GetVGTilingVariableName();
    }

    interface IMayRequireOutputVGTiling
    {
        // SlotId is for SubGraph to determine whether output is connected.
        VGTiling RequiresOutputVGTiling(int slotId = -1);

        // If the node is output node, it is essential to use interface below to return VGTiling's name.
        string GetVGTilingVariableName(int slotId = -1);
    }

    static class MayRequireVGTilingExtensions
    {
        public static VGTiling RequiresInputVGTiling(this MaterialSlot slot, ShaderStageCapability stageCapability)
        {
            var mayRequireVGTiling = slot as IMayRequireInputVGTiling;
            return mayRequireVGTiling?.RequiresInputVGTiling(stageCapability) ?? VGTiling.UV_INVALID;
        }

        public static VGTiling RequiresOutputVGTiling(this MaterialSlot slot)
        {
            if (slot.isConnected)
                return VGTiling.UV_INVALID;
            if (slot is UVMaterialSlot mayRequireVGTiling)
                return (VGTiling)mayRequireVGTiling.channel;
            return VGTiling.UV_INVALID;
        }

        public static string GetVGTilingVariableName(this MaterialSlot slot)
        {
            var mayRequireVGTiling = slot as IMayRequireInputVGTiling;
            return mayRequireVGTiling?.GetVGTilingVariableName();
        }
    }
}
