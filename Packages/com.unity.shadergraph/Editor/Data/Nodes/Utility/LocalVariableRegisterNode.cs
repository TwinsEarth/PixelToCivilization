using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Rendering;
using System.Text.RegularExpressions;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Local Variable Register")]
    class LocalVariableRegisterNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public LocalVariableRegisterNode()
        {
            name = k_DefaultNodeName;
            synonyms = new string[] { "register", "custom", "set" };
            m_Variable = "_" + objectId;
            previousVariableName = m_Variable;
            UpdateNodeName();
            UpdateNodeAfterDeserialization();
        }
        
        public override bool canSetPrecision => false;
        public override bool hasPreview => false;

        [SerializeField]
        string m_Variable = "";
        [SerializeField]
        bool m_CustomizedVar = false;
        [SerializeField]
        bool m_ConflictRegister = false;
        public bool conflictRegister { get => m_ConflictRegister; set => m_ConflictRegister = value; }
        public string previousVariableName;

        public string variableName
        {
            get => m_Variable;
            set
            {
                m_Variable = value;
                UpdateNodeName();
                UpdateNodeAfterDeserialization();
            }
        }

        public bool customizedVar { get => m_CustomizedVar; set => m_CustomizedVar = value; }

        public const int InputSlotId = 0;
        const string k_DefaultNodeName = "Local Variable Register";

        public static LocalVariableRegisterNode Create(GraphData graph,  Rect absolutePosition, SlotReference outputRef, GroupData group, out string portalPairName)
        {
            var nodeData = new LocalVariableRegisterNode();
            nodeData.SetPosition(absolutePosition);
            nodeData.group = group;

            var nodeInSlotRef = nodeData.GetSlotReference(LocalVariableRegisterNode.InputSlotId);
            
            graph.owner.RegisterCompleteObjectUndo("Add Portal Node");
            graph.AddNode(nodeData);
            
            graph.ReConnectPortals(outputRef, nodeInSlotRef);

            portalPairName = nodeData.variableName;

            return nodeData;
        }
        
        public void SetPosition(Rect pos)
        {
            var temp = drawState;
            Vector2 offset = new Vector2(pos.width + 30, 0);
            temp.position = new Rect(pos.position + offset, Vector2.zero);
            drawState = temp;
        }

        void UpdateNodeName() => name = customizedVar ? "Set " + m_Variable + " (Local Variable Register)" : k_DefaultNodeName;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, "In", "", SlotType.Input, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId });
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            UpdateNodeName();
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputValue = GetSlotValue(InputSlotId, generationMode);
            sb.AppendLine("{0} {1} = {2};", FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType.ToShaderString(), Regex.Replace(m_Variable, " ", "_"), inputValue);
        }

        const string k_LocalVariableRegisterAlreadyExistsErrorMessage = "This variable has already registed";

        public override void ValidateNode()
        {
            base.ValidateNode();
            if (m_ConflictRegister)
                owner.messageManager?.AddOrAppendError(owner, objectId, new ShaderMessage(k_LocalVariableRegisterAlreadyExistsErrorMessage, (ShaderCompilerMessageSeverity)0));
        }
    }
}
