using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    abstract class LoopNode : AbstractMaterialNode
    {
        public new abstract bool ExposeToSearcher();
        [SerializeField]
        int m_LoopIndex;

        public int loopIndex { get=> m_LoopIndex; set=>m_LoopIndex = value; }
    }
}
