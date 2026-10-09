using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using System.Collections.Generic;
using System;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph
{    
    [Title("Input", "Lighting", "Additional Lights Loop End")]
    class AdditionalLightsLoopEndNode : LoopEnd, IGeneratesBodyCode
    {
        public AdditionalLightsLoopEndNode()
        {
            name = "Additional Lights Loop End";
            UpdateNodeAfterDeserialization();
        }
        public override bool hasPreview { get { return false; } }

        const string kAdditionalLightingInputSlotName = "AdditionalLighting";
        const string kOutputSlotName = "Additional Lighting";
        const int kAdditionalLightingInputSlotId = 0;
        const int OutputSlotId = 1;

        public override bool ExposeToSearcher(){ return false; }
        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kAdditionalLightingInputSlotId, kAdditionalLightingInputSlotName, kAdditionalLightingInputSlotName, SlotType.Input, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { kAdditionalLightingInputSlotId, OutputSlotId });
        }
        
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
            {
                sb.AppendLine("#if defined(_ADDITIONAL_LIGHTS)");
                sb.AppendLine("outColor{1} += {0};", GetSlotValue(kAdditionalLightingInputSlotId, generationMode), loopIndex);
                sb.AppendLine("}");  // if matching light layer
                sb.AppendLine("#if USE_FORWARD_PLUS");
                sb.AppendLine("}");  // for loop
                sb.AppendLine("#else");
                sb.AppendLine("}");  // for loop
                sb.AppendLine("#endif"); // use forward plus
                sb.AppendLine("$precision3 {0} = outColor{1};", GetVariableNameForSlot(OutputSlotId), loopIndex);
                sb.AppendLine("#else");   // if additional lights
                sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(OutputSlotId));
                sb.AppendLine("#endif "); // if additional lights
            }
            else
            {
               // Output zeros
               sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(OutputSlotId));
            }
        }
    }
}
