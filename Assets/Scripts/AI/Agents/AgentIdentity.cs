using System;

namespace PixelToCivilization.AI.Agents
{
    /// <summary>智能体的大类（用于注册表分组与 UI 浏览）</summary>
    public enum AgentKind
    {
        System,      // 游戏系统 / 管理者
        Manager,     // 内存、存档、对象池、崩溃守护等基础管理
        Building,    // 建筑（原型）
        Ship,        // 船只（原型）
        Vehicle,     // 车辆（原型）
        Ground,      // 地面作战装备（原型）
        Person,      // 人物（按职业原型）
        Nature,      // 树 / 山 / 鸟 / 鱼 / 土地 / 岛屿 / 道路（原型）
        Commander,   // 实例级指挥官（航母、坦克指挥官等）
    }

    /// <summary>
    /// 智能体身份：一个对象"我是谁、我负责什么、我会什么、我关注什么"。
    /// 身份按原型共享（只读），实体实例只持有 Id 引用，不复制身份对象。
    /// </summary>
    [Serializable]
    public class AgentIdentity
    {
        public string Id;                 // 唯一 id
        public AgentKind Kind;            // 大类
        public string Group;              // UI 分组（中文小类名）
        public string Name;               // 名称
        public string Domain;             // 职责域
        public string Intro;              // 自我介绍台词（第一人称）
        public string[] Capabilities;     // 能力标签
        public string[] Watches;          // 关注事件
        public long Color;                // 代表色
        public float Priority;            // 自主优先级（0~1，越高越主动）

        public AgentIdentity() { }

        public AgentIdentity(string id, AgentKind kind, string group, string name,
            string domain, string intro, long color, float priority,
            string[] capabilities = null, string[] watches = null)
        {
            Id = id; Kind = kind; Group = group; Name = name;
            Domain = domain; Intro = intro; Color = color; Priority = priority;
            Capabilities = capabilities ?? Array.Empty<string>();
            Watches = watches ?? Array.Empty<string>();
        }

        /// <summary>完整自我介绍（用于属性面板 / 探针）</summary>
        public string FullIntro
        {
            get
            {
                string cap = Capabilities != null && Capabilities.Length > 0
                    ? "｜能做：" + string.Join("、", Capabilities) : "";
                return $"【{Name}】（{Domain}）{Intro}{cap}";
            }
        }
    }
}
