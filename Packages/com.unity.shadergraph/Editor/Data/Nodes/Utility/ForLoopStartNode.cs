using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "For Loop")]
    class ForLoopStartNode : LoopStart, IGeneratesBodyCode
    {


        public ForLoopStartNode()
        {
            name = "For Loop Start";
            synonyms = new string[] { "forloop", "loop" };
            UpdateNodeAfterDeserialization();
        }
        public override bool hasPreview => false;
        ForLoopEndNode m_EndNode;

        // output
        const string kIndexOutputSlotName = "Loop Index";
        const int kIndexOutputSlotId = 0;

        // input 
        const string kIterationsInputSlotName = "Iterations";
        const int kIterationsInputSlotId = 1;
        const int kHistorySlotBias = 2;
        List<string> kHistorySlotName = new List<string>
        { 
           "History 1",
           "History 2",
           "History 3",
           "History 4",
           "History 5"
        };

        public override bool ExposeToSearcher(){ return true; }
        public override void CreateGroup(ref GraphData graph)
        {
            GroupData groupData = new GroupData(){ title = "For Loop", loopGroup = true};
            graph.CreateGroup(groupData);
            graph.SetGroup(this as IGroupItem, groupData);
        }
        public override void CreateEndNode(ref GraphData graph)
        {
            var endNode = new ForLoopEndNode();
            var endNodeDS = endNode.drawState;
            endNodeDS.position = new Rect(drawState.position.position + new Vector2(400, 0), Vector2.zero);
            endNode.drawState = endNodeDS;
            endNode.loopIndex = loopIndex;
            graph.AddNode(endNode);
            graph.SetGroup(endNode, group);
            endNode.UpdateNodeAfterDeserialization();
            endNode.Dirty(ModificationScope.Node);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (owner != null && owner.loopEndNodes.Count > 0)
                m_EndNode = owner.GetLoopEndNode(loopIndex) as ForLoopEndNode;
            if (m_EndNode == null)
                return;
            if (generationMode == GenerationMode.ForReals)
            {
                for (int i = 0; i < m_EndNode.outputSlots; i++)
                {
                    AppendOutputLines(sb, m_EndNode.outputTypes[i], string.Format("outColor{0}_{1}", loopIndex, i), "0");
                }
                sb.AppendLine("for(int i = 0; i < {0}; i++)", GetSlotValue(kIterationsInputSlotId, generationMode));
                sb.AppendLine("{");
                sb.AppendLine("$precision {0} = i;", GetVariableNameForSlot(kIndexOutputSlotId));
                for (int i = 0; i < m_EndNode.outputSlots; i++)
                {
                    AppendOutputLines(sb, m_EndNode.outputTypes[i], GetVariableNameForSlot(kHistorySlotBias + i), string.Format("outColor{0}_{1}", loopIndex, i));
                }

            }
            else
            {
                sb.AppendLine("$precision {0} = 0;", GetVariableNameForSlot(kIndexOutputSlotId));
                for (int i = 0; i < m_EndNode.outputSlots; i++)
                {
                    AppendOutputLines(sb, m_EndNode.outputTypes[i], GetVariableNameForSlot(kHistorySlotBias + i), "0");
                }
            }
        }

        void AppendOutputLines(ShaderStringBuilder sb, ForLoopEndNode.OutputType outputType, string name, string value)
        {
            switch (outputType)
            {
                case ForLoopEndNode.OutputType.Vector4:
                    sb.AppendLine("$precision4 {0} = {1};", name, value);
                    break;
                case ForLoopEndNode.OutputType.Vector3:
                    sb.AppendLine("$precision3 {0} = {1};", name, value);
                    break;
                case ForLoopEndNode.OutputType.Vector2:
                    sb.AppendLine("$precision2 {0} = {1};", name, value);
                    break;
                case ForLoopEndNode.OutputType.Float:
                    sb.AppendLine("$precision {0} = {1};", name, value);
                    break; 
            }  
        }
        public sealed override void UpdateNodeAfterDeserialization()
        {
            List<int> Slots = new List<int>();
            AddSlot(new Vector1MaterialSlot(kIterationsInputSlotId, kIterationsInputSlotName, kIterationsInputSlotName, SlotType.Input, 5));
            Slots.Add(kIterationsInputSlotId);
            AddSlot(new Vector1MaterialSlot(kIndexOutputSlotId, kIndexOutputSlotName, kIndexOutputSlotName, SlotType.Output, 0));
            Slots.Add(kIndexOutputSlotId);

            if (owner?.loopEndNodes.Count > 0)
                m_EndNode = owner.GetLoopEndNode(loopIndex) as ForLoopEndNode;
            if (m_EndNode != null)
            {
                for (int i = 0; i < m_EndNode.outputSlots; i++)
                {
                    switch (m_EndNode.outputTypes[i])
                    {
                        case ForLoopEndNode.OutputType.Vector4:
                            AddSlot(new Vector4MaterialSlot(kHistorySlotBias + i, kHistorySlotName[i],  kHistorySlotName[i], SlotType.Output, Vector4.zero));
                            break;
                        case ForLoopEndNode.OutputType.Vector3:
                            AddSlot(new Vector3MaterialSlot(kHistorySlotBias + i, kHistorySlotName[i],  kHistorySlotName[i], SlotType.Output, Vector3.zero));
                            break;
                        case ForLoopEndNode.OutputType.Vector2:
                            AddSlot(new Vector2MaterialSlot(kHistorySlotBias + i, kHistorySlotName[i],  kHistorySlotName[i], SlotType.Output, Vector2.zero));
                            break;
                        case ForLoopEndNode.OutputType.Float:
                            AddSlot(new Vector1MaterialSlot(kHistorySlotBias + i, kHistorySlotName[i],  kHistorySlotName[i], SlotType.Output, 0));
                            break;
                    }
                    Slots.Add(kHistorySlotBias + i);
                }
            }
            RemoveSlotsNameNotMatching(Slots, true);
        }

    }
}
