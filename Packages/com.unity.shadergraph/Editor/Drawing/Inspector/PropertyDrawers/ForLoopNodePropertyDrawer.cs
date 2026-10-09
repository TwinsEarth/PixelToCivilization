using System;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(ForLoopEndNode))]
    class ForLoopNodePropertyDrawer : AbstractMaterialNodePropertyDrawer
    {   


        internal override void AddCustomNodeProperties(VisualElement parentElement, AbstractMaterialNode nodeBase, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {

            var node = nodeBase as ForLoopEndNode;

            List<EnumField> enumFields = new List<EnumField>();
            for(int i = 0; i < node.outputTypes.Count; i++)
            {
                enumFields.Add(new EnumField("Slot " + (i + 1), node.outputTypes[i]){style = {marginLeft = 30}});
            }

            foreach(var enumField in enumFields)
            {
                enumField.RegisterValueChangedCallback(evt=>{
                    if (((ForLoopEndNode.OutputType)evt.newValue) == node.outputTypes[enumFields.IndexOf(enumField)])
                        return;
                    setNodesAsDirtyCallback?.Invoke();
                    node.owner.owner.RegisterCompleteObjectUndo("Change output types");
                    node.outputTypes[enumFields.IndexOf(enumField)] = (ForLoopEndNode.OutputType)evt.newValue;
                    updateNodeViewsCallback?.Invoke();
                    node.UpdateNodeAfterDeserialization();
                    node.Dirty(ModificationScope.Node);    
                });
                enumField.visible = enumFields.IndexOf(enumField) < node.outputSlots;
            }

            var sliderField = new SliderInt(1, 5);
            sliderField.value = node.outputSlots;
            sliderField.showInputField = true;
            var propertyRow = new PropertyRow(new Label("Output Slots Count"));
            propertyRow.Add(sliderField, (field) =>
            {

               field.RegisterValueChangedCallback(evt =>
               {
                   if (evt.newValue.Equals(node.outputSlots))
                       return;

                   setNodesAsDirtyCallback?.Invoke();
                   node.owner.owner.RegisterCompleteObjectUndo("Change output numbers");
                   node.outputSlots = evt.newValue;
                   updateNodeViewsCallback?.Invoke();
                   for(int i = 0; i < enumFields.Count; i++)
                   {
                        enumFields[i].visible = i < node.outputSlots;
                   }
                   node.UpdateNodeAfterDeserialization();
                   node.Dirty(ModificationScope.Node);
               });
            });
            parentElement.Add(propertyRow);
            for (int i = 0; i < enumFields.Count; i++)
            {
                parentElement.Add(enumFields[i]);
            }
        }
    
    

    }
}
