using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "For Loop End")]
    class ForLoopEndNode : LoopEnd, IGeneratesBodyCode
    {
        public ForLoopEndNode()
        {
            name = "For Loop End";
            synonyms = new string[] { "forloop", "loop"};
            UpdateNodeAfterDeserialization();
        }
        public override bool hasPreview => false;

        public enum OutputMode
        {
            [InspectorName("Accumulation")]
            Accum,
            [InspectorName("Override")]
            Override
        }
        [SerializeField]
        private OutputMode m_OutputMode = OutputMode.Accum;

        [EnumControl("Output Mode")]
        public OutputMode outptuMode
        {
            get { return m_OutputMode; }
            set
            {
                if (m_OutputMode == value)
                    return;

                m_OutputMode = value;
                Dirty(ModificationScope.Node);
            }
        }


        [SerializeField]
        [Range(1, 5)]
        int m_OutputSlots = 1;
        [SerializeField]
        List<OutputType> m_OutputTypes = Enumerable.Repeat(OutputType.Vector4, 5).ToList();

        public int outputSlots
        {
            get => m_OutputSlots;
            set
            {
                m_OutputSlots = value;
            }
        }

        public List<OutputType> outputTypes
        {
            get => m_OutputTypes;
            set => m_OutputTypes = value;
            
        }
        
        // Input
        List<string> kForLoopInputSlotName = new List<string>
        { 
           "In 1",
           "In 2",
           "In 3",
           "In 4",
           "In 5"
        };
        const string kForLoopContinueSlotName = "Continue";
        const string kForLoopBreakSlotName = "Break";
        const int kInputSlotBias = 0;
        const int kForLoopContinueSlotId = 5;
        const int kForLoopBreakSlotId = 6;

        // Output
        List<string> kOutputSlotName = new List<string>
        { 
           "Out 1",
           "Out 2",
           "Out 3",
           "Out 4",
           "Out 5"
        };
        const int kOutputSlotBias = 7;

        public override bool ExposeToSearcher(){ return false; }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            List<int> Slots = new List<int>();
            // Input
            for (int i = 0; i < outputSlots; i++)
            {
                switch (outputTypes[i])
                {
                    case OutputType.Vector4:
                        AddSlot(new Vector4MaterialSlot(kInputSlotBias + i, kForLoopInputSlotName[i],  kForLoopInputSlotName[i], SlotType.Input, Vector4.zero));
                        break;
                    case OutputType.Vector3:
                        AddSlot(new Vector3MaterialSlot(kInputSlotBias + i, kForLoopInputSlotName[i],  kForLoopInputSlotName[i], SlotType.Input, Vector3.zero));
                        break;
                    case OutputType.Vector2:
                        AddSlot(new Vector2MaterialSlot(kInputSlotBias + i, kForLoopInputSlotName[i],  kForLoopInputSlotName[i], SlotType.Input, Vector2.zero));
                        break;
                    case OutputType.Float:
                        AddSlot(new Vector1MaterialSlot(kInputSlotBias + i, kForLoopInputSlotName[i],  kForLoopInputSlotName[i], SlotType.Input, 0));
                        break;
                }
                Slots.Add(kInputSlotBias + i);
            }

            // Output
            for (int i = 0; i < outputSlots; i++)
            {
                switch (outputTypes[i])
                {
                    case OutputType.Vector4:
                        AddSlot(new Vector4MaterialSlot(kOutputSlotBias + i, kOutputSlotName[i],  kOutputSlotName[i], SlotType.Output, Vector4.zero));
                        break;
                    case OutputType.Vector3:
                        AddSlot(new Vector3MaterialSlot(kOutputSlotBias + i, kOutputSlotName[i],  kOutputSlotName[i], SlotType.Output, Vector3.zero));
                        break;
                    case OutputType.Vector2:
                        AddSlot(new Vector2MaterialSlot(kOutputSlotBias + i, kOutputSlotName[i],  kOutputSlotName[i], SlotType.Output, Vector2.zero));
                        break;
                    case OutputType.Float:
                        AddSlot(new Vector1MaterialSlot(kOutputSlotBias + i, kOutputSlotName[i],  kOutputSlotName[i], SlotType.Output, 0));
                        break;
                }
                Slots.Add(kOutputSlotBias + i);
            }

            RemoveSlotsNameNotMatching(Slots, true);
            AddSlot(new BooleanMaterialSlot(kForLoopContinueSlotId, kForLoopContinueSlotName, kForLoopContinueSlotName, SlotType.Input, false));
            AddSlot(new BooleanMaterialSlot(kForLoopBreakSlotId, kForLoopBreakSlotName, kForLoopBreakSlotName, SlotType.Input, false));
            Slots.Add(kForLoopContinueSlotId);
            Slots.Add(kForLoopBreakSlotId);
            RemoveSlotsNameNotMatching(Slots, true);

            if (owner?.loopStartNodes.Count > 0)
            {
                var startNode = owner.GetLoopStartNode(loopIndex);
                startNode.UpdateNodeAfterDeserialization();
                startNode.Dirty(ModificationScope.Node);
            }
        }
        
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                for (int i = 0; i < outputSlots; i++)
                {
                    if (outptuMode == OutputMode.Accum)
                        sb.AppendLine("outColor{1}_{2} += {0};", GetSlotValue(kInputSlotBias + i, generationMode), loopIndex, i);
                    else
                        sb.AppendLine("outColor{1}_{2} = {0};", GetSlotValue(kInputSlotBias + i, generationMode), loopIndex, i);
                }
                sb.AppendLine("if ({0})", GetSlotValue(kForLoopContinueSlotId, generationMode));
                sb.AppendLine("{continue;}");
                sb.AppendLine("if ({0})", GetSlotValue(kForLoopBreakSlotId, generationMode));
                sb.AppendLine("{break;}");
                sb.AppendLine("}");

                for (int i = 0; i < outputSlots; i++)
                {
                    AppendOutputLines(sb, outputTypes[i], GetVariableNameForSlot(kOutputSlotBias + i), string.Format("outColor{0}_{1}", loopIndex, i));
                }
            }
            else
            {
               // Output zeros
                for (int i = 0; i < outputSlots; i++)
                {
                    AppendOutputLines(sb, outputTypes[i], GetVariableNameForSlot(kOutputSlotBias + i), "0");
                }
            }
        }

        void AppendOutputLines(ShaderStringBuilder sb, OutputType outputType, string name, string value)
        {
            switch (outputType)
            {
                case OutputType.Vector4:
                    sb.AppendLine("$precision4 {0} = {1};", name, value);
                    break;
                case OutputType.Vector3:
                    sb.AppendLine("$precision3 {0} = {1};", name, value);
                    break;
                case OutputType.Vector2:
                    sb.AppendLine("$precision2 {0} = {1};", name, value);
                    break;
                case OutputType.Float:
                    sb.AppendLine("$precision {0} = {1};", name, value);
                    break; 
            }  
        }
        public enum OutputType
        {
            Vector4 = 0,
            Vector3 = 1,
            Vector2 = 2,
            Float = 3
        }
    }
}
