using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.Building
{
    /// <summary>存档用建筑记录 DTO（位置拆为 X/Y/Z，避免依赖 Vector3 序列化）。</summary>
    [Serializable]
    public struct BuildingRecordDto
    {
        public int Faction;
        public int TypeId;
        public float X;
        public float Y;
        public float Z;
        public float Hp;
        public float MaxHp;
        public int Level;
        public bool IsTower;
    }

    /// <summary>building 插件存档段：仅保存存活建筑。</summary>
    [Serializable]
    public sealed class BuildingSaveSegment : ISaveSegment
    {
        /// <summary>插件 id（="pxc.building"）。</summary>
        public string PluginId;

        /// <summary>段版本（当前 1）。</summary>
        public int SegmentVersion = 1;

        /// <summary>存活建筑数量。</summary>
        public int Count;

        /// <summary>存活建筑记录。</summary>
        public BuildingRecordDto[] Buildings;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
