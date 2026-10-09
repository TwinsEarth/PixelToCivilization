using System;
using UnityEditor.Graphing;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    public class PortalNodePropertyDrawer
    {
        [SGPropertyDrawer(typeof(LocalVariableRegisterNode))]
        public class LocalVariableRegisterNodePropertyDrawer : AbstractMaterialNodePropertyDrawer
        {
            internal override void AddCustomNodeProperties(VisualElement parentElement, AbstractMaterialNode nodeBase, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
            {
                var node = nodeBase as LocalVariableRegisterNode;
                var variableNameField = new TextField { value = node.variableName, multiline = false };
                var propertyRow = new PropertyRow(new Label("Name"));
                propertyRow.Add(variableNameField, (field) =>
                {
                    field.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue.Equals(node.variableName))
                            return;

                        setNodesAsDirtyCallback?.Invoke();
                        node.owner.owner.RegisterCompleteObjectUndo("Change variable name");
                        updateNodeViewsCallback?.Invoke();
                    });

                    field.RegisterCallback<FocusOutEvent>(evt =>
                    {   
                        if (field.value.Equals(node.variableName))
                            return;

                        field.value = Regex.Replace(field.value, @"\s+", "_");          // replace whitespace with underscore
                        field.value = Regex.Replace(field.value, @"[^a-zA-Z_]", "");    // clean invalid characters
                        node.customizedVar = field.value.Equals("") ? false : true;
                        node.variableName = field.value.Equals("") ? "_" + node.objectId : field.value;
                        node.owner.RegistLocalVariable(node.owner.localVariables, node.owner.registerIds, node.objectId, node.variableName);
                        node.owner.ValidateGraph();
                        node.Dirty(ModificationScope.Node);
                        node.owner.getVarNodes.ForEach(n =>
                        {
                            n.customizedVar = n.variableName.Equals(node.previousVariableName) ? node.customizedVar : n.customizedVar;
                            n.variableName = n.variableName.Equals(node.previousVariableName) ? node.variableName : n.variableName;
                            n.UpdateNodeName();
                            updateNodeViewsCallback?.Invoke();
                            n.owner.ValidateGraph();
                            n.Dirty(ModificationScope.Node);
                        });
                        node.previousVariableName = node.variableName;
                    });
                });
                parentElement.Add(propertyRow);
            }
        }
        
        [SGPropertyDrawer(typeof(GetLocalVariableNode))]
        public class GetLocalVariableNodePropertyDrawer : AbstractMaterialNodePropertyDrawer
        {
            internal override void AddCustomNodeProperties(VisualElement parentElement, AbstractMaterialNode nodeBase, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
            {
                var node = nodeBase as GetLocalVariableNode;
                var variableNameField = new PopupField<string>(node.owner.localVariables, node.varIndex);
                var propertyRow = new PropertyRow(new Label("Variable"));
                var initial = new HashSet<AbstractMaterialNode> { nodeBase };
                var results = new HashSet<AbstractMaterialNode>();
                if (node.owner.localVariables.Count > 0 && node.owner.localVariables.Contains(node.variableName))
                    node.customizedVar = ((LocalVariableRegisterNode)node.owner.GetNodeFromId(node.owner.registerIds[node.owner.localVariables.IndexOf(node.variableName)])).customizedVar;
                node.customizedVar = node.owner.localVariables.Contains(node.variableName) ? node.customizedVar : false;
                propertyRow.Add(variableNameField, (field) =>
                {
                    node.variableName = variableNameField.value != null ? variableNameField.value : "";
                    field.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue.Equals(node.variableName))
                            return;

                        // If newValue's register is in the propagated node list means detected circular loop, so report a warning and do nothing.
                        results.Clear();
                        PreviewManager.PropagateNodes(initial, PreviewManager.PropagationDirection.Downstream, results);
                        if (results.Contains(nodeBase.owner.GetLocalVariableRegisterNode(evt.newValue)))
                        {
                            UnityEngine.Debug.LogWarning("Detect circular loop, reverting change");
                            return;
                        }

                        setNodesAsDirtyCallback?.Invoke();
                        node.owner.owner.RegisterCompleteObjectUndo("Change variable name");
                        node.variableName = evt.newValue;
                        node.varIndex = field.index;
                        node.customizedVar = ((LocalVariableRegisterNode)node.owner.GetNodeFromId(node.owner.registerIds[field.index])).customizedVar;
                        updateNodeViewsCallback?.Invoke();
                        foreach (var edge in node.owner.GetEdges(node.GetSlotReference(GetLocalVariableNode.OutputSlotId)))
                        {
                            node.owner.RemoveEdge(edge);
                            node.owner.Connect(edge.outputSlot, edge.inputSlot);
                        }
                        node.owner.ValidateGraph();
                        node.Dirty(ModificationScope.Node);
                    });
                });
                parentElement.Add(propertyRow);
            }
        }
    }
}
