using System;

namespace PxC.Core.Persistence
{
    /// <summary>
    /// 势力静态信息的存档快照（DTO）。FactionRegistry 是共享层 registry，不属任何插件，
    /// 故随 SaveData 顶层保存，保证读档后势力名单（含灭国状态、玩家归属、出生坐标、颜色）完整恢复。
    /// </summary>
    [Serializable]
    public sealed class FactionSnapshot
    {
        /// <summary>显示名（部落/诸侯/王朝/国家）。</summary>
        public string Name;

        /// <summary>是否为玩家控制的主文明。</summary>
        public bool IsPlayer;

        /// <summary>是否存续（被灭国后 false）。</summary>
        public bool Alive = true;

        /// <summary>文明颜色（视图用，索引到调色板）。</summary>
        public int ColorIndex;

        /// <summary>出生地中心坐标（世界 xz）。</summary>
        public float HomeX;
        public float HomeZ;
    }
}
