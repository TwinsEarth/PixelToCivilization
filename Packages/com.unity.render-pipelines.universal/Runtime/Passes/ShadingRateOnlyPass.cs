#if UNITY_EDITOR
using System;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    sealed class ShadingRateOnlyPass : ScriptableRenderPass
    {
        #region Fields
        static readonly string[] s_ShaderTags = new string[] { "ShadingRate" };

        RTHandle m_Color;
        RTHandle m_Depth;
        readonly Material m_ObjectShadingRateMaterial;
        internal const GraphicsFormat k_TargetFormat = GraphicsFormat.R8G8B8A8_SRGB;

        private PassData m_PassData;
        #endregion

        #region Constructors
        internal ShadingRateOnlyPass(Material objectShadingRateMaterial)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            m_ObjectShadingRateMaterial = objectShadingRateMaterial;
            m_PassData = new PassData();
            base.profilingSampler = ProfilingSampler.Get(URPProfileId.ShadingRate);

            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        #endregion

        #region State
        internal void Setup(RTHandle color, RTHandle depth)
        {
            m_Color = color;
            m_Depth = depth;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.SetGlobalTexture(m_Color.name, m_Color.nameID);
            cmd.SetGlobalTexture(m_Depth.name, m_Depth.nameID);
            ConfigureTarget(m_Color, m_Depth);
            ConfigureClear(ClearFlag.Color | ClearFlag.Depth, new Color(0.06299f, 0.06299f, 0.06299f, 1.0f));

            ConfigureDepthStoreAction(RenderBufferStoreAction.DontCare);
        }

        #endregion

        #region Execution
        private static void ExecutePass(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData)
        {
            var objectShadingRateMaterial = passData.objectMaterial;

            if (objectShadingRateMaterial == null)
                return;

            // Get data
            ref var cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;

            // Never draw in Preview
            if (camera.cameraType == CameraType.Preview)
                return;

            // Profiling command
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.ShadingRate)))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                DrawShadingRate(context, ref renderingData, camera, objectShadingRateMaterial, cmd);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_PassData.objectMaterial = m_ObjectShadingRateMaterial;

            ExecutePass(context, m_PassData, ref renderingData);
        }

        private static DrawingSettings GetDrawingSettings(ref RenderingData renderingData, Material objectMaterial)
        {
            var camera = renderingData.cameraData.camera;
            var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
            var drawingSettings = new DrawingSettings(ShaderTagId.none, sortingSettings)
            {
                perObjectData = PerObjectData.ShadingRate,
                enableDynamicBatching = renderingData.supportsDynamicBatching,
                enableInstancing = true,
            };

            for (int i = 0; i < s_ShaderTags.Length; ++i)
            {
                drawingSettings.SetShaderPassName(i, new ShaderTagId(s_ShaderTags[i]));
            }

            drawingSettings.overrideMaterial = objectMaterial;

            return drawingSettings;
        }

        private static void DrawShadingRate(ScriptableRenderContext context, ref RenderingData renderingData, Camera camera, Material objectMaterial, CommandBuffer cmd)
        {
            var drawingSettings = GetDrawingSettings(ref renderingData, objectMaterial);
            var filteringSettings = new FilteringSettings(RenderQueueRange.all, camera.cullingMask);
            var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
        }
        #endregion

        private class PassData
        {
            internal TextureHandle ShadingRateColor;
            internal TextureHandle ShadingRateDepth;
            internal TextureHandle cameraDepth;
            internal RenderingData renderingData;
            internal Material objectMaterial;
        }

        internal void Render(RenderGraph renderGraph, ref TextureHandle cameraDepthTexture, in TextureHandle ShadingRateColor, in TextureHandle ShadingRateDepth, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Shading Rate Pass", out var passData, base.profilingSampler))
            {
                //  TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);

                passData.ShadingRateColor = builder.UseColorBuffer(ShadingRateColor, 0);
                passData.ShadingRateDepth = builder.UseDepthBuffer(ShadingRateDepth, DepthAccess.Write);
                passData.cameraDepth       = builder.ReadTexture(cameraDepthTexture);
                passData.renderingData = renderingData;
                passData.objectMaterial = m_ObjectShadingRateMaterial;

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    ExecutePass(context.renderContext, data, ref data.renderingData);
                    data.renderingData.commandBuffer.SetGlobalTexture("_ShadingRateTexture", data.ShadingRateColor);
                });
                return;
            }
        }
    }
}
#endif
