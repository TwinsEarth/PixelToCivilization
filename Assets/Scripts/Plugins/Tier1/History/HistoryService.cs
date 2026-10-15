using System;
using PxC.Gameplay.Services;

namespace PxC.Plugins.History
{
    /// <summary>
    /// 历史服务：写入环形缓冲，提供可读描述。
    /// </summary>
    public sealed class HistoryService : IHistoryService
    {
        private readonly HistoryState _state;
        private readonly Action<string> _log;

        public HistoryService(HistoryState state, Action<string> log)
        {
            _state = state;
            _log = log;
        }

        public int Count => _state.Count;
        public int Capacity => _state.Capacity;

        public void Record(HistoryKind kind, int year, int faction, int a, int b, string text)
        {
            _state.Add(new HistoryEntry
            {
                Kind = (int)kind,
                Year = year,
                Faction = faction,
                A = a,
                B = b,
                Text = text,
            });
            _log?.Invoke($"[历史] {Describe(_state.GetRecent(0))}");
        }

        public HistoryEntry GetRecent(int i) => _state.GetRecent(i);

        /// <summary>生成可读文本。</summary>
        public static string Describe(HistoryEntry e)
        {
            var kind = (HistoryKind)e.Kind;
            switch (kind)
            {
                case HistoryKind.Disaster:
                    return $"{e.Year}年 {e.Text}：死亡{e.A}人，毁建筑{e.B}栋";
                case HistoryKind.Famine:
                    return $"{e.Year}年 势力{e.Faction} 饥荒（缺口{e.A}）";
                case HistoryKind.Policy:
                    return $"{e.Year}年 势力{e.Faction} 改为 {e.Text}";
                case HistoryKind.Culture:
                    return $"{e.Year}年 势力{e.Faction} 文化升至{e.A}级";
                case HistoryKind.Tech:
                    return $"{e.Year}年 势力{e.Faction} 进入 {e.Text}";
                case HistoryKind.Dynasty:
                    return $"{e.Year}年 朝代更替：{e.Text}";
                case HistoryKind.War:
                    return $"{e.Year}年 势力{e.A} 对 势力{e.B} 宣战";
                case HistoryKind.Victory:
                    return $"{e.Year}年 势力{e.Faction} {e.Text} 胜利！";
                default:
                    return $"{e.Year}年 {e.Text}";
            }
        }
    }
}
