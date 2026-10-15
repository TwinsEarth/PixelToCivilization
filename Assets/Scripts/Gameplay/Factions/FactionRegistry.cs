using System.Collections.Generic;

namespace PxC.Gameplay.Factions
{
    /// <summary>势力（faction）静态信息：玩家文明与 AI 文明共用。</summary>
    public sealed class FactionInfo
    {
        /// <summary>势力 id（=在 FactionRegistry 中的顺序索引，0 起）。</summary>
        public int Id;

        /// <summary>显示名（部落/诸侯/王朝/国家）。</summary>
        public string Name;

        /// <summary>是否为玩家控制的主文明。</summary>
        public bool IsPlayer;

        /// <summary>是否存续（被灭国后 false，停止结算）。</summary>
        public bool Alive = true;

        /// <summary>文明颜色（视图用，索引到调色板）。</summary>
        public int ColorIndex;

        /// <summary>出生地中心坐标（世界 xz，用于初始据点与单位出生锚点）。</summary>
        public float HomeX;
        public float HomeZ;
    }

    /// <summary>
    /// 势力登记表（运行时服务）。所有势力的权威名单，
    /// 经济/人口/科技/军事的 SoA 第一维均以 factionId 为索引。
    /// </summary>
    public sealed class FactionRegistry
    {
        private readonly List<FactionInfo> _factions = new List<FactionInfo>(8);

        /// <summary>势力总数（含已灭国）。</summary>
        public int Count => _factions.Count;

        /// <summary>玩家势力（未设置时 null）。</summary>
        public FactionInfo Player { get; private set; }

        /// <summary>新增势力，返回其 FactionInfo（id 自动分配）。</summary>
        public FactionInfo Add(string name, bool isPlayer, float homeX = 0f, float homeZ = 0f, int colorIndex = 0)
        {
            var info = new FactionInfo
            {
                Id = _factions.Count,
                Name = name,
                IsPlayer = isPlayer,
                HomeX = homeX,
                HomeZ = homeZ,
                ColorIndex = colorIndex,
            };
            _factions.Add(info);
            if (isPlayer) Player = info;
            return info;
        }

        /// <summary>按 id 取势力（越界返回 null）。</summary>
        public FactionInfo Get(int id) =>
            (id >= 0 && id < _factions.Count) ? _factions[id] : null;

        /// <summary>全部势力（含灭国）。</summary>
        public IReadOnlyList<FactionInfo> All => _factions;

        /// <summary>所有存续势力。</summary>
        public IEnumerable<FactionInfo> EnumerateAlive()
        {
            for (int i = 0; i < _factions.Count; i++)
                if (_factions[i].Alive) yield return _factions[i];
        }

        /// <summary>存续势力数量。</summary>
        public int AliveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _factions.Count; i++)
                    if (_factions[i].Alive) n++;
                return n;
            }
        }
    }
}
