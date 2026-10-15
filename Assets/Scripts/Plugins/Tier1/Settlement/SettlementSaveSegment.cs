using System;
using UnityEngine;
using PxC.Core.Persistence;

namespace PxC.Plugins.Settlement
{
    /// <summary>聚落插件存档段（v10.4.0，SegmentVersion=2）。平行数组保存所有存活聚落；
    /// 人口由 population 段独立保存（聚落 × 分层），本段不再含 Populations。</summary>
    [Serializable]
    public sealed class SettlementSaveSegment : ISaveSegment
    {
        /// <summary>插件 id（="pxc.settlement"）。</summary>
        public string PluginId;

        /// <summary>段版本（v10.4.0 起为 2：删除人口字段）。</summary>
        public int SegmentVersion = 2;

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

        /// <summary>建立年份。</summary>
        public int[] FoundedYears;

        /// <summary>发展度。</summary>
        public float[] Prosperities;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
