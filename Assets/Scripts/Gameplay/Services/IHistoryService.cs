namespace PxC.Gameplay.Services
{
    /// <summary>历史事件类别（"小时"记录的条目分类）。</summary>
    public enum HistoryKind
    {
        Other = 0,
        Dynasty = 1,
        Disaster = 2,
        War = 3,
        Tech = 4,
        Famine = 5,
        Policy = 6,
        Culture = 7,
        Victory = 8,
    }

    /// <summary>
    /// 一条历史记录（环形缓冲中的条目）。
    /// </summary>
    public struct HistoryEntry
    {
        /// <summary>类别（HistoryKind）。</summary>
        public int Kind;

        /// <summary>游戏年份。</summary>
        public int Year;

        /// <summary>相关势力（-1 表示全局）。</summary>
        public int Faction;

        /// <summary>数值1（如死亡数、新等级、新政策）。</summary>
        public int A;

        /// <summary>数值2（如建筑摧毁数）。</summary>
        public int B;

        /// <summary>文本（灾害/政策/朝代名称，可为 null）。</summary>
        public string Text;
    }

    /// <summary>
    /// 历史服务（history 插件实现，Tier-1，对应"小时"）。
    /// 环形缓冲记录重大事件，满了覆盖最旧；提供按时间倒序查询。
    /// </summary>
    public interface IHistoryService
    {
        /// <summary>已记录条目数（≤容量）。</summary>
        int Count { get; }

        /// <summary>环形缓冲容量。</summary>
        int Capacity { get; }

        /// <summary>记录一条。</summary>
        void Record(HistoryKind kind, int year, int faction, int a, int b, string text);

        /// <summary>按时间倒序取第 i 条（i=0 最新）。</summary>
        HistoryEntry GetRecent(int i);
    }
}
