using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;
using UnityEngine.Assertions;
using static UnityEditor.Rendering.Universal.ShaderGraph.SubShaderUtils;
using UnityEngine.Rendering.Universal;
using static Unity.Rendering.Universal.ShaderUtils;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.Rendering.Universal.Analytics;
using static UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers.GraphDataPropertyDrawer;
using JetBrains.Annotations;
using System.ComponentModel;
using UnityEngine.Rendering;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering.Fullscreen.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalFabricSubTarget : UniversalSubTarget, ILegacyTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("a8b7873cda87d02479d0c5764fb3ef15"); // UniversalFabricSubTarget.cs

        public override int latestVersion => 3;

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace = NormalDropOffSpace.Tangent;

        [SerializeField]
        FabricType m_FabricType = FabricType.CottonWool;

        [SerializeField]
        DiffuseModelQuality m_DiffuseQuality = DiffuseModelQuality.Low;

        [SerializeField]
        bool m_BlendModePreserveSpecular = true;

        [SerializeField]
        List<string> m_RemovedVariants = new List<string>();

        [SerializeField]
        List<string> m_RemovedPasses = new List<string>();

        [SerializeField]
        List<string> m_Features = new List<string>();

        [SerializeField]
        bool m_UsePreIntegratedFDG = false;
        public UniversalFabricSubTarget()
        {
            displayName = "Fabric";
        }

        protected override ShaderID shaderID => ShaderID.SG_Fabric;

        public NormalDropOffSpace normalDropOffSpace
        {
            get => m_NormalDropOffSpace;
            set => m_NormalDropOffSpace = value;
        }

        public bool blendModePreserveSpecular
        {
            get => m_BlendModePreserveSpecular;
            set => m_BlendModePreserveSpecular = value;
        }

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
        public bool usePreIntegratedFDG
        {
            get => m_UsePreIntegratedFDG;
            set => m_UsePreIntegratedFDG = value;
        }
        [Obsolete("Use feature mask instead", false)]
        public List<string> features
        {
            get => m_Features;
            set => m_Features = value;
        }
        [SerializeField]
        public List<bool> keywordsStatus = Enumerable.Repeat(true, KeywordName.keywordDic.Count()).ToList();
        [SerializeField]
        public List<bool> passStatus = Enumerable.Repeat(true, PassName.passDic.Count()).ToList();
        [SerializeField]
        public List<bool> featureStatus = Enumerable.Repeat(false, FeatureName.featureDic.Count()).ToList();

        public FabricType fabricType
        {
            get => m_FabricType;
            set => m_FabricType = value;
        }
        public DiffuseModelQuality diffuseQuality
        {
            get => m_DiffuseQuality;
            set => m_DiffuseQuality = value;
        }

        // Foldout
        public TargetPropertyGUIFoldout shadingFeaturesController;
        public bool shadingFeatureFoldout = false;

        public TargetPropertyGUIFoldout surfaceOptionsController;
        public bool surfaceOptionsFoldout = false;

        public TargetPropertyGUIFoldout shadingQualityController;
        public bool shadingQualityFoldout = false;

        public TargetPropertyGUIFoldout passesController;
        public bool passesFoldout = false;

        public TargetPropertyGUIFoldout variantsController;
        public bool variantsFoldout = false;

        public static readonly Color foldoutColor = new Color(0.25f, 0.25f, 0.25f, 1);
        public static readonly Color borderColor = new Color(0.222f, 0.222f, 0.222f, 1);
        public override bool IsActive() => true;

        // featuer mask
        [Flags]
        public enum FabricFeatureTypeMask
        {
            ThinFilm = 1 << 2,
            DiffractionGratings = 1 << 3,
            CustomIndirectDiffuse = 1 << 4,
            CustomIndirectSpecular = 1 << 5
        }

        [SerializeField]
        FabricFeatureTypeMask m_FeatureTypeMask;
        public FabricFeatureTypeMask featureTypeMask
        {
            get => m_FeatureTypeMask;
            set
            {
                m_FeatureTypeMask = value;
            }
        }
        public bool HasFeatureType(FabricFeatureTypeMask featureType) => (m_FeatureTypeMask & featureType) != 0;
        [SerializeField]
        bool m_AllowFeatureOverride = false;
        public bool allowFeatureOverride
        {
            get => m_AllowFeatureOverride;
            set
            {
                m_AllowFeatureOverride = value;
            }
        }
        private FabricTargetParams m_subTargetParams;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            base.Setup(ref context);

            var universalRPType = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset);
            if (!context.HasCustomEditorForRenderPipeline(universalRPType))
            {
                var gui = typeof(ShaderGraphFabricGUI);
#if HAS_VFX_GRAPH
                if (TargetsVFX())
                    gui = typeof(VFXShaderGraphLitGUI);
#endif
                context.AddCustomEditorForRenderPipeline(gui.FullName, universalRPType);
            }

            // init params
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
            m_subTargetParams.fabricType = fabricType;
            m_subTargetParams.diffuseModelQuality = diffuseQuality;
            m_subTargetParams.passStatus = passStatus;
            m_subTargetParams.keywordsStatus = keywordsStatus;
            m_subTargetParams.featureTypeMask = featureTypeMask;
            m_subTargetParams.AllowFeatureOverride = allowFeatureOverride;
            m_subTargetParams.UsePreIntegratedFDG = usePreIntegratedFDG;
            context.AddSubShader(PostProcessSubShader(SubShaders.FabricSubShader(target, m_subTargetParams, target.renderType, target.renderQueue, target.disableBatching, blendModePreserveSpecular)));
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            if (target.allowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set
                material.SetFloat(Property.SpecularWorkflowMode, (float)1);
                material.SetFloat(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                material.SetFloat(Property.ReceiveShadows, target.receiveShadows ? 1.0f : 0.0f);
                material.SetFloat(Property.SurfaceType, (float)target.surfaceType);
                material.SetFloat(Property.BlendMode, (float)target.alphaMode);
                material.SetFloat(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                material.SetFloat(Property.CullMode, (int)target.renderFace);
                material.SetFloat(Property.ZWriteControl, (float)target.zWriteControl);
                material.SetFloat(Property.ZTest, (float)target.zTestMode);
            }

            // We always need these properties regardless of whether the material is allowed to override
            // Queue control & offset enable correct automatic render queue behavior
            // Control == 0 is automatic, 1 is user-specified render queue
            material.SetFloat(Property.QueueOffset, 0.0f);
            material.SetFloat(Property.QueueControl, (float)BaseShaderGUI.QueueControl.Auto);

            // call the full unlit material setup function
            ShaderGraphLitGUI.UpdateMaterial(material, MaterialUpdateType.CreatedNewMaterial);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            var descs = context.blocks.Select(x => x.descriptor);

            // Lit -- always controlled by subtarget
            context.AddField(UniversalFields.NormalDropOffOS, normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(UniversalFields.NormalDropOffTS, normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(UniversalFields.NormalDropOffWS, normalDropOffSpace == NormalDropOffSpace.World);
            context.AddField(UniversalFields.Normal, descs.Contains(BlockFields.SurfaceDescription.NormalOS) ||
                descs.Contains(BlockFields.SurfaceDescription.NormalTS) ||
                descs.Contains(BlockFields.SurfaceDescription.NormalWS));
            // Complex Lit

            // Template Predicates
            //context.AddField(UniversalFields.PredicateClearCoat, clearCoat);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.NormalOS, normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS, normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS, normalDropOffSpace == NormalDropOffSpace.World);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);

            // when the surface options are material controlled, we must show all of these blocks
            // when target controlled, we can cull the unnecessary blocks
            context.AddBlock(BlockFields.SurfaceDescription.Specular);
           // context.AddBlock(BlockFields.SurfaceDescription.Metallic, (workflowMode == WorkflowMode.Metallic) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha, (target.surfaceType == SurfaceType.Transparent || target.alphaClip) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, (target.alphaClip) || target.allowMaterialOverride);

            // these features are only valid for forward pass
            if (passStatus[PassName.GetPassID(PassName.forwardPass)])
            {
                context.AddBlock(UniversalBlockFields.SurfaceDescription.ThinFilmThickness, HasFeatureType(FabricFeatureTypeMask.ThinFilm));
                context.AddBlock(UniversalBlockFields.SurfaceDescription.ThinFilmMask, HasFeatureType(FabricFeatureTypeMask.ThinFilm));
                context.AddBlock(UniversalBlockFields.SurfaceDescription.SlitsDistance, HasFeatureType(FabricFeatureTypeMask.DiffractionGratings));
                context.AddBlock(UniversalBlockFields.SurfaceDescription.SlitsMask, HasFeatureType(FabricFeatureTypeMask.DiffractionGratings));
                context.AddBlock(UniversalBlockFields.SurfaceDescription.SlitsDirection, HasFeatureType(FabricFeatureTypeMask.DiffractionGratings));
                context.AddBlock(UniversalBlockFields.SurfaceDescription.Anisotropy, fabricType == FabricType.Silk || target.allowMaterialOverride);
                context.AddBlock(UniversalBlockFields.SurfaceDescription.Tangent, fabricType == FabricType.Silk || target.allowMaterialOverride);
                context.AddBlock(UniversalBlockFields.SurfaceDescription.CustomIndirectSpecular, HasFeatureType(FabricFeatureTypeMask.CustomIndirectSpecular));
                context.AddBlock(UniversalBlockFields.SurfaceDescription.IndirectSpecularMask, HasFeatureType(FabricFeatureTypeMask.CustomIndirectSpecular));
                context.AddBlock(UniversalBlockFields.SurfaceDescription.CustomIndirectDiffuse, HasFeatureType(FabricFeatureTypeMask.CustomIndirectDiffuse));
                context.AddBlock(UniversalBlockFields.SurfaceDescription.IndirectDiffuseMask, HasFeatureType(FabricFeatureTypeMask.CustomIndirectDiffuse));
                context.AddBlock(UniversalBlockFields.SurfaceDescription.CustomSpecularFDG, usePreIntegratedFDG);
                context.AddBlock(UniversalBlockFields.SurfaceDescription.CustomEnergyCompensation, usePreIntegratedFDG);
            }
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // if using material control, add the material property to control workflow mode
            if (target.allowMaterialOverride)
            {
                //collector.AddFloatProperty(Property.SpecularWorkflowMode, (float)workflowMode);
                collector.AddFloatProperty(Property.CastShadows, target.castShadows ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.ReceiveShadows, target.receiveShadows ? 1.0f : 0.0f);

                // setup properties using the defaults
                collector.AddFloatProperty(Property.FabricType, (float)fabricType);
                collector.AddFloatProperty(Property.SurfaceType, (float)target.surfaceType);
                collector.AddFloatProperty(Property.BlendMode, (float)target.alphaMode);
                collector.AddFloatProperty(Property.AlphaClip, target.alphaClip ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.BlendModePreserveSpecular, blendModePreserveSpecular ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.SrcBlend, 1.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddFloatProperty(Property.DstBlend, 0.0f);    // always set by material inspector, ok to have incorrect values here
                collector.AddToggleProperty(Property.ZWrite, (target.surfaceType == SurfaceType.Opaque));
                collector.AddFloatProperty(Property.ZWriteControl, (float)target.zWriteControl);
                collector.AddFloatProperty(Property.ZTest, (float)target.zTestMode);    // ztest mode is designed to directly pass as ztest
                collector.AddFloatProperty(Property.CullMode, (float)target.renderFace);    // render face enum is designed to directly pass as a cull mode

                bool enableAlphaToMask = (target.alphaClip && (target.surfaceType == SurfaceType.Opaque));
                collector.AddFloatProperty(Property.AlphaToMask, enableAlphaToMask ? 1.0f : 0.0f);
            }
            if (allowFeatureOverride)
                collector.AddIntProperty(Property.ShadingFeatuers, (int)featureTypeMask);
            // We always need these properties regardless of whether the material is allowed to override other shader properties.
            // Queue control & offset enable correct automatic render queue behavior.  Control == 0 is automatic, 1 is user-specified.
            // We initialize queue control to -1 to indicate to UpdateMaterial that it needs to initialize it properly on the material.
            collector.AddFloatProperty(Property.QueueOffset, 0.0f);
            collector.AddFloatProperty(Property.QueueControl, -1.0f);
        }

        PostTargetSettingsChangedCallback m_postChangeTargetSettingsCallback;
        ChangeGraphDefaultPrecisionCallback m_changeGraphDefaultPrecisionCallback;

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var universalTarget = (target as UniversalTarget);

            context.AddProperty("Fabric Type", new EnumField(FabricType.CottonWool) { value = fabricType }, (evt) =>
            {
                if (Equals(fabricType, evt.newValue))
                    return;

                registerUndo("Change Fabric Type");
                fabricType = (FabricType)evt.newValue;
                onChange();
            });

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
                universalTarget.AddDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo, showReceiveShadows: true);

                context.AddProperty("Fragment Normal Space", new EnumField(NormalDropOffSpace.Tangent) { value = normalDropOffSpace }, (evt) =>
                {
                    if (Equals(normalDropOffSpace, evt.newValue))
                        return;

                    registerUndo("Change Fragment Normal Space");
                    normalDropOffSpace = (NormalDropOffSpace)evt.newValue;
                    onChange();
                });



                if (target.surfaceType == SurfaceType.Transparent)
                {
                    if (target.alphaMode == AlphaMode.Alpha || target.alphaMode == AlphaMode.Additive)
                        context.AddProperty("Preserve Specular Lighting", new Toggle() { value = blendModePreserveSpecular }, (evt) =>
                        {
                            if (Equals(blendModePreserveSpecular, evt.newValue))
                                return;

                            registerUndo("Change Preserve Specular");
                            blendModePreserveSpecular = evt.newValue;
                            onChange();
                        });
                }

                context.globalIndentLevel--;
            }
        #endregion



        #region ShadingQuality


            shadingQualityController = new TargetPropertyGUIFoldout()
            {
                text = "Shading Quality",
                value = shadingQualityFoldout,
                style = { backgroundColor = foldoutColor, borderTopColor = borderColor, borderTopWidth = 1.5f},
                name = "Shading Quality Foldout"
            };
            shadingQualityController.ApplyIndent(context.globalIndentLevel);
            shadingQualityController.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                shadingQualityFoldout = !shadingQualityFoldout;
                onChange();
            });
            context.Add(shadingQualityController);

            if (shadingQualityFoldout && (target.allowMaterialOverride || fabricType == FabricType.Silk))
            {
                context.globalIndentLevel++;
                
                context.AddProperty("Custom PreIntegrated FDG", new Toggle() { value = usePreIntegratedFDG }, (evt) =>
                {
                    if (Equals(usePreIntegratedFDG, evt.newValue))
                        return;
                    registerUndo("Change Custom PreIntegrated FDG");
                    usePreIntegratedFDG = evt.newValue;
                    ShaderGraphGUIAnalytics.SendToggleFDGEvent("toggleFDG", usePreIntegratedFDG, diffuseQuality.ToString());
                    onChange();
                });

                context.AddProperty("Diffuse Quality (Silk Only)", new EnumField(DiffuseModelQuality.Low) { value = diffuseQuality }, (evt) =>
                {
                    if (Equals(diffuseQuality, evt.newValue))
                        return;

                    registerUndo("Change Diffuse Quality");
                    diffuseQuality = (DiffuseModelQuality)evt.newValue;
                    ShaderGraphGUIAnalytics.SendToggleFDGEvent("toggleDiffuseQuality", usePreIntegratedFDG, diffuseQuality.ToString());
                    onChange();
                });
                context.globalIndentLevel--;
            }
        #endregion


        #region ShadingFeatures
            shadingFeaturesController = new TargetPropertyGUIFoldout()
            {
                text = "Shading Features",
                value = shadingFeatureFoldout,
                style = { backgroundColor = foldoutColor, borderTopColor = borderColor, borderTopWidth = 1.5f},
                name = "Shading Features Foldout"
            };
            shadingFeaturesController.ApplyIndent(context.globalIndentLevel);
            shadingFeaturesController.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                shadingFeatureFoldout = !shadingFeatureFoldout;
                onChange();
            });
            context.Add(shadingFeaturesController);

            if (shadingFeatureFoldout)
            {
                context.AddProperty("Allow Feature Override", new Toggle() { value = allowFeatureOverride }, (evt) =>
                {
                    if (Equals(allowFeatureOverride, evt.newValue))
                        return;

                    registerUndo("Change Allow Feature Override");
                    allowFeatureOverride = evt.newValue;
                    onChange();
                });

                context.AddProperty("Shading Features", new EnumFlagsField(featureTypeMask) {value = featureTypeMask}, (evt) =>
                {
                    if (Equals(featureTypeMask, evt.newValue))
                        return;
                    registerUndo("Change Feature Type Mask");
                    featureTypeMask = (FabricFeatureTypeMask)evt.newValue;
                    onChange();
                });
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



        protected override int ComputeMaterialNeedsUpdateHash()
        {
            int hash = base.ComputeMaterialNeedsUpdateHash();
            hash = hash * 23 + target.allowMaterialOverride.GetHashCode();
            return hash;
        }

        public bool TryUpgradeFromMasterNode(IMasterNode1 masterNode, out Dictionary<BlockFieldDescriptor, int> blockMap)
        {
            blockMap = null;
            if (!(masterNode is PBRMasterNode1 pbrMasterNode))
                return false;

            //m_WorkflowMode = (WorkflowMode)pbrMasterNode.m_Model;
            m_NormalDropOffSpace = (NormalDropOffSpace)pbrMasterNode.m_NormalDropOffSpace;

            // Handle mapping of Normal block specifically
            BlockFieldDescriptor normalBlock;
            switch (m_NormalDropOffSpace)
            {
                case NormalDropOffSpace.Object:
                    normalBlock = BlockFields.SurfaceDescription.NormalOS;
                    break;
                case NormalDropOffSpace.World:
                    normalBlock = BlockFields.SurfaceDescription.NormalWS;
                    break;
                default:
                    normalBlock = BlockFields.SurfaceDescription.NormalTS;
                    break;
            }

            // Set blockmap
            blockMap = new Dictionary<BlockFieldDescriptor, int>()
            {
                { BlockFields.VertexDescription.Position, 9 },
                { BlockFields.VertexDescription.Normal, 10 },
                { BlockFields.VertexDescription.Tangent, 11 },
                { BlockFields.SurfaceDescription.BaseColor, 0 },
                { normalBlock, 1 },
                { BlockFields.SurfaceDescription.Emission, 4 },
                { BlockFields.SurfaceDescription.Smoothness, 5 },
                { BlockFields.SurfaceDescription.Occlusion, 6 },
                { BlockFields.SurfaceDescription.Alpha, 7 },
                { BlockFields.SurfaceDescription.AlphaClipThreshold, 8 },
            };

            blockMap.Add(BlockFields.SurfaceDescription.Specular, 3);
            return true;
        }

        internal override void OnAfterParentTargetDeserialized()
        {
            Assert.IsNotNull(target);

            if (this.sgVersion < latestVersion)
            {
                // Upgrade old incorrect Premultiplied blend into
                // equivalent Alpha + Preserve Specular blend mode.
                if (this.sgVersion < 1)
                {
                    if (target.alphaMode == AlphaMode.Premultiply)
                    {
                        target.alphaMode = AlphaMode.Alpha;
                        blendModePreserveSpecular = true;
                    }
                    else
                        blendModePreserveSpecular = false;
                }


                // for older version compatibility
                if (featureStatus != null)
                {
                    if (featureStatus.Count > FeatureName.GetFeatureID(FeatureName.ThinFilm))
                    {
                        if (featureStatus[FeatureName.GetFeatureID(FeatureName.ThinFilm)])
                        {
                            featureTypeMask |= FabricFeatureTypeMask.ThinFilm;
                        }
                    }           
                    if (featureStatus.Count > FeatureName.GetFeatureID(FeatureName.DiffractionGratings))
                    {
                        if (featureStatus[FeatureName.GetFeatureID(FeatureName.DiffractionGratings)])
                        {
                            featureTypeMask |= FabricFeatureTypeMask.DiffractionGratings;
                        }
                    }       
                    if (featureStatus.Count > FeatureName.GetFeatureID(FeatureName.CustomIndirectDiffuse))
                    {
                        if (featureStatus[FeatureName.GetFeatureID(FeatureName.CustomIndirectDiffuse)])
                        {
                            featureTypeMask |= FabricFeatureTypeMask.CustomIndirectDiffuse;
                        }
                    }
                    if (featureStatus.Count > FeatureName.GetFeatureID(FeatureName.CustomIndirectSpecular))
                    {
                        if (featureStatus[FeatureName.GetFeatureID(FeatureName.CustomIndirectSpecular)])
                        {
                            featureTypeMask |= FabricFeatureTypeMask.CustomIndirectSpecular;
                        }
                    }
                }
                ChangeVersion(latestVersion);
            }
        }


        #region Struct
        struct FabricTargetParams
        {
            public FabricType fabricType;
            public DiffuseModelQuality diffuseModelQuality;
            public FabricFeatureTypeMask featureTypeMask;
            public bool AllowFeatureOverride;
            public bool UsePreIntegratedFDG;
            public List<bool> passStatus;
            public List<bool> keywordsStatus;
        }

        internal enum FabricType
        {
            CottonWool = 1,
            Silk = 0,
        }
        internal enum DiffuseModelQuality
        {
            Low = 0,
            High = 1,
        }
        #endregion

        #region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor FabricSubShader(UniversalTarget target, FabricTargetParams targetParams, string renderType, string renderQueue, string disableBatchingTag, bool blendModePreserveSpecular)
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = UniversalTarget.kComplexLitMaterialTypeTag,
                    renderType = renderType,
                    renderQueue = renderQueue,
                    disableBatchingTag = disableBatchingTag,
                    generatesPreview = true,
                    passes = new PassCollection()
                };

                if (targetParams.passStatus[PassName.GetPassID(PassName.forwardPass)])
                    result.passes.Add(FabricPasses.ForwardOnly(target, targetParams, blendModePreserveSpecular, CorePragmas.Forward));

               // Fills GBuffer too for potential custom usage of the GBuffer.
                if (targetParams.passStatus[PassName.GetPassID(PassName.gBufferPass)])
                    result.passes.Add(FabricPasses.GBuffer(target, targetParams, blendModePreserveSpecular));

                // cull the shadowcaster pass if we know it will never be used
                if ((target.castShadows || target.allowMaterialOverride) && targetParams.passStatus[PassName.GetPassID(PassName.shadowCasterPass)])
                    result.passes.Add(PassVariant(CorePasses.ShadowCaster(target), CorePragmas.Instanced));

                if (target.mayWriteDepth && targetParams.passStatus[PassName.GetPassID(PassName.depthOnlyPass)])
                    result.passes.Add(PassVariant(CorePasses.DepthOnly(target), CorePragmas.Instanced));

                if (targetParams.passStatus[PassName.GetPassID(PassName.depthNormalPass)])
                    result.passes.Add(PassVariant(FabricPasses.DepthNormalOnly(target), CorePragmas.Instanced));

                if (targetParams.passStatus[PassName.GetPassID(PassName.metaPass)])
                    result.passes.Add(PassVariant(FabricPasses.Meta(target), CorePragmas.Default));

                // Currently neither of these passes (selection/picking) can be last for the game view for
                // UI shaders to render correctly. Verify [1352225] before changing this order.
                if (targetParams.passStatus[PassName.GetPassID(PassName.sceneSelectionPass)])
                    result.passes.Add(PassVariant(CorePasses.SceneSelection(target), CorePragmas.Default));
                if (targetParams.passStatus[PassName.GetPassID(PassName.scenePickingPass)])
                    result.passes.Add(PassVariant(CorePasses.ScenePicking(target), CorePragmas.Default));
                if(targetParams.passStatus[PassName.GetPassID(PassName._2DPass)])
                    result.passes.Add(PassVariant(FabricPasses._2D(target), CorePragmas.Default));
                return result;
            }
        }
        #endregion

        #region Passes
        static class FabricPasses
        {
            static void AddKeywordsControlToForwardPass(ref PassDescriptor passDesc, FabricTargetParams targetParams)
            {
                for (int i = 0; i < PotentialForwardKeywords.Count(); i++)
                {
                    if (targetParams.keywordsStatus[KeywordName.GetKeywordID(PotentialForwardKeywords[i])])
                        passDesc.keywords.Add(KeywordName.GetDesc(PotentialForwardKeywords[i]));
                }
            }
            static void AddKeywordsControlToGBufferPass(ref PassDescriptor passDesc, FabricTargetParams targetParams)
            {
                for (int i = 0; i < PotentialGBufferKeywords.Count(); i++)
                {
                    if (targetParams.keywordsStatus[KeywordName.GetKeywordID(PotentialGBufferKeywords[i])])
                        passDesc.keywords.Add(KeywordName.GetDesc(PotentialGBufferKeywords[i]));
                }
            }

            static void AddReceiveShadowsControlToPass(ref PassDescriptor pass, UniversalTarget target, bool receiveShadows)
            {
                if (target.allowMaterialOverride)
                    pass.keywords.Add(FabricKeywords.ReceiveShadowsOff);
                else if (!receiveShadows)
                    pass.defines.Add(FabricKeywords.ReceiveShadowsOff, 1);
            }


            public static PassDescriptor ForwardOnly(
                UniversalTarget target,
                FabricTargetParams targetParams,
                bool blendModePreserveSpecular,
                PragmaCollection pragmas)
            {

                var block = FabricBlockMasks.FragmentLit;
                DefineCollection defineCollections = new DefineCollection { CoreDefines.UseFragmentFog };
                defineCollections.Add(FabricDefines.Fabric, 1);
                KeywordCollection keywordCollection = new KeywordCollection() {  FabricKeywords.Forward };

                // thin film
                if (UniversalTarget.HasFeatureType(targetParams.featureTypeMask, FabricFeatureTypeMask.ThinFilm))//if (targetParams.featureStatus[FeatureName.GetFeatureID(FeatureName.ThinFilm)])
                {
                    block = block.Append(UniversalBlockFields.SurfaceDescription.ThinFilmThickness).ToArray();
                    block = block.Append(UniversalBlockFields.SurfaceDescription.ThinFilmMask).ToArray();
                    if (targetParams.AllowFeatureOverride)
                        keywordCollection.Add(CoreKeywordDescriptors.ThinFilm);
                    else
                        defineCollections.Add(CoreKeywordDescriptors.ThinFilm, 1);
                }

                // diffraction gratings
                if (UniversalTarget.HasFeatureType(targetParams.featureTypeMask, FabricFeatureTypeMask.DiffractionGratings))//if (targetParams.featureStatus[FeatureName.GetFeatureID(FeatureName.ThinFilm)])
                {
                    block = block.Append(UniversalBlockFields.SurfaceDescription.SlitsDistance).ToArray();
                    block = block.Append(UniversalBlockFields.SurfaceDescription.SlitsMask).ToArray();
                    block = block.Append(UniversalBlockFields.SurfaceDescription.SlitsDirection).ToArray();
                    if (targetParams.AllowFeatureOverride)
                        keywordCollection.Add(CoreKeywordDescriptors.DiffractionGratings);
                    else
                        defineCollections.Add(CoreKeywordDescriptors.DiffractionGratings, 1);
                }

                // custom indirect specular
                if (UniversalTarget.HasFeatureType(targetParams.featureTypeMask, FabricFeatureTypeMask.CustomIndirectSpecular))//if (targetParams.featureStatus[FeatureName.GetFeatureID(FeatureName.ThinFilm)])
                {
                    block = block.Append(UniversalBlockFields.SurfaceDescription.CustomIndirectSpecular).ToArray();
                    block = block.Append(UniversalBlockFields.SurfaceDescription.IndirectSpecularMask).ToArray();
                    if (targetParams.AllowFeatureOverride)
                        keywordCollection.Add(CoreKeywordDescriptors.CustomIndirectSpecular);
                    else
                        defineCollections.Add(CoreKeywordDescriptors.CustomIndirectSpecular, 1);
                }

                // custom indirect diffuse
                if (UniversalTarget.HasFeatureType(targetParams.featureTypeMask, FabricFeatureTypeMask.CustomIndirectDiffuse))//if (targetParams.featureStatus[FeatureName.GetFeatureID(FeatureName.ThinFilm)])
                {
                    block = block.Append(UniversalBlockFields.SurfaceDescription.CustomIndirectDiffuse).ToArray();
                    block = block.Append(UniversalBlockFields.SurfaceDescription.IndirectDiffuseMask).ToArray();
                    if (targetParams.AllowFeatureOverride)
                        keywordCollection.Add(CoreKeywordDescriptors.CustomIndirectDiffuse);
                    else
                        defineCollections.Add(CoreKeywordDescriptors.CustomIndirectDiffuse, 1);
                }


                if (target.allowMaterialOverride)
                {
                    block = block.Append(UniversalBlockFields.SurfaceDescription.Anisotropy).ToArray();
                    block = block.Append(UniversalBlockFields.SurfaceDescription.Tangent).ToArray();
                    keywordCollection.Add(FabricDefines.FabricType_CottonWool);
                    keywordCollection.Add(FabricDefines.FabricType_Silk);
                    keywordCollection.Add(CoreKeywordDescriptors.SpecularModel_Aniso);
                }
                else
                {
                    // cotton wool
                    if (targetParams.fabricType == FabricType.CottonWool)
                            defineCollections.Add(FabricDefines.FabricType_CottonWool, 1);

                    // silk
                    if (targetParams.fabricType == FabricType.Silk)
                    {
                        block = block.Append(UniversalBlockFields.SurfaceDescription.Anisotropy).ToArray();
                        block = block.Append(UniversalBlockFields.SurfaceDescription.Tangent).ToArray();

                        defineCollections.Add(FabricDefines.FabricType_Silk, 1);
                        defineCollections.Add(CoreKeywordDescriptors.SpecularModel_Aniso, 1);
                    }
                }

                if (target.allowMaterialOverride || targetParams.fabricType == FabricType.Silk)
                {
                    if (targetParams.diffuseModelQuality == DiffuseModelQuality.Low)
                        defineCollections.Add(CoreKeywordDescriptors.DiffuseModel_Lambert, 1);
                    else
                        defineCollections.Add(CoreKeywordDescriptors.DiffuseModel_Disney, 1);
                }

                defineCollections.Add(FabricDefines.SpecularSetup, 1);

                // PreIntegratedFDG
                if (targetParams.UsePreIntegratedFDG)
                {
                    block = block.Append(UniversalBlockFields.SurfaceDescription.CustomSpecularFDG).ToArray();
                    block = block.Append(UniversalBlockFields.SurfaceDescription.CustomEnergyCompensation).ToArray();
                    defineCollections.Add(CoreKeywordDescriptors.UsePreIntegratedFDG, 1);
                }

                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Universal Forward Only",
                    referenceName = "SHADERPASS_FORWARDONLY",
                    lightMode = "UniversalForwardOnly",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = block,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = FabricRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target, blendModePreserveSpecular),
                    pragmas = pragmas,
                    defines = defineCollections,
                    keywords = keywordCollection,
                    includes = new IncludeCollection { FabricIncludes.Forward },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
                AddKeywordsControlToForwardPass(ref result, targetParams);
                CorePasses.AddTargetSurfaceControlsToPass(ref result, target, blendModePreserveSpecular);
                CorePasses.AddAlphaToMaskControlToPass(ref result, target);
                AddReceiveShadowsControlToPass(ref result, target, target.receiveShadows);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }

            // Deferred only in SM4.5, MRT not supported in GLES2
            public static PassDescriptor GBuffer(UniversalTarget target,
            FabricTargetParams targetParams, bool blendModePreserveSpecular)
            {

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
                    validPixelBlocks = FabricBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = FabricRequiredFields.GBuffer,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target, blendModePreserveSpecular),
                    pragmas = CorePragmas.GBuffer,
                    defines = new DefineCollection { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection() {  FabricKeywords.GBuffer },
                    includes = new IncludeCollection { FabricIncludes.GBuffer },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
                AddKeywordsControlToGBufferPass(ref result, targetParams);
                result.defines.Add(FabricDefines.SpecularSetup, 1);
                CorePasses.AddTargetSurfaceControlsToPass(ref result, target, blendModePreserveSpecular);
                AddReceiveShadowsControlToPass(ref result, target, target.receiveShadows);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor Meta(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "Meta",
                    referenceName = "SHADERPASS_META",
                    lightMode = "Meta",

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = FabricBlockMasks.FragmentMeta,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = FabricRequiredFields.Meta,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.Meta,
                    pragmas = CorePragmas.Default,
                    defines = new DefineCollection() { CoreDefines.UseFragmentFog },
                    keywords = new KeywordCollection() { CoreKeywordDescriptors.EditorVisualization },
                    includes = FabricIncludes.Meta,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor _2D(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    referenceName = "SHADERPASS_2D",
                    lightMode = "Universal2D",

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentColorAlpha,

                    // Fields
                    structs = CoreStructCollections.Default,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.UberSwitchedRenderState(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = FabricIncludes._2D,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor DepthNormal(UniversalTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "DepthNormals",
                    referenceName = "SHADERPASS_DEPTHNORMALS",
                    lightMode = "DepthNormals",
                    useInPreview = true,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentDepthNormals,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = CoreRequiredFields.DepthNormals,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.DepthNormalsOnly(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = new IncludeCollection { CoreIncludes.DepthNormalsOnly },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }

            public static PassDescriptor DepthNormalOnly(UniversalTarget target)
            {
                var result = new PassDescriptor()
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
                    validPixelBlocks = CoreBlockMasks.FragmentDepthNormals,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = CoreRequiredFields.DepthNormals,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.DepthNormalsOnly(target),
                    pragmas = CorePragmas.Instanced,
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = new IncludeCollection { CoreIncludes.DepthNormalsOnly },

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                CorePasses.AddAlphaClipControlToPass(ref result, target);
                CorePasses.AddLODCrossFadeControlToPass(ref result, target);

                return result;
            }
        }
        #endregion

        #region PortMasks
        static class FabricBlockMasks
        {
            public static readonly BlockFieldDescriptor[] FragmentLit = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalOS,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Specular,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };

            public static readonly BlockFieldDescriptor[] FragmentMeta = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };
        }
        #endregion

        #region RequiredFields
        static class FabricRequiredFields
        {
            public static readonly FieldCollection Forward = new FieldCollection()
            {
                StructFields.Attributes.uv1,
                StructFields.Attributes.uv2,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,                        // needed for vertex lighting
                UniversalStructFields.Varyings.staticLightmapUV,
                UniversalStructFields.Varyings.dynamicLightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };
            public static readonly FieldCollection GBuffer = new FieldCollection()
            {
                StructFields.Attributes.uv1,
                StructFields.Attributes.uv2,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,                        // needed for vertex lighting
                UniversalStructFields.Varyings.staticLightmapUV,
                UniversalStructFields.Varyings.dynamicLightmapUV,
                UniversalStructFields.Varyings.sh,
                UniversalStructFields.Varyings.fogFactorAndVertexLight, // fog and vertex lighting, vert input is dependency
                UniversalStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };

            public static readonly FieldCollection Meta = new FieldCollection()
            {
                StructFields.Attributes.positionOS,
                StructFields.Attributes.normalOS,
                StructFields.Attributes.uv0,                            //
                StructFields.Attributes.uv1,                            // needed for meta vertex position
                StructFields.Attributes.uv2,                            // needed for meta UVs
                StructFields.Attributes.instanceID,                     // needed for rendering instanced terrain
                StructFields.Varyings.positionCS,
                StructFields.Varyings.texCoord0,                        // needed for meta UVs
                StructFields.Varyings.texCoord1,                        // VizUV
                StructFields.Varyings.texCoord2,                        // LightCoord
            };
        }
        #endregion

        #region Defines
        public static class FabricDefines
        {
            public static readonly KeywordDescriptor SpecularSetup = new KeywordDescriptor()
            {
                displayName = "Specular Setup",
                referenceName = "_SPECULAR_SETUP",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Fragment
            };
            public static readonly KeywordDescriptor FabricType_CottonWool = new KeywordDescriptor()
            {
                displayName = "Fabric Type: CottonWool",
                referenceName = "_FABRIC_COTTON_WOOL",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Fragment
            };
            public static readonly KeywordDescriptor FabricType_Silk = new KeywordDescriptor()
            {
                displayName = "Fabric Type: Silk",
                referenceName = "_FABRIC_SILK",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Fragment
            };
            public static readonly KeywordDescriptor Fabric = new KeywordDescriptor()
            {
                displayName = "Fabric",
                referenceName = "_FABRIC",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Fragment
            };
        }
        #endregion

        #region Keywords
        static class FabricKeywords
        {
            public static readonly KeywordDescriptor ReceiveShadowsOff = new KeywordDescriptor()
            {
                displayName = "Receive Shadows Off",
                referenceName = ShaderKeywordStrings._RECEIVE_SHADOWS_OFF,
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordCollection Forward = new KeywordCollection
            {
                { CoreKeywordDescriptors.DebugDisplay },
                { CoreKeywordDescriptors.ForwardPlus },
            };
            public static readonly KeywordCollection GBuffer = new KeywordCollection
            {
                { CoreKeywordDescriptors.GBufferNormalsOct },
                { CoreKeywordDescriptors.RenderPassEnabled },
                { CoreKeywordDescriptors.DebugDisplay },
            };
        }
        #endregion

        #region PotentialFeatureList
        public static readonly List<string> PotentialFeatureList = new List<string>()
        {
            { FeatureName.ThinFilm },
            { FeatureName.DiffractionGratings },
            { FeatureName.CustomIndirectDiffuse},
            { FeatureName.CustomIndirectSpecular}
        };
        #endregion

        #region PotentialPassList
        public static readonly List<string> PotentialPassList = new List<string>()
        {
            { PassName.forwardPass },
            { PassName.shadowCasterPass },
            { PassName.depthOnlyPass },
            { PassName.depthNormalPass },
            { PassName.gBufferPass },
            { PassName.metaPass },
            { PassName.sceneSelectionPass },
            { PassName.scenePickingPass },
            { PassName._2DPass },
        };
        #endregion

        #region PotentialKeywordsList
        public static readonly List<string> PotentialForwardKeywords = new List<string>()
        {
            KeywordName.screenSpaceAmbientOcclusion,
            KeywordName.staticLightmap,
            KeywordName.dynamicLightmap,
            KeywordName.directionalLightmapCombined,
            KeywordName.mainLightShadows,
            KeywordName.additionalLights,
            KeywordName.additionalLightShadows,
            KeywordName.reflectionProbeBlending,
            KeywordName.reflectionProbeBoxProjection,
            KeywordName.shadowsSoft,
            KeywordName.lightmapShadowMixing,
            KeywordName.shadowsShadowmask,
            KeywordName.dBuffer,
            KeywordName.lightLayers,
            KeywordName.lightCookies,
            KeywordName.evaluateSH
        };

        public static readonly List<string> PotentialGBufferKeywords = new List<string>()
        {
            KeywordName.staticLightmap,
            KeywordName.dynamicLightmap,
            KeywordName.directionalLightmapCombined,
            KeywordName.mainLightShadows,
            KeywordName.reflectionProbeBlending,
            KeywordName.reflectionProbeBoxProjection,
            KeywordName.shadowsSoft,
            KeywordName.lightmapShadowMixing,
            KeywordName.shadowsShadowmask,
            KeywordName.mixedLightingSubtractive,
            KeywordName.dBuffer,
        };
        public static readonly List<string> PotentialKeywords = PotentialForwardKeywords.Union(PotentialGBufferKeywords).ToList();
        #endregion

        #region Includes
        static class FabricIncludes
        {
            const string kShadows = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl";
            const string kMetaInput = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl";
            const string kForwardPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRScalableForwardPass.hlsl";
            const string kGBuffer = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl";
            const string kPBRGBufferPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl";
            const string kLightingMetaPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl";
            const string k2DPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl";

            public static readonly IncludeCollection Forward = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.DOTSPregraph },
                { CoreIncludes.WriteRenderLayersPregraph },
                { CoreIncludes.CorePregraph },
                { kShadows, IncludeLocation.Pregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kForwardPass, IncludeLocation.Postgraph },
            };
            public static readonly IncludeCollection GBuffer = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.DOTSPregraph },
                { CoreIncludes.WriteRenderLayersPregraph },
                { CoreIncludes.CorePregraph },
                { kShadows, IncludeLocation.Pregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { CoreIncludes.DBufferPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kGBuffer, IncludeLocation.Postgraph },
                { kPBRGBufferPass, IncludeLocation.Postgraph },
            };
            public static readonly IncludeCollection Meta = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kMetaInput, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kLightingMetaPass, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection _2D = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { k2DPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
