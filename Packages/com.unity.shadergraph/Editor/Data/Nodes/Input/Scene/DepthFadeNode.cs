using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Depth Fade")]
    sealed class DepthFadeNode : CodeFunctionNode, IMayRequireDepthTexture, IMayRequireScreenPosition
    {
        public DepthFadeNode()
        {
            name = "Depth Fade";
            synonyms = new string[] { "depth", "fade", };
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }

        #region Booleans

        [SerializeField]
        internal bool m_Convert2Linear = true;

        [ToggleControl("Convert To Linear")]
        public ToggleData convert2Linear
        {
            get { return new ToggleData(m_Convert2Linear); }
            set
            {
                if (m_Convert2Linear == value.isOn)
                    return;
                m_Convert2Linear = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        internal bool m_Mirror = true;

        [ToggleControl("Mirror")]
        public ToggleData mirror
        {
            get { return new ToggleData(m_Mirror); }
            set
            {
                if (m_Mirror == value.isOn)
                    return;
                m_Mirror = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        internal bool m_Saturate = true;

        [ToggleControl("Saturate")]
        public ToggleData saturate
        {
            get { return new ToggleData(m_Saturate); }
            set
            {
                if (m_Saturate == value.isOn)
                    return;
                m_Saturate = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        #endregion

        static string convert2LinearLine, mirrorLine, saturateLine;

        protected override MethodInfo GetFunctionToConvert()
        {
            convert2LinearLine = m_Convert2Linear ? @"$precision depth = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(SceneUV.xy), _ZBufferParams);" : 
                                                    @"$precision depth = SHADERGRAPH_SAMPLE_SCENE_DEPTH(SceneUV.xy) * (_ProjectionParams.z - _ProjectionParams.y);";

            mirrorLine = m_Mirror ? @"$precision fade = abs((depth - LinearEyeDepth(SceneUV.z, _ZBufferParams)) / Distance);" :
                                    @"$precision fade = (depth - LinearEyeDepth(SceneUV.z, _ZBufferParams)) / Distance;";

            saturateLine = m_Saturate ? @"Out = saturate(fade);" : @"Out = fade;";
            
            return GetType().GetMethod("Unity_DepthFade", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DepthFade(
            [Slot(0, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out,
            [Slot(1, Binding.RawScreenPosition, true)] Vector2 SceneUV,
            [Slot(2, Binding.None, 2f, 0f, 0f, 0f)] Vector1 Distance)
        {
            return
@"
{
    SceneUV /= SceneUV.w;" +
    convert2LinearLine +
    mirrorLine +
    saturateLine +
@"
}";
        }

        bool IMayRequireDepthTexture.RequiresDepthTexture(ShaderStageCapability stageCapability)
        {
            return true;
        }

        bool IMayRequireScreenPosition.RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
