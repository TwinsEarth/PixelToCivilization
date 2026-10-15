using System;
using UnityEngine;
using PxC.Core.Persistence;

namespace PxC.Plugins.Settlement
{
    /// <summary>聚落插件存档段。平行数组保存所有聚落（含被废弃的槽位不导出）。</summary>
    [Serializable]
    public sealed class SettlementSaveSegment : ISaveSegment
    {
        /// <summary>插件 id（="pxc.settlement"）。</summary>
        public string PluginId;

        /// <summary>段版本（当前 1）。</summary>
        public int SegmentVersion = 1;

        /// <summary>有效聚落数量。</summary>
        public int Count;

        /// <summary>聚落 slot（在 registry 中的槽位，用于恢复时尽量对齐）。</summary>
        public int[] Slots;

        /// <summary>所属势力。</summary>
        public int[] Factions;

        /// <summary>聚落类型。</summary>
        public int[] Kinds;

        /// <summary>聚落名。</summary>
        public string[] Names;

        /// <summary>世界坐标。</summary>
        public Vector3[] Positions;

        /// <summary>等级。</summary>
        public int[] Levels;

        /// <summary>规模人口。</summary>
        public int[] Populations;

        /// <summary>建立年份。</summary>
        public int[] FoundedYears;

        /// <summary>发展度。</summary>
        public float[] Prosperities;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
