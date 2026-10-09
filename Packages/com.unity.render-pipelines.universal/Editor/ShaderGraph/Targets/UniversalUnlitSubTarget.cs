using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;
using static UnityEditor.Rendering.Universal.ShaderGraph.SubShaderUtils;
using static Unity.Rendering.Universal.ShaderUtils;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine.UIElements;
using System.Linq;
using TreeEditor;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalUnlitSubTarget : UniversalSubTarget, ILegacyTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("97c3f7dcb477ec842aa878573640313a"); // UniversalUnlitSubTarget.cs

        public override int latestVersion => 2;

        [SerializeField]
        List<string> m_RemovedVariants = new List<string>();

        [SerializeField]
        List<string> m_RemovedPasses = new List<string>();

        public List<string> removedVariants
        {
            get => m_RemovedVariants;
            set => m_RemovedVariants = value;
        }
        public List<string> removedPasses
        {
            get => m_RemovedPasses;
            set => m_RemovedPasses = value;
        }
        [SerializeField]
        public List<bool> keywordsStatus = Enumerable.Repeat(true, KeywordName.keywordDic.Count()).ToList();
        [SerializeField]
        public List<bool> passStatus = Enumerable.Repeat(true, PassName.passDic.Count()).ToList();

        public TargetPropertyGUIFoldout surfaceOptionsController;
        public bool surfaceOptionsFoldout = false;
        public TargetPropertyGUIFoldout passesController;
        public bool passesFoldout = false;

        public TargetPropertyGUIFoldout variantsController;
        public bool variantsFoldout = false;

        public static readonly Color foldoutColor = new Color(0.25f, 0.25f, 0.25f, 1);
        public static readonly Color borderColor = new Color(0.222f, 0.222f, 0.222f, 1);

        private UnlitTargetParams m_subTargetParams;

        public UniversalUnlitSubTarget()
        {
            displayName = "Unlit";
        }

        protected override ShaderID shaderID => ShaderID.SG_Unlit;

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);

            var universalRPType = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset);
            if (!context.HasCustomEditorForRenderPipeline(universalRPType))
            {
                var gui = typeof(ShaderGraphUnlitGUI);
#if HAS_VFX_GRAPH
                if (TargetsVFX())
                    gui = typeof(VFXShaderGraphUnlitGUI);
#endif
                context.AddCustomEditorForRenderPipeline(gui.FullName, universalRPType);
            }

            // initial params
            if (keywordsStatus.Count() != KeywordName.keywordDic.Count())
            {
                List<bool> tempKeywordStatus = Enumerable.Repeat(true, KeywordName.keywordDic.Count()).ToList();
                List<string> tempRemovedKeywords = new List<string>();
                for(int i = 0; i < removedVariants.Count(); i++)
                {
                    if (KeywordName.keywordDic.ContainsKey(removedVariants[i]))
                    {
                        tempRemovedKeywords.Add(removedVariants[i]);
                        tempKeywordStatus[KeywordName.GetKeywordID(removedVariants[i])] = false;
                    }
                }
                removedVariants = tempRemovedKeywords;
                keywordsStatus = tempKeywordStatus;
            }

            if (passStatus.Count() != PassName.passDic.Count())
            {
                List<bool> tempPassStatus = Enumerable.Repeat(true, PassName.passDic.Count()).ToList();
                List<string> tempRemovedPasses = new List<string>();
                for(int i = 0; i < removedPasses.Count(); i++)
                {
                    if (PassName.passDic.ContainsKey(removedPasses[i]))
                    {
                        tempRemovedPasses.Add(removedPasses[i]);
                        tempPassStatus[PassName.GetPassID(removedPasses[i])] = false;
                    }
                }
                removedPasses = tempRemovedPasses;
                passStatus = tempPassStatus;
            }
            m_subTargetParams.passStatus = passStatus;
            m_subTargetParams.keywordsStatus = keywordsStatus;

            // Process SubShaders
            context.AddSubShader(PostProcessSubShader(SubShaders.Unlit(target, m_subTargetParams, target.renderType, target.renderQueue, target.disableBatching)));
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            if (target.allowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set
                material.SetFloat(Property.SurfaceType, (float)target.surfaceType);
                material.SetFloat(Property.BlendMode, (float)target.alphaMode);
                material.SetFloat(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                material.SetFloat(Property.CullMode, (int)target.renderFace);
                material.SetFloat(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                material.SetFloat(Property.ZWriteControl, (float)target.zWriteControl);
                material.SetFloat(Property.ZTest, (float)target.zTestMode);
            }

            // We always need these properties regardless of whether the material is allowed to override
            // Queue control & offset enable correct automatic render queue behavior
            // Control == 0 is automatic, 1 is user-specified render queue
            material.SetFloat(Property.QueueOffset, 0.0f);
            material.SetFloat(Property.QueueControl, (float)BaseShaderGUI.QueueControl.Auto);

            // call the full unlit material setup function
            ShaderGraphUnlitGUI.UpdateMaterial(material, MaterialUpdateType.CreatedNewMaterial);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.Alpha, (target.surfaceType == SurfaceType.Transparent || target.alphaClip) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, target.alphaClip || target.allowMaterialOverride);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (target.allowMaterialOverride)
            {
                collector.AddFloatProperty(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.SurfaceType, (float)target.surfaceType);
                collector.AddFloatProperty(Property.BlendMode, (float)target.alphaMode);
                collector.AddFloatProperty(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.SrcBlend, 1.0f);    // always set by material inspector
                collector.AddFloatProperty(Property.DstBlend, 0.0f);    // always set by material inspector
                collector.AddToggleProperty(Property.ZWrite, (target.surfaceType == SurfaceType.Opaque));
                collector.AddFloatProperty(Property.ZWriteControl, (float)target.zWriteControl);
                collector.AddFloatProperty(Property.ZTest, (float)target.zTestMode);    // ztest mode is designed to directly pass as ztest
                collector.AddFloatProperty(Property.CullMode, (float)target.renderFace);    // render face enum is designed to directly pass as a cull mode

                bool enableAlphaToMask = (target.alphaClip && (target.surfaceType == SurfaceType.Opaque));
                collector.AddFloatProperty(Property.AlphaToMask, enableAlphaToMask ? 1.0f : 0.0f);
            }

            // We always need these properties regardless of whether the material is allowed to override other shader properties.
            // Queue control & offset enable correct automatic render queue behavior.  Control == 0 is automatic, 1 is user-specified.
            // We initialize queue control to -1 to indicate to UpdateMaterial that it needs to initialize it properly on the material.
            collector.AddFloatProperty(Property.QueueOffset, 0.0f);
            collector.AddFloatProperty(Property.QueueControl, -1.0f);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var universalTarget = (target as UniversalTarget);
        #region SurfaceOptions
            surfaceOptionsController = new TargetPropertyGUIFoldout()
            {
                text = "Surface Options",
                value = surfaceOptionsFoldout,
                style = { backgroundColor = foldoutColor, borderTopColor = borderColor, borderTopWidth = 1.5f },
                name = "Surface Options Foldout"
            };
            surfaceOptionsController.ApplyIndent(context.globalIndentLevel);
            surfaceOptionsController.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                surfaceOptionsFoldout = !surfaceOptionsFoldout;
                onChange();
            });
            context.Add(surfaceOptionsController);
            if (surfaceOptionsFoldout)
            {
                context.globalIndentLevel++;
                universalTarget.AddDefaultMaterialOverrideGUI(ref context, onChange, registerUndo);
                universalTarget.AddDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo, showReceiveShadows: false);
                context.globalIndentLevel--;
            }
        #endregion

        #region PassesControl
            passesController = new TargetPropertyGUIFoldout()
            {
                text = "Removed Passes",
                value = passesFoldout,
                style = { backgroundColor = foldoutColor, borderTopColor = borderColor, borderTopWidth = 1.5f},
                name = "Passes Control Foldout"
            };
            passesController.ApplyIndent(context.globalIndentLevel);
            passesController.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                passesFoldout = !passesFoldout;
                onChange();
            });
            context.Add(passesController);

            if (passesFoldout)
            {
                var removedPassList = new ReorderableListView<string>(
                    removedPasses,
                    "Removed Passes",
                    allowReorder: false,
                    showHeader: false);
                removedPassList.style.marginLeft = context.globalIndentLevel * 15;
                removedPassList.GetAddMenuOptions = () => PotentialPassList;

                removedPassList.OnAddMenuItemCallback +=
                (list, addMenuOptionIndex, addMenuOption) =>
                {
                    registerUndo("Change Removed Passes");
                    passStatus[PassName.GetPassID(addMenuOption)] = false;
                    list.Add(addMenuOption);
                    onChange();
                };

                removedPassList.RemoveItemCallback +=
                (list, itemIndex) =>
                {
                    registerUndo("Change Removed Passes");
                    passStatus[PassName.GetPassID(list[itemIndex])] = true;
                    list.RemoveAt(itemIndex);
                    onChange();
                };
                context.Add(removedPassList);
            }
        #endregion

        #region VariantsControl
            variantsController = new TargetPropertyGUIFoldout()
            {
                text = "Removed Keywords",
                value = variantsFoldout,
                style = { backgroundColor = foldoutColor, borderTopColor = borderColor, borderTopWidth = 1.5f},
                name = "Variants Control Foldout"
            };
            variantsController.ApplyIndent(context.globalIndentLevel);
            variantsController.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                variantsFoldout = !variantsFoldout;
                onChange();
            });
            context.Add(variantsController);

            if (variantsFoldout)
            {
                var removedList = new ReorderableListView<string>(
                    removedVariants,
                    "Removed Keywords",
                    allowReorder: false,
                    showHeader: false);
                removedList.style.marginLeft = context.globalIndentLevel * 15;
                removedList.GetAddMenuOptions = () => PotentialKeywords;

                removedList.OnAddMenuItemCallback +=
                (list, addMenuOptionIndex, addMenuOption) =>
                {
                    registerUndo("Change Removed Keywords");
                    keywordsStatus[KeywordName.GetKeywordID(addMenuOption)] = false;
                    list.Add(addMenuOption);
                    onChange();
                };

                removedList.RemoveItemCallback +=
                (list, itemIndex) =>
                {
                    registerUndo("Change Removed Keywords");
                    keywordsStatus[KeywordName.GetKeywordID(list[itemIndex])] = true;
                    list.RemoveAt(itemIndex);
                    onChange();
                };
                context.Add(removedList);
            }
        #endregion
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if (!(masterNode is UnlitMasterNode1 unlitMasterNode))
                return false;

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>()
            {
                { BlockFields.VertexDescription.Position, 9 },
                { BlockFields.VertexDescription.Normal, 10 },
                { BlockFields.VertexDescription.Tangent, 11 },
                { BlockFields.SurfaceDescription.BaseColor, 0 },
                { BlockFields.SurfaceDescription.Alpha, 7 },
                { BlockFields.SurfaceDescription.AlphaClipThreshold, 8 },
            };

            return true;
        }

        internal override void OnAfterParentTargetDeserialized()
        {
            Assert.IsNotNull(target);

            if (this.sgVersion < latestVersion)
            {
                // Upgrade old incorrect Premultiplied blend (with alpha multiply in shader) into
                // equivalent Alpha blend mode for backwards compatibility.
                if (this.sgVersion < 1)
                {
                    if (target.alphaMode == AlphaMode.Premultiply)
                    {
                        target.alphaMode = AlphaMode.Alpha;
                    }
                }
                ChangeVersion(latestVersion);
            }
        }

        #region Struct
        struct UnlitTargetParams
        {
            public List<bool> passStatus;
            public List<bool> keywordsStatus;
        }
        #endregion

        #region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor Unlit(UniversalTarget target, UnlitTargetParams targetParams, string renderType, string renderQueue, string disableBatchingTag)
            {
                var result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = UniversalTarget.kUnlitMaterialTypeTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    disableBatchingTag = disableBatchingTag,
                    generatesPreview = true,
                    passes = new PassCollection()
                };

                if (targetParams.passStatus[PassName.GetPassID(PassName.forwardPass)])
                    result.passes.Add(UnlitPasses.Forward(target, targetParams));

                if (target.mayWriteDepth && targetParams.passStatus[PassName.GetPassID(PassName.depthOnlyPass)])
                    result.passes.Add(PassVariant(CorePasses.DepthOnly(target), CorePragmas.Instanced));

                if (targetParams.passStatus[PassName.GetPassID(PassName.depthNormalPass)])
                    result.passes.Add(PassVariant(UnlitPasses.DepthNormalOnly(target), CorePragmas.Instanced));

                if ((target.castShadows || target.allowMaterialOverride) && targetParams.passStatus[PassName.GetPassID(PassName.shadowCasterPass)])
                    result.passes.Add(PassVariant(CorePasses.ShadowCaster(target), CorePragmas.Instanced));

                // Fill GBuffer with color and normal for custom GBuffer use cases.
                if (targetParams.passStatus[PassName.GetPassID(PassName.gBufferPass)])
                    result.passes.Add(UnlitPasses.GBuffer(target, targetParams));

                // Currently neither of these passes (selection/picking) can be last for the game view for
                // UI shaders to render correctly. Verify [1352225] before changing this order.
                if (targetParams.passStatus[PassName.GetPassID(PassName.sceneSelectionPass)])
                    result.passes.Add(PassVariant(CorePasses.SceneSelection(target), CorePragmas.Default));
                if (targetParams.passStatus[PassName.GetPassID(PassName.scenePickingPass)])
                    result.passes.Add(PassVariant(CorePasses.ScenePicking(target), CorePragmas.Default));
                return result;
            }
        }
        #endregion

        #region Pass
        static class UnlitPasses
        {
            static void AddKeywordsControlToForwardPass(ref PassDescriptor passDesc, UnlitTargetParams targetParams)
            {
                for (int i = 0; i < PotentialForwardKeywords.Count(); i++)
                {
                    if (targetParams.keywordsStatus[KeywordName.GetKeywordID(PotentialForwardKeywords[i])])
                        passDesc.keywords.Add(KeywordName.GetDesc(PotentialForwardKeywords[i]));
                }
            }
            static void AddKeywordsControlToGBufferPass(ref PassDescriptor passDesc, UnlitTargetParams targetParams)
            {
                for (int i = 0; i < PotentialGBufferKeywords.Count(); i++)
                {
                    if (targetParams.keywordsStatus[KeywordName.GetKeywordID(PotentialGBufferKeywords[i])])
                        passDesc.keywords.Add(KeywordName.GetDesc(PotentialGBufferKeywords[i]));
                }
            }

            public static PassDescriptor Forward(UniversalTarget target, UnlitTargetParams targetParams)
            {
                KeywordCollection keywords = UnlitKeywords.Forward;
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Universal Forward",
                    referenceName = "SHADERPASS_UNLIT",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentColorAlpha,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = UnlitRequiredFields.Unlit,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = CorePragmas.Forward,
                    defines = new DefineCollection { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection{ keywords },
                    includes = new IncludeCollection { UnlitIncludes.Unlit },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                AddKeywordsControlToForwardPass(ref result, targetParams);
                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
                CorePasses.AddAlphaToMaskControlToPass(ref result, target);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor DepthNormalOnly(UniversalTarget target)
            {
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "DepthNormalsOnly",
                    referenceName = "SHADERPASS_DEPTHNORMALSONLY",
                    lightMode = "DepthNormalsOnly",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = UnlitBlockMasks.FragmentDepthNormals,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = UnlitRequiredFields.DepthNormalsOnly,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.DepthNormalsOnly(target),
                    pragmas = CorePragmas.Forward,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection { CoreKeywordDescriptors.GBufferNormalsOct },
                    includes = new IncludeCollection { CoreIncludes.DepthNormalsOnly },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
                result.defines.Add(CoreKeywordDescriptors.SmoothnessInNormalPass_Off, 1);
                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }

            // Deferred only in SM4.5
            // GBuffer fill for consistency.
            public static PassDescriptor GBuffer(UniversalTarget target, UnlitTargetParams targetParams)
            {
                KeywordCollection keywords = new KeywordCollection { UnlitKeywords.GBuffer};
                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "GBuffer",
                    referenceName = "SHADERPASS_GBUFFER",
                    lightMode = "UniversalGBuffer",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentColorAlpha,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = UnlitRequiredFields.GBuffer,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = CorePragmas.GBuffer,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection{ keywords },
                    includes = new IncludeCollection { UnlitIncludes.GBuffer },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                AddKeywordsControlToGBufferPass(ref result, targetParams);
                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }

            #region PortMasks
            static class UnlitBlockMasks
            {
                public static readonly BlockFieldDescriptor[] FragmentDepthNormals = new BlockFieldDescriptor[]
                {
                    BlockFields.SurfaceDescription.NormalWS,
                    BlockFields.SurfaceDescription.Alpha,
                    BlockFields.SurfaceDescription.AlphaClipThreshold,
                };
            }
            #endregion

            #region RequiredFields
            static class UnlitRequiredFields
            {
                public static readonly FieldCollection Unlit = new FieldCollection()
                {
                    StructFields.Varyings.positionWS,
                    StructFields.Varyings.normalWS
                };

                public static readonly FieldCollection DepthNormalsOnly = new FieldCollection()
                {
                    StructFields.Varyings.normalWS,
                };

                public static readonly FieldCollection GBuffer = new FieldCollection()
                {
                    StructFields.Varyings.positionWS,
                    StructFields.Varyings.normalWS,
                    UniversalStructFields.Varyings.sh   // Satisfy !LIGHTMAP_ON requirements.
                };
            }
            #endregion
        }
        #endregion

        #region Keywords
        static class UnlitKeywords
        {
            public static readonly KeywordCollection Forward = new KeywordCollection()
            {
                // This contain lightmaps because without a proper custom lighting solution in Shadergraph,
                // people start with the unlit then add lightmapping nodes to it.
                // If we removed lightmaps from the unlit target this would ruin a lot of peoples days.
                CoreKeywordDescriptors.DebugDisplay,
                CoreKeywordDescriptors.UseLightmapSingle,
            };

            public static readonly KeywordCollection GBuffer = new KeywordCollection
            {
            };
        }
        #endregion

        #region PotentialPassList
        public static readonly List<string> PotentialPassList = new List<string>()
        {
            { PassName.forwardPass },
            { PassName.shadowCasterPass },
            { PassName.depthOnlyPass },
            { PassName.depthNormalPass },
            { PassName.gBufferPass },
            { PassName.sceneSelectionPass },
            { PassName.scenePickingPass },
        };
        #endregion

        #region PotentialKeywordsList
        public static readonly List<string> PotentialForwardKeywords = new List<string>()
        {
            KeywordName.screenSpaceAmbientOcclusion,
            KeywordName.staticLightmap,
            KeywordName.directionalLightmapCombined,
            KeywordName.dBuffer,
            KeywordName.sampleGI,
        };

        public static readonly List<string> PotentialGBufferKeywords = new List<string>()
        {
            KeywordName.screenSpaceAmbientOcclusion,
            KeywordName.dBuffer,
        };
        public static readonly List<string> PotentialKeywords = PotentialForwardKeywords.Union(PotentialGBufferKeywords).ToList();
        #endregion

        #region Includes
        static class UnlitIncludes
        {
            const string kUnlitPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl";
            const string kUnlitGBufferPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitGBufferPass.hlsl";

            public static IncludeCollection Unlit = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.DOTSPregraph },
                { CoreIncludes.WriteRenderLayersPregraph },
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kUnlitPass, IncludeLocation.Postgraph },
            };

            public static IncludeCollection GBuffer = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.DOTSPregraph },
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kUnlitGBufferPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
