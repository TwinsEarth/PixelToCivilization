using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEditor.Rendering.Universal;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEditor.Rendering.Universal.Analytics;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor
{
    // Used for ShaderGraph ScalableLit shaders
    class ShaderGraphScalableLitGUI : ShaderGraphLitGUI
    {
        MaterialProperty shadingFeature;
        MaterialProperty usePreIntegratedFDG;

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            var material = materialEditor?.target as Material;
            if (material == null)
                return;

            base.FindProperties(properties);
            shadingFeature = BaseShaderGUI.FindProperty(Property.ShadingFeatuers, properties, false);
            usePreIntegratedFDG = BaseShaderGUI.FindProperty(Property.UsePreIntegratedFDG, properties, false);
        }


        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            base.DrawSurfaceOptions(material);
            // if (usePreIntegratedFDG != null)
            // {
            //     if (usePreIntegratedFDG.floatValue > 0.5 && !material.HasTexture("_PreIntegratedFGD_GGXDisneyDiffuse"))
            //         material.SetTexture("_PreIntegratedFGD_GGXDisneyDiffuse", (Texture2D)UnityEditor.AssetDatabase.LoadAssetAtPath("Packages/com.unity.render-pipelines.universal/Textures/PreIntegratedFDG/PreIntegratedFDG_GGXDisneyDiffuse.exr", typeof(Texture2D)));   
            // }

            if (shadingFeature != null && (PotentialEnumList.Count == PotentialNameList.Count) && (PotentialEnumList.Count == PotentialRefNameList.Count))
            {
                MaterialEditor.BeginProperty(shadingFeature);
                var defaultFeatureTypes = (UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask)material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex(Property.ShadingFeatuers));
                if (defaultFeatureTypes > 0)
                {
                    var prevFeatureMask = (UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask)shadingFeature.floatValue;
                    List<string> featureList = new List<string>();
                    List<string> featureRef = new List<string>();
                    List<UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask> featureID = new List<UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask>();
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
                        UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask newFeatureMask = new UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask();
                        for (int i = 0; i < featureRef.Count; i++)
                        {
                            var value = (newMask & (1 << i)) != 0;
                            CoreUtils.SetKeyword(material, featureRef[i], value);
                            if (value)
                                newFeatureMask |= featureID[i];
                        }
                        shadingFeature.floatValue = (int)newFeatureMask;
                        ShaderGraphGUIAnalytics.SendShaderFeatures("ScalableLitFeature", (int)newFeatureMask, false, "None");
                    }

                    MaterialEditor.EndProperty();
                }
            
            }
        }

        void UpdateDataList(ref List<string> featureList, ref List<string> featureRef, ref List<UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask> featureID, ref int count, ref int featureMask, ref Material material, UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask featureType, UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask defaultFeatureTypes, UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask prevFeatureMask, string featureName, string featureRefName)
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

        public static readonly List<UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask> PotentialEnumList = new List<UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask>()
        {
            { UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask.ClearCoat },
            { UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask.ThinFilm },
            { UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask.DiffractionGratings },
            { UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask.Anisotropy },
            { UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask.CustomIndirectDiffuse },
            { UniversalScalableLitSubTarget.ScalableLitFeatureTypeMask.CustomIndirectSpecular },
        };
        public static readonly List<string> PotentialNameList = new List<string>()
        {
            { FeatureName.ClearCoat },
            { FeatureName.ThinFilm },
            { FeatureName.DiffractionGratings },
            { FeatureName.Anisotropy },
            { FeatureName.CustomIndirectDiffuse },
            { FeatureName.CustomIndirectSpecular },
        };

        public static readonly List<string> PotentialRefNameList = new List<string>()
        {
            { CoreKeywordDescriptors.ClearCoat.referenceName },
            { CoreKeywordDescriptors.ThinFilm.referenceName },
            { CoreKeywordDescriptors.DiffractionGratings.referenceName },
            { CoreKeywordDescriptors.SpecularModel_Aniso.referenceName },
            { CoreKeywordDescriptors.CustomIndirectDiffuse.referenceName },
            { CoreKeywordDescriptors.CustomIndirectSpecular.referenceName },
        };
    }
} // namespace UnityEditor
