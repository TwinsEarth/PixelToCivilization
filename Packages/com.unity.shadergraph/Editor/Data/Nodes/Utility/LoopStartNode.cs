using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEditor.ShaderKeywordFilter;

namespace UnityEditor.ShaderGraph
{
    abstract class LoopStart : LoopNode
    {
        public abstract void CreateGroup(ref GraphData graph);
        public abstract void CreateEndNode(ref GraphData graph);

    }
}
