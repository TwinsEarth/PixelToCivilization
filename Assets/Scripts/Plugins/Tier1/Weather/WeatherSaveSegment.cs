using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.Weather
{
    /// <summary>
    /// 天气插件存档段（全局天气，一段）。
    /// </summary>
    [Serializable]
    public sealed class WeatherSaveSegment : ISaveSegment
    {
        /// <summary>插件 id（="pxc.weather"）。</summary>
        public string PluginId;

        /// <summary>段版本（当前 1）。</summary>
        public int SegmentVersion = 1;

        /// <summary>当前天气（WeatherCondition）。</summary>
        public int Condition;

        /// <summary>当前强度 [0,1]。</summary>
        public float Intensity;

        /// <summary>当前天气对应年份。</summary>
        public int CurrentYear;

        /// <summary>随机流状态（用于复现）。</summary>
        public ulong RngState;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
