using UnityEngine.Analytics;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.Analytics
{
    internal class ShaderGraphGUIAnalytics
    {
        const string k_VendorKey = "unity.shadergraphGui";
        const string k_EventName = "shadergraphUrp";
        const int k_Version = 1;

        class ShaderFeatureData
        {
            internal const int k_MaxEventsPerHour = 1000;
            internal const int k_MaxNumberOfElements = 1000;
            public string action;
            public int shaderFeatures;
            public bool allowFeatureOverride;
            public string diffuseModal;
        }

        class FDGData
        {
            internal const int k_MaxEventsPerHour = 100;
            internal const int k_MaxNumberOfElements = 100;
            public string action;
            public bool useIntegratedFDG;
            public string diffuseQuality;
        }

        class ShaderGraphCreationData
        {
            internal const int k_MaxEventsPerHour = 100;
            internal const int k_MaxNumberOfElements = 1000;
            public string action;
            public string materialType;
        }

        public static void SendShaderFeatures(string action, int shaderFeatures, bool allowFeatureOverride, string diffuseModal)
        {
            if (!EditorAnalytics.enabled || EditorAnalytics.RegisterEventWithLimit(k_EventName, ShaderFeatureData.k_MaxEventsPerHour, ShaderFeatureData.k_MaxNumberOfElements, k_VendorKey, k_Version) != AnalyticsResult.Ok)
                return;

            using (GenericPool<ShaderFeatureData>.Get(out var data))
            {
                data.shaderFeatures = shaderFeatures;
                data.action = action;
                data.allowFeatureOverride = allowFeatureOverride;
                data.diffuseModal = diffuseModal;
                EditorAnalytics.SendEventWithLimit(k_EventName, data, k_Version);
            }
        }

        public static void SendToggleFDGEvent(string action, bool useIntegratedFDG, string diffuseQuality)
        {
            if (!EditorAnalytics.enabled || EditorAnalytics.RegisterEventWithLimit(k_EventName, FDGData.k_MaxEventsPerHour, FDGData.k_MaxNumberOfElements, k_VendorKey, k_Version) != AnalyticsResult.Ok)
                return;

            using (GenericPool<FDGData>.Get(out var data))
            {
                data.useIntegratedFDG = useIntegratedFDG;
                data.diffuseQuality = diffuseQuality;
                data.action = action;
                EditorAnalytics.SendEventWithLimit(k_EventName, data, k_Version);
            }
        }

        public static void SendShaderGraphCreate(string action, string materialType)
        {
            if (!EditorAnalytics.enabled || EditorAnalytics.RegisterEventWithLimit(k_EventName, ShaderGraphCreationData.k_MaxEventsPerHour, ShaderGraphCreationData.k_MaxNumberOfElements, k_VendorKey, k_Version) != AnalyticsResult.Ok)
                return;

            using (GenericPool<ShaderGraphCreationData>.Get(out var data))
            {
                data.materialType = materialType;
                data.action = action;
                EditorAnalytics.SendEventWithLimit(k_EventName, data, k_Version);
            }
        }
    }
}
