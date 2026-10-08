using System;
using System.Collections.Generic;

namespace PixelToCivilization.AI.Agents
{
    /// <summary>智能体消息类型：广播 / 求助 / 请求 / 回应</summary>
    public enum AgentMessageType
    {
        Broadcast,   // 广播（告知全体）
        Help,        // 求助（我需要支援）
        Request,     // 请求（点对点，需要对方做某事）
        Reply,       // 回应（同意/拒绝/已到位）
    }

    /// <summary>智能体之间的一条消息</summary>
    public struct AgentMessage
    {
        public AgentMessageType Type;
        public string From;       // 发送者 id
        public string To;         // 接收者 id（空 = 全体）
        public string Topic;      // 主题（如 combat / transport / repair / rescue）
        public string Content;    // 内容
        public float RealTime;    // 现实时间戳
        public int GameYear;      // 游戏年
    }

    /// <summary>
    /// 智能体消息总线：对象间广播 / 求助 / 请求 / 回应，支持订阅回调。
    /// 环形缓冲、限长，零侵入（不依赖现有 EventBus 的字符串事件，避免与玩法事件耦合）。
    /// </summary>
    public class AgentMessageBus
    {
        public const int MaxMessages = 120;
        private readonly List<AgentMessage> _messages = new();
        private readonly List<Action<AgentMessage>> _watchers = new();

        public IReadOnlyList<AgentMessage> Messages => _messages;

        /// <summary>订阅所有消息（UI 面板 / 编年史用）</summary>
        public void Subscribe(Action<AgentMessage> w) { if (!_watchers.Contains(w)) _watchers.Add(w); }
        public void Unsubscribe(Action<AgentMessage> w) { _watchers.Remove(w); }

        public void Post(AgentMessageType type, string from, string to, string topic,
            string content, int gameYear, float realTime)
        {
            var m = new AgentMessage
            {
                Type = type, From = from, To = to ?? "", Topic = topic ?? "",
                Content = content ?? "", RealTime = realTime, GameYear = gameYear
            };
            _messages.Add(m);
            if (_messages.Count > MaxMessages) _messages.RemoveRange(0, _messages.Count - MaxMessages);
            // 复制一份再遍历，避免回调中订阅/退订导致枚举异常
            var snapshot = _watchers.ToArray();
            foreach (var w in snapshot)
            {
                try { w(m); }
                catch (Exception e) { UnityEngine.Debug.LogError("[AgentBus] watcher error: " + e.Message); }
            }
        }

        public void Broadcast(string from, string topic, string content, int gameYear, float realTime)
            => Post(AgentMessageType.Broadcast, from, "", topic, content, gameYear, realTime);

        /// <summary>向某接收者发请求；返回是否存在该接收者（由 Director 实际投递）</summary>
        public void Request(string from, string to, string topic, string content, int gameYear, float realTime)
            => Post(AgentMessageType.Request, from, to, topic, content, gameYear, realTime);

        public void Help(string from, string topic, string content, int gameYear, float realTime)
            => Post(AgentMessageType.Help, from, "", topic, content, gameYear, realTime);

        public void Reply(string from, string to, string topic, string content, int gameYear, float realTime)
            => Post(AgentMessageType.Reply, from, to, topic, content, gameYear, realTime);

        /// <summary>查询某智能体未处理的求助/请求（To 匹配或全体）</summary>
        public List<AgentMessage> PendingFor(string agentId)
        {
            var list = new List<AgentMessage>();
            for (int i = _messages.Count - 1; i >= 0; i--)
            {
                var m = _messages[i];
                if (m.Type != AgentMessageType.Help && m.Type != AgentMessageType.Request) continue;
                if (m.To == agentId || (m.Type == AgentMessageType.Help && m.To == ""))
                    list.Add(m);
                if (list.Count >= 8) break;
            }
            return list;
        }

        public void Clear() { _messages.Clear(); }
    }
}
