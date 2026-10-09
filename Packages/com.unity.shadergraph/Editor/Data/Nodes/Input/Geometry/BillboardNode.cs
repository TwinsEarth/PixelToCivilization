using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using System.Reflection;
using System.ComponentModel;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Geometry", "Billboard")]
    class BillboardNode : CodeFunctionNode, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public enum BillboardMode
        {
            [InspectorName("All Axis")]
            AllAxis,
            [InspectorName("Around Y Axis")]
            AroundYAxis
        };

        [SerializeField]
        private BillboardMode m_BillboardMode = BillboardMode.AllAxis;

        [EnumControl("Billboard Mode")]
        public BillboardMode billboardMode
        {
            get { return m_BillboardMode; }
            set
            {
                if (m_BillboardMode == value)
                    return;

                m_BillboardMode = value;
                Dirty(ModificationScope.Graph);
            }
        }


        public override int latestVersion => 1;


        public BillboardNode()
        {
            name = "Billboard";
            precision = Precision.Single;
            UpdateNodeAfterDeserialization();
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (m_BillboardMode)
            {
                case BillboardMode.AroundYAxis:
                    return GetType().GetMethod("Unity_BillboardAroundYAxis", BindingFlags.Static | BindingFlags.NonPublic);
                default:
                    return GetType().GetMethod("Unity_BillboardDefault", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_BillboardDefault(
            [Slot(0, Binding.None, ShaderStageCapability.Vertex)] out Vector3 Position,
            [Slot(1, Binding.None, ShaderStageCapability.Vertex)] out Vector3 Normal,
            [Slot(2, Binding.None, ShaderStageCapability.Vertex)] out Vector3 Tangent,
            [Slot(3, Binding.ObjectSpacePosition, ShaderStageCapability.Vertex)] Vector3 PositionOS,
            [Slot(4, Binding.ObjectSpaceNormal, ShaderStageCapability.Vertex)] Vector3 NormalOS,
            [Slot(5, Binding.ObjectSpaceTangent, ShaderStageCapability.Vertex)] Vector3 TangentOS)
        {
            Position = PositionOS;
            Normal = NormalOS;
            Tangent = TangentOS;
            return
            @"
            {
                $precision3 posWS = TransformObjectToWorld(PositionOS);
                $precision3 normalWS = TransformObjectToWorldDir(NormalOS);
                $precision3 tangentWS = TransformObjectToWorldDir(TangentOS);
                $precision3 objPosWS = UNITY_MATRIX_M._m03_m13_m23;

                $precision3 relativePosWS = posWS - objPosWS;
                relativePosWS.z *= -1;
                $precision3 viewPos = mul(UNITY_MATRIX_V, $precision4(objPosWS, 1)) + relativePosWS;
                Position = mul(UNITY_MATRIX_I_M, mul(UNITY_MATRIX_I_V, $precision4(viewPos, 1)));
                normalWS.z *= -1;
                Normal = mul(UNITY_MATRIX_I_M, mul(UNITY_MATRIX_I_V, normalWS));
                tangentWS.z *= -1;
                Tangent = mul(UNITY_MATRIX_I_M, mul(UNITY_MATRIX_I_V, tangentWS));
            }
            ";
        }

        static string Unity_BillboardAroundYAxis(
            [Slot(0, Binding.None, ShaderStageCapability.Vertex)] out Vector3 Position,
            [Slot(1, Binding.None, ShaderStageCapability.Vertex)] out Vector3 Normal,
            [Slot(2, Binding.None, ShaderStageCapability.Vertex)] out Vector3 Tangent,
            [Slot(3, Binding.ObjectSpacePosition, ShaderStageCapability.Vertex)] Vector3 PositionOS,
            [Slot(4, Binding.ObjectSpaceNormal, ShaderStageCapability.Vertex)] Vector3 NormalOS,
            [Slot(5, Binding.ObjectSpaceTangent, ShaderStageCapability.Vertex)] Vector3 TangentOS)
        {
            Position = PositionOS;
            Normal = NormalOS;
            Tangent = TangentOS;
            return
            @"
            {

                $precision3 forward = mul(UNITY_MATRIX_I_M, UNITY_MATRIX_V._m02_m12_m22);
                forward.y = 0;
                forward = normalize(forward);
                $precision3 up = $precision3(0, 1, 0);
                $precision3 right = normalize(cross(up, forward));
                up = normalize(cross(forward, right));

                Position = PositionOS.x * right + PositionOS.y * up + PositionOS.z * forward;
                Normal = NormalOS.x * right + NormalOS.y * up + NormalOS.z * forward;
                Tangent = TangentOS.x * right + TangentOS.y * up + TangentOS.z * forward;
            }
            ";
        }
        NeededCoordinateSpace IMayRequirePosition.RequiresPosition(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.Object;
        }

        public override void OnAfterMultiDeserialize(string json)
        {
            base.OnAfterMultiDeserialize(json);
            //required update
            if (sgVersion < 1)
            {
                ChangeVersion(1);
            }
        }
    }
}
