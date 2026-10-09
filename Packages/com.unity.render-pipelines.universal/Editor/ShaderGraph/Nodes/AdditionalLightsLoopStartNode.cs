using UnityEngine;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using System.Collections.Generic;
using System;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Additional Lights Loop")]
    class AdditionalLightsLoopStartNode : LoopStart, IGeneratesBodyCode, IMayRequireNDCPosition, IMayRequirePosition
    {
        public AdditionalLightsLoopStartNode()
        {
            name = "Additional Lights Loop Start";
            synonyms = new string[] { "additionalLightsLoop", "light", "loop", "start" };
            UpdateNodeAfterDeserialization();
        }
        public override bool hasPreview { get { return false; } }
        const string kLightColorOutputSlotName = "Additional Light Color";
        const string kLightDirectionOutputSlotName = "Additional Light Direction";
        const string kLightAttenuationOutputSlotName = "Additional Light Attenuation";
        const string kPositionWSInputSlotName = "PositionWS";
        const string kScreenPositionInputSlotName = "ScreenPos";
        const string kShadowmaskInputSlotName = "ShadowMask";


        const int kLightColorOutputSlotId = 0;
        const int kLightDirectionOutputSlotId = 1;
        const int kLightAttenuationOutputSlotId = 2;
        const int kPositionWSInputSlotId = 3;
        const int kScreenPositionInputSlotId = 4;
        const int kShadowmaskInputSlotId = 5;

        public override bool ExposeToSearcher(){ return true; }

        public override void CreateGroup(ref GraphData graph)
        {
            GroupData groupData = new GroupData(){ title = "Additional Lights Loop", loopGroup = true };
            graph.CreateGroup(groupData);
            graph.SetGroup(this as IGroupItem, groupData);
        }

        public override void CreateEndNode(ref GraphData graph)
        {
            var endNode = new AdditionalLightsLoopEndNode();
            var endNodeDS = endNode.drawState;
            endNodeDS.position = new Rect(drawState.position.position + new Vector2(400, 0), Vector2.zero);
            endNode.drawState = endNodeDS;
            endNode.group = group;
            endNode.loopIndex = loopIndex;
            graph.SetGroup(endNode, group);
            graph.AddNode(endNode);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("$precision3 {0} = 0;",  GetVariableNameForSlot(kLightColorOutputSlotId));
            sb.AppendLine("$precision3 {0} = $precision3(0, 1, 0);",  GetVariableNameForSlot(kLightDirectionOutputSlotId));
            sb.AppendLine("$precision {0} = 1;",  GetVariableNameForSlot(kLightAttenuationOutputSlotId));

            if (!generationMode.IsPreview())
            {
                sb.AppendLine("#if defined(_ADDITIONAL_LIGHTS)");
                sb.AppendLine("$precision3 outColor{0} = 0;", loopIndex);
                sb.AppendLine("uint pixelLightCount{0} = GetAdditionalLightsCount();", loopIndex);

                #region loopBegin
                sb.AppendLine("#if USE_FORWARD_PLUS");
                sb.AppendLine("ClusterIterator _urp_internal_clusterIterator{2} = ClusterInit({0}.xy, {1}, 0);", GetSlotValue(kScreenPositionInputSlotId, generationMode), GetSlotValue(kPositionWSInputSlotId, generationMode), loopIndex);
                sb.AppendLine("uint lightIndex{0} = 0; ", loopIndex);
                sb.AppendLine("[loop] while(true){");
                sb.AppendLine("if (lightIndex{0} >= min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS))", loopIndex);
                sb.AppendLine("{");
                sb.AppendLine("if(!ClusterNext(_urp_internal_clusterIterator{0}, lightIndex{0}))", loopIndex);
                sb.AppendLine("{break;}");
                sb.AppendLine("lightIndex{0} += URP_FP_DIRECTIONAL_LIGHTS_COUNT;", loopIndex);
                sb.AppendLine("}");

                sb.AppendLine("#if USE_FORWARD_PLUS && defined(LIGHTMAP_ON) && defined(LIGHTMAP_SHADOW_MIXING)");
                sb.AppendLine("if (_AdditionalLightsColor[lightIndex{0}].a > 0.0h) continue;", loopIndex);
                sb.AppendLine("#endif");

                sb.AppendLine("#elif !_USE_WEBGL1_LIGHTS");
                sb.AppendLine("for(int lightIndex = 0u; lightIndex < pixelLightCount{0}; ++lightIndex)", loopIndex);
                sb.AppendLine("{");
                sb.AppendLine("#else");
                sb.AppendLine("for(int lightIndex = 0; lightIndex < _WEBGL1_MAX_LIGHTS; ++lightIndex)");
                sb.AppendLine("{ if (lightIndex >= (int)lightCount) break;");
                sb.AppendLine("#endif");
                #endregion

                sb.AppendLine("#if USE_FORWARD_PLUS");
                sb.AppendLine("Light light = GetAdditionalLight(lightIndex{2}, {0}, {1});", GetSlotValue(kPositionWSInputSlotId, generationMode), GetSlotValue(kShadowmaskInputSlotId, generationMode), loopIndex);
                sb.AppendLine("lightIndex{0}++;", loopIndex);
                sb.AppendLine("#else");
                sb.AppendLine("Light light = GetAdditionalLight(lightIndex, {0}, {1});", GetSlotValue(kPositionWSInputSlotId, generationMode), GetSlotValue(kShadowmaskInputSlotId, generationMode));
                sb.AppendLine("#endif");

                #region lightLayers
                sb.AppendLine("#ifdef _LIGHT_LAYERS");
                sb.AppendLine("if (IsMatchingLightLayer(light.layerMask, GetMeshRenderingLayer()))");
                sb.AppendLine("#endif");
                sb.AppendLine("{");
                #endregion

                sb.AppendLine("{0} = light.color;",  GetVariableNameForSlot(kLightColorOutputSlotId));
                sb.AppendLine("{0} = light.direction;",  GetVariableNameForSlot(kLightDirectionOutputSlotId));
                sb.AppendLine("{0} = light.shadowAttenuation * light.distanceAttenuation;",  GetVariableNameForSlot(kLightAttenuationOutputSlotId));
                
                sb.AppendLine("#endif "); // if additional lights
            }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kLightColorOutputSlotId, kLightColorOutputSlotName, kLightColorOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kLightDirectionOutputSlotId, kLightDirectionOutputSlotName, kLightDirectionOutputSlotName, SlotType.Output, Vector3.up));
            AddSlot(new Vector1MaterialSlot(kLightAttenuationOutputSlotId, kLightAttenuationOutputSlotName, kLightAttenuationOutputSlotName, SlotType.Output, 1));

            AddSlot(new PositionMaterialSlot(kPositionWSInputSlotId, kPositionWSInputSlotName, kPositionWSInputSlotName, CoordinateSpace.World));
            AddSlot(new ScreenPositionMaterialSlot(kScreenPositionInputSlotId, kScreenPositionInputSlotName, kScreenPositionInputSlotName, ScreenSpaceType.Default, hidden: true));
            AddSlot(new Vector4MaterialSlot(kShadowmaskInputSlotId, kShadowmaskInputSlotName, kShadowmaskInputSlotName, SlotType.Input, Vector4.one));

            RemoveSlotsNameNotMatching(new[] { kLightColorOutputSlotId, kLightDirectionOutputSlotId, kLightAttenuationOutputSlotId, kPositionWSInputSlotId, kScreenPositionInputSlotId, kShadowmaskInputSlotId});
        }


        public bool RequiresNDCPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return true;
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return FindSlot<PositionMaterialSlot>(kPositionWSInputSlotId).RequiresPosition();
        }

    }
}
