using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering.Universal;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor
{
    // Used for ShaderGraph Fabric shaders
    class ShaderGraphFabricGUI : ShaderGraphLitGUI
    {
        MaterialProperty shadingFeature;
        MaterialProperty fabricType;

        public static GUIContent fabricTypeText = EditorGUIUtility.TrTextContent("Fabric Type",
            "Select a fabric type that fits your needs. Choose between CottonWool or Silk.");
        public static GUIContent diffuseQualityText = EditorGUIUtility.TrTextContent("Diffuse Quality",
            "Select a diffuse quality that fits your needs. Choose between Low or High.");
        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            var material = materialEditor?.target as Material;
            if (material == null)
                return;

            base.FindProperties(properties);
            shadingFeature = BaseShaderGUI.FindProperty(Property.ShadingFeatuers, properties, false);
            fabricType = BaseShaderGUI.FindProperty(Property.FabricType, properties, false);

        }


        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // fabric type
            if (fabricType != null)
            {
                MaterialEditor.BeginProperty(fabricType);

                int newType = EditorGUILayout.Popup(fabricTypeText, (int)fabricType.floatValue, Enum.GetNames(typeof(UniversalFabricSubTarget.FabricType)));
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck() && (newType != fabricType.floatValue || fabricType.hasMixedValue))
                {
                    this.materialEditor.RegisterPropertyChangeUndo(fabricTypeText.text);
                    fabricType.floatValue = newType;
                }
                MaterialEditor.EndProperty();
                if (fabricType.floatValue == (int)UniversalFabricSubTarget.FabricType.CottonWool)
                {
                    CoreUtils.SetKeyword(material, UniversalFabricSubTarget.FabricDefines.FabricType_CottonWool.referenceName, true);
                    CoreUtils.SetKeyword(material, UniversalFabricSubTarget.FabricDefines.FabricType_Silk.referenceName, false);
                }
                else
                {
                    CoreUtils.SetKeyword(material, UniversalFabricSubTarget.FabricDefines.FabricType_CottonWool.referenceName, false);
                    CoreUtils.SetKeyword(material, UniversalFabricSubTarget.FabricDefines.FabricType_Silk.referenceName, true);
                }

                if (!material.HasTexture("_PreIntegratedFGD_CharlieAndFabricLambert"))
                    material.SetTexture("_PreIntegratedFGD_CharlieAndFabricLambert", (Texture2D)UnityEditor.AssetDatabase.LoadAssetAtPath("Packages/com.unity.render-pipelines.universal/Textures/PreIntegratedFDG/PreIntegrated_CharlieAndFabricLambert.exr", typeof(Texture2D)));
                CoreUtils.SetKeyword(material, CoreKeywordDescriptors.SpecularModel_Aniso.referenceName, fabricType.floatValue == (int)UniversalFabricSubTarget.FabricType.Silk ? true : false);
            }


            base.DrawSurfaceOptions(material);


            // shading feature
            if (shadingFeature != null && (PotentialEnumList.Count == PotentialNameList.Count) && (PotentialEnumList.Count == PotentialRefNameList.Count))
            {
                MaterialEditor.BeginProperty(shadingFeature);

                var defaultFeatureTypes = (UniversalFabricSubTarget.FabricFeatureTypeMask)material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex(Property.ShadingFeatuers));
                if (defaultFeatureTypes > 0)
                {
                    var prevFeatureMask = (UniversalFabricSubTarget.FabricFeatureTypeMask)shadingFeature.floatValue;
                    List<string> featureList = new List<string>();
                    List<string> featureRef = new List<string>();
                    List<UniversalFabricSubTarget.FabricFeatureTypeMask> featureID = new List<UniversalFabricSubTarget.FabricFeatureTypeMask>();
                    int featureMask = 0;
                    int count = 0;
                    
                    for (int i = 0; i < PotentialEnumList.Count(); i++)
                    {
                        UpdateDataList(ref featureList, ref featureRef, ref featureID, ref count, ref featureMask, ref material, PotentialEnumList[i], defaultFeatureTypes, prevFeatureMask, PotentialNameList[i], PotentialRefNameList[i]);
                    }

                    EditorGUI.BeginChangeCheck();

                    var newMask = EditorGUILayout.MaskField("Shading Features", featureMask, featureList.ToArray());
                    if (EditorGUI.EndChangeCheck() && !newMask.Equals(featureMask))
                    {
                        this.materialEditor.RegisterPropertyChangeUndo("Change Material Feature Mask");
                        UniversalFabricSubTarget.FabricFeatureTypeMask newFeatureMask = new UniversalFabricSubTarget.FabricFeatureTypeMask();
                        for (int i = 0; i < featureRef.Count; i++)
                        {
                            var value = (newMask & (1 << i)) != 0;
                            CoreUtils.SetKeyword(material, featureRef[i], value);
                            if (value)
                                newFeatureMask |= featureID[i];
                        }
                        shadingFeature.floatValue = (int)newFeatureMask;
                    }

                    MaterialEditor.EndProperty();
                }
            
            }
        }

        void UpdateDataList(ref List<string> featureList, ref List<string> featureRef, ref List<UniversalFabricSubTarget.FabricFeatureTypeMask> featureID, ref int count, ref int featureMask, ref Material material, UniversalFabricSubTarget.FabricFeatureTypeMask featureType, UniversalFabricSubTarget.FabricFeatureTypeMask defaultFeatureTypes, UniversalFabricSubTarget.FabricFeatureTypeMask prevFeatureMask, string featureName, string featureRefName)
        {
            if (UniversalTarget.HasFeatureType(defaultFeatureTypes, featureType))
            {
                featureList.Add(featureName);
                featureRef.Add(featureRefName);
                featureID.Add(featureType);
                if (UniversalTarget.HasFeatureType(prevFeatureMask, featureType))
                {
                    featureMask |= 1 << count;
                    if (!material.IsKeywordEnabled(featureRefName))
                        material.EnableKeyword(featureRefName);
                }
                count++;
            }
        }

        public static readonly List<UniversalFabricSubTarget.FabricFeatureTypeMask> PotentialEnumList = new List<UniversalFabricSubTarget.FabricFeatureTypeMask>()
        {
            { UniversalFabricSubTarget.FabricFeatureTypeMask.ThinFilm },
            { UniversalFabricSubTarget.FabricFeatureTypeMask.DiffractionGratings },
            { UniversalFabricSubTarget.FabricFeatureTypeMask.CustomIndirectDiffuse },
            { UniversalFabricSubTarget.FabricFeatureTypeMask.CustomIndirectSpecular },
        };
        public static readonly List<string> PotentialNameList = new List<string>()
        {
            { FeatureName.ThinFilm },
            { FeatureName.DiffractionGratings },
            { FeatureName.CustomIndirectDiffuse },
            { FeatureName.CustomIndirectSpecular },
        };

        public static readonly List<string> PotentialRefNameList = new List<string>()
        {
            { CoreKeywordDescriptors.ThinFilm.referenceName },
            { CoreKeywordDescriptors.DiffractionGratings.referenceName },
            { CoreKeywordDescriptors.CustomIndirectDiffuse.referenceName },
            { CoreKeywordDescriptors.CustomIndirectSpecular.referenceName },
        };
    }
} // namespace UnityEditor
