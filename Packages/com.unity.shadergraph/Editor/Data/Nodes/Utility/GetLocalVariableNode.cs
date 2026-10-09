using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Get Local Variable")]
    class GetLocalVariableNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public GetLocalVariableNode()
        {
            name = k_DefaultNodeName;
            synonyms = new string[] { "get" };
            UpdateNodeName();
            UpdateNodeAfterDeserialization();
        }

        public override bool canSetPrecision => false;
        public override bool hasPreview => false;
        
        [SerializeField]
        string m_Variable = "";

        [SerializeField]
        int variablesIndex = 0;
        
        [SerializeField]
        bool m_CustomizedVar = false;

        public string variableName
        {
            get => m_Variable;
            set
            {
                m_Variable = value;
                UpdateNodeName();
                UpdateNodeAfterDeserialization();

                Dirty(ModificationScope.Node);
            }
        }

        public int varIndex
        {
            get => variablesIndex;
            set
            {
                variablesIndex = value;
                UpdateNodeName();
                UpdateNodeAfterDeserialization(); 

                Dirty(ModificationScope.Node);
            }
        }

        public bool customizedVar 
        { 
            get => m_CustomizedVar; 
            set
            {
                m_CustomizedVar = value;
                UpdateNodeName();

                Dirty(ModificationScope.Node);
            }
        }

        public const int OutputSlotId = 0;
        const string k_DefaultNodeName = "Get Local Variable";

        public static GetLocalVariableNode Create(GraphData graph,  Rect absolutePosition, SlotReference inputRef, GroupData group, string portalPairName)
        {
            var nodeData = new GetLocalVariableNode();
            nodeData.group = group;
            nodeData.variableName = portalPairName;
            nodeData.SetPosition(absolutePosition);
            nodeData.varIndex = graph.localVariables.IndexOf(portalPairName);

            var nodeOutSlotRef = nodeData.GetSlotReference(GetLocalVariableNode.OutputSlotId);
            
            graph.owner.RegisterCompleteObjectUndo("Add Portal Node");
            graph.AddNode(nodeData);
            
            graph.ReConnectPortals(inputRef, nodeOutSlotRef);

            return nodeData;
        }
        
        public void SetPosition(Rect pos)
        {
            var temp = drawState;
            Vector2 offset = new Vector2(pos.width + 240, 0);
            temp.position = new Rect(pos.position + offset, Vector2.zero);
            drawState = temp;
        }
        
        public void UpdateNodeName() => name = customizedVar ? "Get " + m_Variable + " (Get Local Variable)" : k_DefaultNodeName;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, "In", "", SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            UpdateNodeName();
        }

        public sealed override void EvaluateDynamicMaterialSlots(List<MaterialSlot> inputSlots, List<MaterialSlot> outputSlots)
        {
            if (owner != null && owner.localVariables.Any() && owner.localVariables.Count > variablesIndex)
            {
                m_Variable = owner.localVariables[variablesIndex];
                UpdateNodeName();
                var registNode = owner.GetLocalVariableRegisterNode(m_Variable);
                if (registNode != null)
                {
                    inputSlots.Clear();
                    inputSlots.Add(registNode.FindInputSlot<MaterialSlot>(LocalVariableRegisterNode.InputSlotId));
                    if (FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType != registNode.FindInputSlot<MaterialSlot>(LocalVariableRegisterNode.InputSlotId).concreteValueType)
                        UpdateNodeAfterDeserialization();
                }
            }
            base.EvaluateDynamicMaterialSlots(inputSlots, outputSlots);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var outputValue = GetSlotValue(OutputSlotId, generationMode);
            var valueType = FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString();
            if (owner != null  && owner.localVariables.Any() && owner.localVariables.Count > variablesIndex)
            {
                m_Variable = owner.localVariables[variablesIndex];
                UpdateNodeName();
                var registNode = owner.GetLocalVariableRegisterNode(m_Variable);
                if (registNode != null)
                    valueType = registNode.FindInputSlot<MaterialSlot>(LocalVariableRegisterNode.InputSlotId).concreteValueType.ToShaderString();
                sb.AppendLine("{0} {1} = {2};", valueType, outputValue, Regex.Replace(m_Variable, " ", "_"));
            }
            else
            {
                sb.AppendLine("{0} {1} = {2};", valueType, outputValue, 0);
            }
        }
    }
}
