using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.Naval
{
    /// <summary>单艘船艇的存档 DTO。</summary>
    [Serializable]
    public sealed class NavalUnitDto
    {
        public int Faction;
        public int Model;
        public float X;
        public float Y;
        public float Z;
        public float Hp;
        public int Level;
        public int Xp;
        public int AiState;
    }

    /// <summary>海军插件存档段。</summary>
    [Serializable]
    public sealed class NavalSaveSegment : ISaveSegment
    {
        public string PluginId;
        public int SegmentVersion = 1;
        public int Count;
        public NavalUnitDto[] Units;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
