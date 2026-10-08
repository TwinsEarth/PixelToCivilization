using System;
using System.Collections.Generic;
using System.Text;

namespace PixelToCivilization.AI.Agents
{
    /// <summary>一条"情境 → 动作"行为记录（可被离线复用）</summary>
    [Serializable]
    public class AgentBehaviorEntry
    {
        public string AgentId;       // 所属智能体
        public string Situation;     // 情境（如 "被敌舰围攻" / "人口不足"）
        public string Action;        // 动作（如 "向附近友舰求援"）
        public string Source;        // 来源：offline / online / hybrid
        public int Uses = 1;         // 被复用次数
        public int Score;            // 成效评分（-2~2，越高越值得复用）
    }

    /// <summary>
    /// 离线行为库：混合模式下把大模型决策沉淀为本地行为，离线模式按情境复用。
    /// 可序列化（JsonUtility，含 List 包装）、分智能体限长、可持久化、可导出。
    /// </summary>
    [Serializable]
    public class AgentBehaviorLibrary
    {
        public const int PerAgentCap = 20;
        public List<AgentBehaviorEntry> Entries = new();

        private List<AgentBehaviorEntry> Find(string agentId, string situation)
        {
            var list = new List<AgentBehaviorEntry>();
            foreach (var e in Entries)
                if (e.AgentId == agentId &&
                    (string.IsNullOrEmpty(situation) || e.Situation == situation))
                    list.Add(e);
            return list;
        }

        /// <summary>记录一条行为（同一情境已存在则复用并 Uses+1，否则新增；分智能体限长）</summary>
        public void Record(string agentId, string situation, string action, string source)
        {
            var existing = Find(agentId, situation);
            if (existing.Count > 0)
            {
                var e = existing[0];
                e.Uses++;
                if (!string.IsNullOrEmpty(action)) e.Action = action;
                return;
            }
            Entries.Add(new AgentBehaviorEntry
            {
                AgentId = agentId, Situation = situation, Action = action,
                Source = source, Uses = 1
            });
            // 超限时淘汰该智能体 Uses 最少的
            int count = 0;
            foreach (var e in Entries) if (e.AgentId == agentId) count++;
            if (count > PerAgentCap)
            {
                AgentBehaviorEntry worst = null;
                foreach (var e in Entries)
                    if (e.AgentId == agentId && (worst == null || e.Uses < worst.Uses)) worst = e;
                if (worst != null) Entries.Remove(worst);
            }
        }

        /// <summary>离线模式：按情境取成效最好的行为（没有则返回 null）</summary>
        public AgentBehaviorEntry Recall(string agentId, string situation)
        {
            var matches = Find(agentId, situation);
            if (matches.Count == 0) return null;
            AgentBehaviorEntry best = null;
            foreach (var e in matches)
            {
                int rank = e.Score * 100 + e.Uses;
                int bestRank = best == null ? int.MinValue : best.Score * 100 + best.Uses;
                if (rank > bestRank) best = e;
            }
            return best;
        }

        /// <summary>反馈某条行为的成效（混合模式自我强化）</summary>
        public void Reinforce(string agentId, string situation, int delta)
        {
            var e = Recall(agentId, situation);
            if (e != null) { e.Score += delta; if (e.Score > 2) e.Score = 2; if (e.Score < -2) e.Score = -2; }
        }

        public int CountFor(string agentId)
        {
            int n = 0;
            foreach (var e in Entries) if (e.AgentId == agentId) n++;
            return n;
        }

        public void Clear() => Entries.Clear();

        /// <summary>导出为可上传文本（联网时上传豆包云盘 / 合并进公用离线库）</summary>
        public string Export()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# AgentBehaviorLibrary export (Situation -> Action)");
            foreach (var e in Entries)
                sb.AppendLine($"[{e.AgentId}] ({e.Source},uses={e.Uses},score={e.Score}) {e.Situation} => {e.Action}");
            return sb.ToString();
        }
    }
}
