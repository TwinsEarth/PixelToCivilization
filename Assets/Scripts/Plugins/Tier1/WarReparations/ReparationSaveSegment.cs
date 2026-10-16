using System;
using PxC.Core.Persistence;

namespace PxC.Plugins.WarReparations
{
    /// <summary>
    /// 战争赔款插件存档段（v10.7.7，SegmentVersion=1）。
    /// 平行数组保存所有生效中的赔款条约（跨年度持续状态，必须存档，
    /// 否则读档后条约丢失、胜者收不到剩余赔款）。
    /// 资源类型按 <see cref="PxC.Gameplay.Resources.ResourceType"/> 的整型值存储。
    /// </summary>
    [Serializable]
    public sealed class ReparationSaveSegment : ISaveSegment
    {
        /// <summary>插件 id（="pxc.warreparations"）。</summary>
        public string PluginId;

        /// <summary>段版本（v10.7.7 起为 1）。</summary>
        public int SegmentVersion = 1;

        /// <summary>生效条约数量。</summary>
        public int Count;

        /// <summary>付款方（战败/割地/称臣方）。</summary>
        public int[] Payers;

        /// <summary>收款方（战胜方）。</summary>
        public int[] Receivers;

        /// <summary>赔款资源类型（ResourceType 整型值）。</summary>
        public int[] Types;

        /// <summary>每年付款额。</summary>
        public float[] PerYears;

        /// <summary>剩余年数。</summary>
        public int[] YearsLeft;

        /// <summary>条约总年数。</summary>
        public int[] TotalYears;

        /// <summary>签订年份。</summary>
        public int[] StartYears;

        /// <summary>截至存档时累计实付（用于完成事件报告总额）。</summary>
        public float[] TotalPaid;

        string ISaveSegment.PluginId => PluginId;
        int ISaveSegment.SegmentVersion => SegmentVersion;
    }
}
