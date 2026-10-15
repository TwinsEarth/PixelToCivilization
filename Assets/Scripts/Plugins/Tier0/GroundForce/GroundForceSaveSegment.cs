using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.GroundForce
{
    /// <summary>
    /// 单个地面单位的存档 DTO（运行时 slot 不稳定，存档不按 slot 索引）。
    /// 恢复时据此重新生成，保证 id 语义不依赖旧 slot。
    /// </summary>
    [Serializable]
    public sealed class GroundForceUnitDto
    {
        public int Faction;
        public int Model;
        public float X;
        public float Y;
        public float Z;
        public float Hp;
        public int Level;
        public int Xp;
        public int Squad;
        public int AiState;
    }

    /// <summary>
    /// 地面部队插件存档段。包含当前全部存活地面单位（DTO 数组）。
    /// </summary>
    [Serializable]
    public sealed class GroundForceSaveSegment : ISaveSegment
    {
        /// <summary>插件 id（="pxc.groundforce"）。</summary>
        public string PluginId;

        /// <summary>段版本（当前 1）。</summary>
        public int SegmentVersion = 1;

        /// <summary>地面单位数量。</summary>
        public int Count;

        /// <summary>存活地面单位 DTO 数组。</summary>
        public GroundForceUnitDto[] Units;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
